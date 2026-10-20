using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface ITransferenciaService
{
    Task<PagedResult<TransferenciaDto>> ListarAsync(TransferenciaFiltroDto filtro);
    Task<TransferenciaDto> ObtenerAsync(long id);
    Task<TransferenciaDto> CrearAsync(TransferenciaCrearDto dto);
    Task<TransferenciaDto> AnularAsync(long id, string? motivo);
}

/// <summary>Libro de transferencias (VFP: CTASTRAN) con anulación soft.</summary>
public sealed class TransferenciaService : ITransferenciaService
{
    private static readonly string[] OrigenesValidos = ["CO", "VE", "CU", "DP", "RT", "T1", "T2", "T3", "T4", "T5", "T6"];

    private readonly MonrysDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public TransferenciaService(MonrysDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<TransferenciaDto>> ListarAsync(TransferenciaFiltroDto f)
    {
        var q = _db.Transferencias.AsNoTracking().AsQueryable();

        if (f.Desde.HasValue) { var d = f.Desde.Value.Date; q = q.Where(t => t.Fecha >= d); }
        if (f.Hasta.HasValue) { var h = f.Hasta.Value.Date.AddDays(1); q = q.Where(t => t.Fecha < h); }
        if (!string.IsNullOrWhiteSpace(f.ClienteId)) q = q.Where(t => t.ClienteId == f.ClienteId);
        if (!string.IsNullOrWhiteSpace(f.Origen)) q = q.Where(t => t.Origen == f.Origen);
        if (!string.IsNullOrWhiteSpace(f.BancoId)) q = q.Where(t => t.BancoId == f.BancoId);
        if (!string.IsNullOrWhiteSpace(f.Estado)) q = q.Where(t => t.Estado == f.Estado);

        var total = await q.CountAsync();
        var filas = await q.OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
            .Skip((Math.Max(1, f.Pagina) - 1) * Math.Clamp(f.Tamano, 1, 200))
            .Take(Math.Clamp(f.Tamano, 1, 200))
            .ToListAsync();

        var (clientes, bancos) = await CargarDiccionariosAsync(filas);
        return new PagedResult<TransferenciaDto>
        {
            Total = total,
            Items = filas.Select(t => Mapear(t, clientes, bancos)).ToList()
        };
    }

    public async Task<TransferenciaDto> ObtenerAsync(long id)
    {
        var fila = await _db.Transferencias.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe la transferencia {id}.");
        var (clientes, bancos) = await CargarDiccionariosAsync([fila]);
        return Mapear(fila, clientes, bancos);
    }

    public async Task<TransferenciaDto> CrearAsync(TransferenciaCrearDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Origen) || !OrigenesValidos.Contains(dto.Origen.Trim().ToUpperInvariant()))
            throw new ValidacionException("Origen de transferencia inválido. Use CO, VE, CU, DP, RT o T1..T6.");
        if (dto.Monto <= 0)
            throw new ValidacionException("El monto debe ser mayor que cero.");

        if (!string.IsNullOrWhiteSpace(dto.ClienteId) &&
            !await _db.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
            throw new RecursoNoEncontradoException($"No existe el cliente {dto.ClienteId}.");

        if (!string.IsNullOrWhiteSpace(dto.BancoId) &&
            !await _db.Bancos.AnyAsync(b => b.Id == dto.BancoId))
            throw new RecursoNoEncontradoException($"No existe el banco {dto.BancoId}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fila = new Transferencia
        {
            Origen = dto.Origen.Trim().ToUpperInvariant(),
            TipoTran = dto.TipoTran,
            Fecha = dto.Fecha?.Date ?? DateTime.Today,
            NumeroTran = dto.NumeroTran,
            CuentaTran = dto.CuentaTran,
            Beneficiario = dto.Beneficiario,
            Monto = dto.Monto,
            ClienteId = dto.ClienteId,
            Motivo = dto.Motivo,
            CompraId = dto.CompraId,
            VentaId = dto.VentaId,
            MonedaId = dto.MonedaId,
            BancoId = dto.BancoId,
            CuentaClie = dto.CuentaClie,
            DeCaja = dto.DeCaja,
            Tasa = dto.Tasa,
            Usd = dto.Usd,
            MontoBs = dto.MontoBs,
            CodiIngresoId = dto.CodiIngresoId,
            CodiEgresoId = dto.CodiEgresoId,
            Estado = "ACTIVO"
        };

        _db.Transferencias.Add(fila);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            $"REGISTRA TRANSFERENCIA {fila.Origen} NRO. {fila.Id} MONTO {fila.Monto:0.00}",
            "transferencia", fila.Id.ToString(),
            new { fila.Origen, monto = fila.Monto, banco = fila.BancoId, cliente = fila.ClienteId });

        await tx.CommitAsync();
        return await ObtenerAsync(fila.Id);
    }

