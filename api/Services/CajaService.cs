using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

/// <summary>Parámetros internos para insertar un movimiento de caja (compartido por los servicios).</summary>
public class MovimientoCajaParametros
{
    public string Origen { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public string? Observaciones { get; set; }
    public string? MonedaId { get; set; }
    public string? ClienteId { get; set; }
    public string? CompraId { get; set; }
    public string? VentaId { get; set; }
    public string? BancoId { get; set; }
    public string? CuentaTran { get; set; }
    public string? NumeroTran { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Usd { get; set; }
    public decimal? MontoBs { get; set; }
    public string? CodiIngresoId { get; set; }
    public string? CodiEgresoId { get; set; }
    public string? CentroCosto { get; set; }
}

public interface ICajaService
{
    Task<PagedResult<MovimientoCajaDto>> ListarAsync(MovimientoCajaFiltroDto filtro);
    Task<MovimientoCajaDto> ObtenerAsync(string ejercicio, string id);
    Task<MovimientoCajaDto> CrearIngresoEgresoAsync(bool esIngreso, MovimientoCajaCrearDto dto);
    Task<MovimientoCajaDto> AnularAsync(string ejercicio, string id, string? motivo);
    Task<CierreCajaResultadoDto> CerrarAsync(DateTime fecha);
    Task<CierreCajaEstadoDto> EstadoCierreAsync(DateTime fecha);
    Task<ArqueoDto> ArqueoAsync(DateTime fecha);
    /// <summary>Inserta un movimiento respetando correlativos (10 díglobales/año y recibo global de 20). Se une a la transacción activa.</summary>
    Task<Caja> InsertarMovimientoAsync(MovimientoCajaParametros p);
}

/// <summary>Libro de caja: movimientos, anulación soft, cierre de día y arqueo.</summary>
public sealed class CajaService : ICajaService
{
    private readonly MonrysDbContext _db;
    private readonly ICorrelativoService _correlativos;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuarioActual _usuario;

    public CajaService(MonrysDbContext db, ICorrelativoService correlativos,
        IAuditoriaService auditoria, IUsuarioActual usuario)
    {
        _db = db;
        _correlativos = correlativos;
        _auditoria = auditoria;
        _usuario = usuario;
    }

    public async Task<PagedResult<MovimientoCajaDto>> ListarAsync(MovimientoCajaFiltroDto f)
    {
        var q = _db.MovimientosCaja.AsNoTracking().AsQueryable();

        if (f.Desde.HasValue)
        {
            var d = f.Desde.Value.Date;
            q = q.Where(x => x.Fecha >= d);
        }
        if (f.Hasta.HasValue)
        {
            var h = f.Hasta.Value.Date.AddDays(1);
            q = q.Where(x => x.Fecha < h);
        }
        if (!string.IsNullOrWhiteSpace(f.Origen)) q = q.Where(x => x.Origen == f.Origen);
        if (!string.IsNullOrWhiteSpace(f.ClienteId)) q = q.Where(x => x.ClienteId == f.ClienteId);
        if (!string.IsNullOrWhiteSpace(f.Estado)) q = q.Where(x => x.Estado == f.Estado);
        if (!string.IsNullOrWhiteSpace(f.Buscar))
        {
            var t = f.Buscar.Trim();
            q = q.Where(x => (x.Descripcion != null && x.Descripcion.Contains(t)) ||
                             x.Recibo.Contains(t) ||
                             x.Id.Contains(t));
        }

        var total = await q.CountAsync();
        var pagina = Math.Max(1, f.Pagina);
        var tamano = Math.Clamp(f.Tamano, 1, 200);

        var filas = await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync();

        var monedas = await _db.TipoMonedas.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var clienteIds = filas.Select(x => x.ClienteId).Where(x => x != null).Select(x => x!).Distinct().ToList();
        var clientes = await _db.Clientes.AsNoTracking()
            .Where(c => clienteIds.Contains(c.Id))
            .ToDictionaryAsync(x => x.Id);

        return new PagedResult<MovimientoCajaDto>
        {
            Total = total,
            Items = filas.Select(f2 => Mapear(f2, monedas, clientes)).ToList()
        };
    }

    public async Task<MovimientoCajaDto> ObtenerAsync(string ejercicio, string id)
    {
        var fila = await _db.MovimientosCaja.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ejercicio == ejercicio && x.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el movimiento de caja {ejercicio}-{id}.");

        var monedas = await _db.TipoMonedas.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var clientes = fila.ClienteId is null
            ? new Dictionary<string, Cliente>()
            : await _db.Clientes.AsNoTracking().Where(c => c.Id == fila.ClienteId).ToDictionaryAsync(x => x.Id);

        return Mapear(fila, monedas, clientes);
    }

    public async Task<MovimientoCajaDto> CrearIngresoEgresoAsync(bool esIngreso, MovimientoCajaCrearDto dto)
    {
        if (dto.Monto <= 0)
            throw new ValidacionException("El monto debe ser mayor que cero.");

        // Precedencia: si el día está cerrado, la regla de congelamiento gana sobre cualquier otra validación.
        await EnsureDiaAbiertoAsync(dto.Fecha?.Date ?? DateTime.Today);

        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");

        var monedaId = string.IsNullOrWhiteSpace(dto.MonedaId) ? config.MonedaNacionalId : dto.MonedaId.Trim();
        var moneda = await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == monedaId)
            ?? throw new ValidacionException($"La moneda '{monedaId}' no existe.");

        if (esIngreso)
        {
            if (string.IsNullOrWhiteSpace(dto.CodiIngresoId))
                throw new ValidacionException("Debe indicar la codificación de ingreso.");
            if (!await _db.CodificacionesIngreso.AnyAsync(c => c.Id == dto.CodiIngresoId))
                throw new ValidacionException($"La codificación de ingreso '{dto.CodiIngresoId}' no existe.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.CodiEgresoId))
                throw new ValidacionException("Debe indicar la codificación de egreso.");
            if (!await _db.CodificacionesEgreso.AnyAsync(c => c.Id == dto.CodiEgresoId))
                throw new ValidacionException($"La codificación de egreso '{dto.CodiEgresoId}' no existe.");
        }

        if (dto.ClienteId is not null && !await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
            throw new ValidacionException($"El cliente '{dto.ClienteId}' no existe.");
        if (dto.BancoId is not null && !await _db.Bancos.AnyAsync(b => b.Id == dto.BancoId))
            throw new ValidacionException($"El banco '{dto.BancoId}' no existe.");

        // Regla del VFP (cajareposicion): nacional ⇒ IN/EG, divisa ⇒ DE/DS.
        var esNacional = moneda.Nacional || moneda.Id == config.MonedaNacionalId;
        var origen = esIngreso
            ? (esNacional ? OrigenesCaja.Ingreso : OrigenesCaja.DepositoDivisa)
            : (esNacional ? OrigenesCaja.Egreso : OrigenesCaja.SalidaDivisa);

        var tasa = dto.Tasa is > 0 ? dto.Tasa.Value : config.TasaActiva;
        decimal? usd = dto.Usd;
        decimal? montoBs = dto.MontoBs;

        if (esNacional)
        {
            usd ??= tasa > 0 ? Math.Round(dto.Monto / tasa, 2) : null;
            montoBs ??= dto.Monto;
        }
        else
        {
            montoBs ??= Math.Round(dto.Monto * tasa, 2);
            usd ??= dto.Monto;
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fila = await InsertarMovimientoAsync(new MovimientoCajaParametros
        {
            Origen = origen,
            Fecha = dto.Fecha?.Date ?? DateTime.Today,
            Monto = dto.Monto,
            Descripcion = dto.Descripcion ?? (esIngreso ? "Ingreso de caja" : "Egreso de caja"),
            Observaciones = dto.Observaciones,
            MonedaId = moneda.Id,
            ClienteId = dto.ClienteId,
            BancoId = dto.BancoId,
            CuentaTran = dto.CuentaTran,
            NumeroTran = dto.NumeroTran,
            Tasa = tasa,
            Usd = usd,
            MontoBs = montoBs,
            CodiIngresoId = esIngreso ? dto.CodiIngresoId : null,
            CodiEgresoId = esIngreso ? null : dto.CodiEgresoId,
            CentroCosto = dto.CentroCosto
        });

        await _auditoria.RegistrarAsync(
            $"{(esIngreso ? "REGISTRA INGRESO" : "REGISTRA EGRESO")} DE CAJA NRO. {fila.Recibo} MONTO {fila.Monto:0.00}",
            "caja", $"{fila.Ejercicio}-{fila.Id}");

        await tx.CommitAsync();
        return await ObtenerAsync(fila.Ejercicio, fila.Id);
    }

    public async Task<MovimientoCajaDto> AnularAsync(string ejercicio, string id, string? motivo)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var fila = await _db.MovimientosCaja.FirstOrDefaultAsync(x => x.Ejercicio == ejercicio && x.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el movimiento de caja {ejercicio}-{id}.");

        if (fila.Estado == "ANULADO")
            throw new ConflictoException($"El recibo {fila.Recibo} ya está anulado.");

        if (fila.CierreFecha != null)
            throw new ConflictoException($"La caja del {fila.Fecha:dd/MM/yyyy} quedó cerrada; no se pueden anular movimientos de ese día.");

        var previo = new
        {
            fila.Ejercicio, fila.Id, fila.Origen, fila.Fecha, fila.Recibo, fila.Monto,
            fila.Descripcion, fila.MonedaId, fila.ClienteId, fila.Estado
        };

        fila.Estado = "ANULADO";
        fila.AnuladoEn = DateTime.Now;
        fila.AnuladoMotivo = string.IsNullOrWhiteSpace(motivo) ? "Sin motivo" : motivo.Trim();

        await _auditoria.RegistrarAsync(
            $"ANULA EL RECIBO DE CAJA NRO: {fila.Recibo} ORIGEN {fila.Origen}",
            "caja", $"{fila.Ejercicio}-{fila.Id}", previo, fila.AnuladoMotivo);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return await ObtenerAsync(fila.Ejercicio, fila.Id);
    }

    public async Task<CierreCajaResultadoDto> CerrarAsync(DateTime fecha)
    {
        var dia = fecha.Date;
        await using var tx = await _db.Database.BeginTransactionAsync();

        var cerrados = await _db.MovimientosCaja.CountAsync(x => x.Fecha == dia && x.CierreFecha != null);
        if (cerrados > 0)
            throw new ConflictoException($"La caja del {dia:dd/MM/yyyy} ya fue cerrada ({cerrados} movimientos).");

        var filas = await _db.MovimientosCaja
            .Where(x => x.Fecha == dia && x.CierreFecha == null && x.Estado == "ACTIVO")
            .ToListAsync();

        var usuarioId = _usuario.Id ?? "SISTEMA";
        foreach (var fila in filas)
        {
            fila.CierreFecha = dia;
            fila.CierreId = usuarioId;
        }

        await _auditoria.RegistrarAsync($"CIERRA LA CAJA DEL {dia:dd/MM/yyyy}", "caja", null,
            new { fecha = dia.ToString("yyyy-MM-dd"), movimientos = filas.Count, usuario = usuarioId });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new CierreCajaResultadoDto { Fecha = dia, MovimientosCerrados = filas.Count, CerradoPor = usuarioId };
    }

    public async Task<CierreCajaEstadoDto> EstadoCierreAsync(DateTime fecha)
    {
        var dia = fecha.Date;
        var filas = await _db.MovimientosCaja.AsNoTracking().Where(x => x.Fecha == dia).ToListAsync();
        var cerrados = filas.Where(x => x.CierreFecha != null).ToList();

        // Total del día en Bs: usar monto_bs propio del movimiento (no el monto crudo, que mezcla USD y Bs).
        // El historial VFP trae 0 en "sin calcular": en ese caso se usa el monto crudo.
        var saldo = filas.Where(x => x.Estado == "ACTIVO")
            .Sum(x => OrigenesCaja.Signo(x.Origen) * (x.MontoBs is > 0 ? x.MontoBs.Value : x.Monto));

        return new CierreCajaEstadoDto
        {
            Fecha = dia,
            Cerrado = cerrados.Count > 0,
            Movimientos = filas.Count,
            Total = saldo,
            CerradoPor = cerrados.Select(x => x.CierreId).FirstOrDefault(x => x != null)
        };
    }

    public async Task<ArqueoDto> ArqueoAsync(DateTime fecha)
    {
        var dia = fecha.Date;
        var monedas = await _db.TipoMonedas.AsNoTracking().Where(m => m.Activa).ToListAsync();
        var denominaciones = await _db.Denominaciones.AsNoTracking().Where(d => d.Activa).ToListAsync();
        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha <= dia && x.Estado == "ACTIVO")
            .Select(x => new { x.Origen, x.MonedaId, x.Monto })
            .ToListAsync();

        var resultado = new ArqueoDto { Fecha = dia };

        foreach (var moneda in monedas)
        {
            var total = movimientos
                .Where(x => x.MonedaId == moneda.Id && OrigenesCaja.Signo(x.Origen) != 0)
                .Sum(x => OrigenesCaja.Signo(x.Origen) * x.Monto);

            resultado.Monedas.Add(new ArqueoMonedaDto
            {
                MonedaId = moneda.Id,
                Descripcion = moneda.Descripcion,
                Nacional = moneda.Nacional,
                TotalCaja = total,
                Denominaciones = denominaciones
                    .Where(d => d.MonedaId == moneda.Id)
                    .OrderBy(d => d.Valor)
                    .Select(d => new ArqueoDenominacionDto { Id = d.Id, Tipo = d.Tipo, Valor = d.Valor })
                    .ToList()
            });
        }

        return resultado;
    }

    public async Task<Caja> InsertarMovimientoAsync(MovimientoCajaParametros p)
    {
        if (string.IsNullOrWhiteSpace(p.Origen))
            throw new ValidacionException("Debe indicar el origen del movimiento de caja.");
        if (p.Monto == 0)
            throw new ValidacionException("El monto del movimiento de caja no puede ser cero.");

        var fecha = p.Fecha.Date;
        await EnsureDiaAbiertoAsync(fecha);

        var ejercicio = fecha.Year.ToString();

        var id = await _correlativos.SiguienteIdAsync("CAJA_ID", ejercicio, 10);
        var recibo = await _correlativos.SiguienteIdAsync("CAJA_RECIBO", CorrelativoService.EjercicioGlobal, 20);

        var fila = new Caja
        {
            Id = id,
            Ejercicio = ejercicio,
            Origen = p.Origen,
            Fecha = fecha,
            Recibo = recibo,
            Monto = p.Monto,
            Descripcion = Truncar(p.Descripcion, 100),
            Observaciones = Truncar(p.Observaciones, 200),
            MonedaId = p.MonedaId,
            ClienteId = p.ClienteId,
            CompraId = p.CompraId,
            VentaId = p.VentaId,
            BancoId = p.BancoId,
            CuentaTran = p.CuentaTran,
            NumeroTran = p.NumeroTran,
            Tasa = p.Tasa,
            Usd = p.Usd,
            MontoBs = p.MontoBs,
            CodiIngresoId = p.CodiIngresoId,
            CodiEgresoId = p.CodiEgresoId,
            CentroCosto = p.CentroCosto,
            Estado = "ACTIVO"
        };

        _db.MovimientosCaja.Add(fila);
        await _db.SaveChangesAsync();
        return fila;
    }

    /// <summary>Regla de cierre (VFP: FECHACIER/ID_CIERR): el día con cierre queda congelado.</summary>
    private async Task EnsureDiaAbiertoAsync(DateTime fecha)
    {
        if (await _db.MovimientosCaja.AsNoTracking().AnyAsync(x => x.Fecha == fecha && x.CierreFecha != null))
            throw new ConflictoException($"La caja del {fecha:dd/MM/yyyy} ya fue cerrada; el día queda congelado.");
    }

    private static string? Truncar(string? valor, int max)
        => valor is null ? null : (valor.Length <= max ? valor : valor[..max]);

    private static MovimientoCajaDto Mapear(Caja f, Dictionary<string, TipoMoneda> monedas, Dictionary<string, Cliente> clientes)
    {
        monedas.TryGetValue(f.MonedaId ?? "", out var moneda);
        var cliente = f.ClienteId is not null && clientes.TryGetValue(f.ClienteId, out var c) ? c : null;

        return new MovimientoCajaDto
        {
            Ejercicio = f.Ejercicio,
            Id = f.Id,
            Origen = f.Origen,
            Fecha = f.Fecha,
            Recibo = f.Recibo,
            Monto = f.Monto,
            Descripcion = f.Descripcion,
            Observaciones = f.Observaciones,
            MonedaId = f.MonedaId,
            Moneda = moneda?.Descripcion,
            ClienteId = f.ClienteId,
            Cliente = cliente?.NombreCompleto,
            CompraId = f.CompraId,
            VentaId = f.VentaId,
            BancoId = f.BancoId,
            CuentaTran = f.CuentaTran,
            NumeroTran = f.NumeroTran,
            Tasa = f.Tasa,
            Usd = f.Usd,
            MontoBs = f.MontoBs,
            CodiIngresoId = f.CodiIngresoId,
            CodiEgresoId = f.CodiEgresoId,
            CentroCosto = f.CentroCosto,
            CierreFecha = f.CierreFecha,
            CierreId = f.CierreId,
            Estado = f.Estado,
            AnuladoMotivo = f.AnuladoMotivo,
            AnuladoEn = f.AnuladoEn,
            CreadoEn = f.CreadoEn
        };
    }
}
