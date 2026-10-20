using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface ICustodioService
{
    Task<List<CustodioSaldoDto>> SaldosAsync(string? clienteId);
    Task<(List<CustodioMovimientoDto> items, int total, decimal disponible)> MovimientosAsync(
        string clienteId, DateTime? desde, DateTime? hasta, int pagina, int tamano);

    /// <summary>Recibe divisa del cliente en custodia → COMPENSACION.ABONOS (disponible aumenta).</summary>
    Task<CustodioMoverResultadoDto> RecibirAsync(CustodioMoverDto dto);

    /// <summary>Entrega divisa al cliente desde su custodia → COMPENSACION.RETIROS (disponible disminuye).</summary>
    Task<CustodioMoverResultadoDto> EntregarAsync(CustodioMoverDto dto);

    /// <summary>Inserta la fila de compensación (la usan también las operaciones con destino custodio).</summary>
    Task<(string id, string recibo)> InsertarCompensacionAsync(string clienteId, string monedaId,
        decimal abonos, decimal retiros, DateTime fecha, string? descripcion, string? centroCosto,
        string? observaciones = null);

    /// <summary>Recalcula CUSTODIO.disponible = Σ abonos − Σ retiros en la transacción activa.</summary>
    Task<decimal> RecalcularCustodioAsync(string clienteId, string monedaId);
}

/// <summary>
/// Custodia de clientes (VFP: custodios.scx). Dirección de los signos según
/// docs/analisis/03-reglas-negocio-formularios.md §3.1–§3.2:
/// ABONOS = el cliente entrega divisa al sistema (custodia +) · RETIROS = el cliente la retira (custodia −).
/// </summary>
public sealed class CustodioService : ICustodioService
{
    private readonly MonrysDbContext _db;
    private readonly ICorrelativoService _correlativos;
    private readonly ICajaService _caja;
    private readonly IAuditoriaService _auditoria;

    public CustodioService(MonrysDbContext db, ICorrelativoService correlativos,
        ICajaService caja, IAuditoriaService auditoria)
    {
        _db = db;
        _correlativos = correlativos;
        _caja = caja;
        _auditoria = auditoria;
    }

    public async Task<List<CustodioSaldoDto>> SaldosAsync(string? clienteId)
    {
        var q = _db.Custodios.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(clienteId)) q = q.Where(c => c.ClienteId == clienteId);

        var filas = await q.OrderBy(c => c.ClienteId).ThenBy(c => c.MonedaId).ToListAsync();
        if (filas.Count == 0) return [];

        var clientes = await _db.Clientes.AsNoTracking()
            .Where(c => filas.Select(f => f.ClienteId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);
        var monedas = await _db.TipoMonedas.AsNoTracking().ToDictionaryAsync(m => m.Id);
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync();

        return filas.Select(f =>
        {
            clientes.TryGetValue(f.ClienteId, out var cliente);
            monedas.TryGetValue(f.MonedaId, out var moneda);
            // El histórico VFP guarda 0 en disponible_bs ("sin calcular"): en ese caso
            // se convierte con la tasa activa (o se replica si la moneda es nacional).
            var disponibleBs = f.DisponibleBs is > 0
                ? f.DisponibleBs
                : moneda is { Nacional: false } && config is not null
                    ? Math.Round(f.Disponible * config.TasaActiva, 2)
                    : f.Disponible;
            return new CustodioSaldoDto
            {
                ClienteId = f.ClienteId,
                Cliente = cliente?.NombreCompleto ?? f.ClienteId,
                MonedaId = f.MonedaId,
                Moneda = moneda?.Descripcion ?? f.MonedaId,
                Disponible = f.Disponible,
                DisponibleBs = disponibleBs,
                ActualizadoEn = f.ActualizadoEn
            };
        }).ToList();
    }