    public async Task<TransferenciaDto> AnularAsync(long id, string? motivo)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var fila = await _db.Transferencias.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe la transferencia {id}.");

        if (fila.Estado == "ANULADO")
            throw new ConflictoException($"La transferencia {id} ya está anulada.");

        var previo = new { fila.Origen, fila.Fecha, fila.Monto, fila.BancoId, fila.ClienteId, fila.Estado };

        fila.Estado = "ANULADO";
        fila.AnuladoEn = DateTime.Now;
        fila.AnuladoMotivo = string.IsNullOrWhiteSpace(motivo) ? "Sin motivo" : motivo.Trim();

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ANULA LA TRANSFERENCIA {fila.Origen} NRO. {id}",
            "transferencia", id.ToString(), previo, fila.AnuladoMotivo);

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    private async Task<(Dictionary<string, Cliente> clientes, Dictionary<string, Banco> bancos)>
        CargarDiccionariosAsync(IReadOnlyList<Transferencia> filas)
    {
        var clienteIds = filas.Select(t => t.ClienteId).Where(x => x != null).Select(x => x!).Distinct().ToList();
        var bancoIds = filas.Select(t => t.BancoId).Where(x => x != null).Select(x => x!).Distinct().ToList();

        var clientes = clienteIds.Count > 0
            ? await _db.Clientes.AsNoTracking().Where(c => clienteIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id)
            : new Dictionary<string, Cliente>();
        var bancos = bancoIds.Count > 0
            ? await _db.Bancos.AsNoTracking().Where(b => bancoIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id)
            : new Dictionary<string, Banco>();

        return (clientes, bancos);
    }

    private static TransferenciaDto Mapear(Transferencia t, Dictionary<string, Cliente> clientes, Dictionary<string, Banco> bancos)
    {
        clientes.TryGetValue(t.ClienteId ?? "", out var cliente);
        bancos.TryGetValue(t.BancoId ?? "", out var banco);

        return new TransferenciaDto
        {
            Id = t.Id,
            Origen = t.Origen,
            TipoTran = t.TipoTran,
            Fecha = t.Fecha,
            NumeroTran = t.NumeroTran,
            CuentaTran = t.CuentaTran,
            Beneficiario = t.Beneficiario,
            Monto = t.Monto,
            ClienteId = t.ClienteId,
            Cliente = cliente?.NombreCompleto,
            Motivo = t.Motivo,
            CompraId = t.CompraId,
            VentaId = t.VentaId,
            MonedaId = t.MonedaId,
            BancoId = t.BancoId,
            Banco = banco?.Nombre,
            CuentaClie = t.CuentaClie,
            DeCaja = t.DeCaja,
            Tasa = t.Tasa,
            Usd = t.Usd,
            MontoBs = t.MontoBs,
            CodiIngresoId = t.CodiIngresoId,
            CodiEgresoId = t.CodiEgresoId,
            Estado = t.Estado,
            AnuladoMotivo = t.AnuladoMotivo,
            AnuladoEn = t.AnuladoEn,
            CreadoEn = t.CreadoEn
        };
    }
}