    public async Task<(List<CustodioMovimientoDto> items, int total, decimal disponible)> MovimientosAsync(
        string clienteId, DateTime? desde, DateTime? hasta, int pagina, int tamano)
    {
        var q = _db.Compensaciones.AsNoTracking().Where(c => c.ClienteId == clienteId);
        if (desde.HasValue) { var d = desde.Value.Date; q = q.Where(c => c.Fecha >= d); }
        if (hasta.HasValue) { var h = hasta.Value.Date.AddDays(1); q = q.Where(c => c.Fecha < h); }

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .Skip((Math.Max(1, pagina) - 1) * Math.Clamp(tamano, 1, 200))
            .Take(Math.Clamp(tamano, 1, 200))
            .ToListAsync();

        var monedas = await _db.TipoMonedas.AsNoTracking().ToDictionaryAsync(m => m.Id);
        _ = monedas;

        var items = filas.Select(f => new CustodioMovimientoDto
        {
            Id = f.Id,
            Ejercicio = f.Ejercicio,
            Fecha = f.Fecha,
            Descripcion = f.Descripcion,
            ClienteId = f.ClienteId,
            MonedaId = f.MonedaId,
            Abonos = f.Abonos,
            Retiros = f.Retiros,
            ReciboTran = f.ReciboTran,
            CentroCosto = f.CentroCosto,
            Observaciones = f.Observaciones,
            ReversoDe = f.ReversoDe,
            CreadoEn = f.CreadoEn
        }).ToList();

        var disponible = await CalcularDisponibleAsync(clienteId, null);
        return (items, total, disponible);
    }

    public async Task<CustodioMoverResultadoDto> RecibirAsync(CustodioMoverDto dto)
        => await MoverAsync(dto, esAbono: true);

    public async Task<CustodioMoverResultadoDto> EntregarAsync(CustodioMoverDto dto)
        => await MoverAsync(dto, esAbono: false);

    private async Task<CustodioMoverResultadoDto> MoverAsync(CustodioMoverDto dto, bool esAbono)
    {
        if (dto.Monto <= 0)
            throw new ValidacionException("El monto debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(dto.ClienteId))
            throw new ValidacionException("Debe indicar el cliente.");
        if (string.IsNullOrWhiteSpace(dto.MonedaId))
            throw new ValidacionException("Debe indicar la moneda.");

        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.ClienteId)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {dto.ClienteId}.");
        if (!cliente.Activo)
            throw new ValidacionException("El cliente está inactivo.");

        var moneda = await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == dto.MonedaId)
            ?? throw new RecursoNoEncontradoException($"No existe la moneda {dto.MonedaId}.");
        if (moneda.Nacional)
            throw new ValidacionException("La divisa seleccionada es parámetro nacional. NO VÁLIDO PARA CUSTODIO.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fecha = dto.Fecha?.Date ?? DateTime.Today;
        var descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
            ? (esAbono ? "RECEPCIÓN CUSTODIO DIVISAS" : "ENTREGA CUSTODIO DIVISAS")
            : dto.Descripcion;

        var (id, recibo) = await InsertarCompensacionAsync(
            cliente.Id, moneda.Id,
            abonos: esAbono ? dto.Monto : 0m,
            retiros: esAbono ? 0m : dto.Monto,
            fecha: fecha,
            descripcion: descripcion,
            centroCosto: dto.CentroCosto);

        var disponible = await RecalcularCustodioAsync(cliente.Id, moneda.Id);

        string? reciboCaja = null;
        string? idCaja = null;
        if (dto.DeCaja)
        {
            var movimiento = await _caja.InsertarMovimientoAsync(new MovimientoCajaParametros
            {
                Origen = esAbono ? OrigenesCaja.RecepcionCustodio : OrigenesCaja.EntregaCustodio,
                Fecha = fecha,
                Monto = dto.Monto,
                Descripcion = $"{descripcion}: {moneda.Descripcion}(CAJA)",
                MonedaId = moneda.Id,
                ClienteId = cliente.Id,
                CentroCosto = dto.CentroCosto
            });
            reciboCaja = movimiento.Recibo;
            idCaja = movimiento.Id;
        }

        await _auditoria.RegistrarAsync(
            $"{(esAbono ? "REGISTRA RECEPCIÓN" : "REGISTRA ENTREGA")} DE CUSTODIO AL CLIENTE {cliente.Id} MONTO {dto.Monto:0.00} {moneda.Descripcion} RECIBO {recibo}",
            "compensacion", id,
            new { clienteId = cliente.Id, monedaId = moneda.Id, monto = dto.Monto, tipo = esAbono ? "ABONO" : "RETIRO" });

        await tx.CommitAsync();

        return new CustodioMoverResultadoDto
        {
            CompensacionId = id,
            Recibo = recibo,
            Tipo = esAbono ? "ABONO" : "RETIRO",
            Monto = dto.Monto,
            Disponible = disponible,
            ReciboCaja = reciboCaja,
            IdCaja = idCaja
        };
    }

    public async Task<(string id, string recibo)> InsertarCompensacionAsync(string clienteId, string monedaId,
        decimal abonos, decimal retiros, DateTime fecha, string? descripcion, string? centroCosto,
        string? observaciones = null)
    {
        if (abonos < 0 || retiros < 0)
            throw new ValidacionException("Los montos de compensación no pueden ser negativos.");
        if (abonos == 0 && retiros == 0)
            throw new ValidacionException("La compensación debe tener un monto en abonos o retiros.");
        if (abonos > 0 && retiros > 0)
            throw new ValidacionException("Una compensación nunca usa abonos y retiros a la vez.");

        var ejercicio = fecha.Year.ToString();
        var id = await _correlativos.SiguienteIdAsync("COMPENSACION", ejercicio, 10);
        var recibo = ejercicio + id;   // recibo de compensación: ejercicio + id (VFP: EJERCC + ID_TRANS)

        var fila = new Compensacion
        {
            Id = id,
            Ejercicio = ejercicio,
            Fecha = fecha.Date,
            Descripcion = descripcion is null ? null : (descripcion.Length <= 50 ? descripcion : descripcion[..50]),
            ClienteId = clienteId,
            MonedaId = monedaId,
            Abonos = abonos,
            Retiros = retiros,
            ReciboTran = recibo,
            CentroCosto = centroCosto,
            Observaciones = observaciones is null ? null : (observaciones.Length <= 30 ? observaciones : observaciones[..30])
        };

        _db.Compensaciones.Add(fila);
        await _db.SaveChangesAsync();
        return (id, recibo);
    }

    public async Task<decimal> RecalcularCustodioAsync(string clienteId, string monedaId)
    {
        var filas = await _db.Compensaciones.AsNoTracking()
            .Where(c => c.ClienteId == clienteId && c.MonedaId == monedaId)
            .Select(c => new { c.Abonos, c.Retiros })
            .ToListAsync();

        var disponible = filas.Sum(f => f.Abonos) - filas.Sum(f => f.Retiros);

        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync();
        var moneda = await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == monedaId);
        var disponibleBs = moneda is not null && !moneda.Nacional && config is not null
            ? Math.Round(disponible * config.TasaActiva, 2)
            : disponible;

        var fila = await _db.Custodios.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.MonedaId == monedaId);
        if (fila is null)
        {
            _db.Custodios.Add(new Custodio
            {
                ClienteId = clienteId,
                MonedaId = monedaId,
                Disponible = disponible,
                DisponibleBs = disponibleBs,
                ActualizadoEn = DateTime.Now
            });
        }
        else
        {
            fila.Disponible = disponible;
            fila.DisponibleBs = disponibleBs;
            fila.ActualizadoEn = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        return disponible;
    }

    private async Task<decimal> CalcularDisponibleAsync(string clienteId, string? monedaId)
    {
        var q = _db.Compensaciones.AsNoTracking().Where(c => c.ClienteId == clienteId);
        if (monedaId is not null) q = q.Where(c => c.MonedaId == monedaId);
        var filas = await q.Select(c => new { c.Abonos, c.Retiros }).ToListAsync();
        return filas.Sum(f => f.Abonos) - filas.Sum(f => f.Retiros);
    }
}
