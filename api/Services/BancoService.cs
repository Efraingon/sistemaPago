using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IBancoService
{
    Task<List<BancoListaDto>> ListarAsync(string? buscar, bool? activo);
    Task<BancoListaDto> ObtenerAsync(string id);
    Task<BancoListaDto> CrearAsync(BancoCrearDto dto);
    Task<BancoListaDto> ActualizarAsync(string id, BancoCrearDto dto);
    Task EliminarAsync(string id);

    Task<List<CuentaBancoDto>> ListarCuentasAsync(string bancoId);
    Task<CuentaBancoDto> CrearCuentaAsync(string bancoId, CuentaBancoCrearDto dto);
    Task<CuentaBancoDto> ActualizarCuentaAsync(string bancoId, long cuentaId, CuentaBancoCrearDto dto);
    Task EliminarCuentaAsync(string bancoId, long cuentaId);
    Task<List<CuentaBancoDto>> ListarTodasLasCuentasAsync();
}

/// <summary>Bancos y cuentas propias de la casa (VFP: nuevobanco / ctasbanco / eliminacuenta).</summary>
public sealed class BancoService : IBancoService
{
    private readonly MonrysDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public BancoService(MonrysDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<List<BancoListaDto>> ListarAsync(string? buscar, bool? activo)
    {
        var q = _db.Bancos.AsNoTracking().AsQueryable();
        if (activo.HasValue) q = q.Where(b => b.Activo == activo.Value);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim().ToLower();
            q = q.Where(b => b.Nombre.ToLower().Contains(t) || b.Id.ToLower().Contains(t));
        }

        var filas = await q.OrderBy(b => b.Nacional).ThenBy(b => b.Nombre).ToListAsync();
        var cuentas = await _db.CuentasBanco.AsNoTracking().ToListAsync();

        return filas.Select(b => new BancoListaDto
        {
            Id = b.Id,
            Nombre = b.Nombre,
            Direccion = b.Direccion,
            Telefonos = b.Telefonos,
            Correo = b.Correo,
            Contacto = b.Contacto,
            Nacional = b.Nacional,
            Saldo = b.Saldo,
            CuentaContable = b.CuentaContable,
            Activo = b.Activo,
            Cuentas = cuentas.Where(c => c.BancoId == b.Id).Select(MapearCuenta).ToList()
        }).ToList();
    }

    public async Task<BancoListaDto> ObtenerAsync(string id)
    {
        var banco = await _db.Bancos.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el banco {id}.");
        var cuentas = await _db.CuentasBanco.AsNoTracking().Where(c => c.BancoId == id).ToListAsync();

        return new BancoListaDto
        {
            Id = banco.Id,
            Nombre = banco.Nombre,
            Direccion = banco.Direccion,
            Telefonos = banco.Telefonos,
            Correo = banco.Correo,
            Contacto = banco.Contacto,
            Nacional = banco.Nacional,
            Saldo = banco.Saldo,
            CuentaContable = banco.CuentaContable,
            Activo = banco.Activo,
            Cuentas = cuentas.Select(MapearCuenta).ToList()
        };
    }

    public async Task<BancoListaDto> CrearAsync(BancoCrearDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidacionException("Debe indicar el nombre del banco.");
        if (dto.Nacional is not (1 or 2))
            throw new ValidacionException("El indicador nacional debe ser 1 (nacional) o 2 (internacional).");

        var id = string.IsNullOrWhiteSpace(dto.Id)
            ? await GenerarIdBancoAsync()
            : dto.Id.Trim();
        if (await _db.Bancos.AnyAsync(b => b.Id == id))
            throw new ConflictoException($"Ya existe un banco con id {id}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var banco = new Banco
        {
            Id = id,
            Nombre = dto.Nombre.Trim(),
            Direccion = dto.Direccion,
            Telefonos = dto.Telefonos,
            Fax = dto.Fax,
            Correo = dto.Correo,
            Contacto = dto.Contacto,
            Nacional = (short)dto.Nacional,
            CuentaContable = dto.CuentaContable,
            Activo = dto.Activo,
            Saldo = 0m
        };
        _db.Bancos.Add(banco);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync($"CREA EL BANCO {banco.Nombre}", "banco", banco.Id, new { banco.Id, banco.Nombre });
        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task<BancoListaDto> ActualizarAsync(string id, BancoCrearDto dto)
    {
        var banco = await _db.Bancos.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el banco {id}.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidacionException("Debe indicar el nombre del banco.");
        if (dto.Nacional is not (1 or 2))
            throw new ValidacionException("El indicador nacional debe ser 1 (nacional) o 2 (internacional).");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { banco.Nombre, banco.Nacional, banco.Activo };

        banco.Nombre = dto.Nombre.Trim();
        banco.Direccion = dto.Direccion;
        banco.Telefonos = dto.Telefonos;
        banco.Fax = dto.Fax;
        banco.Correo = dto.Correo;
        banco.Contacto = dto.Contacto;
        banco.Nacional = (short)dto.Nacional;
        banco.CuentaContable = dto.CuentaContable;
        banco.Activo = dto.Activo;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"MODIFICA EL BANCO {banco.Nombre}", "banco", banco.Id, previo);
        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(string id)
    {
        var banco = await _db.Bancos.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el banco {id}.");
        if (await _db.CuentasBanco.AnyAsync(c => c.BancoId == id))
            throw new ConflictoException("El banco tiene cuentas asociadas: no puede eliminarse (desactívelo).");
        if (await _db.MovimientosCaja.AnyAsync(c => c.BancoId == id) ||
            await _db.Transferencias.AnyAsync(t => t.BancoId == id) ||
            await _db.CuentasCliente.AnyAsync(c => c.BancoId == id))
            throw new ConflictoException("El banco está en uso: no puede eliminarse (desactívelo).");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { banco.Id, banco.Nombre };
        _db.Bancos.Remove(banco);
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ELIMINA EL BANCO {previo.Nombre}", "banco", id, previo);
        await tx.CommitAsync();
    }

    // ------------------------------------------------------------------
    // CUENTAS PROPIAS (VFP: CTAS / CTASBANCO)
    // ------------------------------------------------------------------
    public async Task<List<CuentaBancoDto>> ListarCuentasAsync(string bancoId)
    {
        await ExigirBancoAsync(bancoId);
        return (await _db.CuentasBanco.AsNoTracking().Where(c => c.BancoId == bancoId).OrderBy(c => c.NumeroCuenta).ToListAsync())
            .Select(MapearCuenta).ToList();
    }

    public async Task<List<CuentaBancoDto>> ListarTodasLasCuentasAsync()
        => (await _db.CuentasBanco.AsNoTracking().OrderBy(c => c.BancoId).ThenBy(c => c.NumeroCuenta).ToListAsync())
            .Select(MapearCuenta).ToList();

    public async Task<CuentaBancoDto> CrearCuentaAsync(string bancoId, CuentaBancoCrearDto dto)
    {
        await ExigirBancoAsync(bancoId);
        var numero = ValidarNumeroCuenta(dto.NumeroCuenta);

        if (await _db.CuentasBanco.AnyAsync(c => c.NumeroCuenta == numero))
            throw new ConflictoException($"La cuenta {numero} ya existe en otro banco.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var cuenta = new CuentaBanco
        {
            BancoId = bancoId,
            NumeroCuenta = numero,
            Tipo = dto.Tipo,
            CuentaContable = dto.CuentaContable,
            Activa = dto.Activa
        };
        _db.CuentasBanco.Add(cuenta);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync($"AGREGA LA CUENTA {numero} AL BANCO {bancoId}", "cuenta_banco",
            cuenta.Id.ToString(), new { bancoId, numero });

        await tx.CommitAsync();
        return MapearCuenta(cuenta);
    }

    public async Task<CuentaBancoDto> ActualizarCuentaAsync(string bancoId, long cuentaId, CuentaBancoCrearDto dto)
    {
        var cuenta = await _db.CuentasBanco.FirstOrDefaultAsync(c => c.Id == cuentaId && c.BancoId == bancoId)
            ?? throw new RecursoNoEncontradoException($"No existe la cuenta {cuentaId} del banco {bancoId}.");
        var numero = ValidarNumeroCuenta(dto.NumeroCuenta);

        if (await _db.CuentasBanco.AnyAsync(c => c.Id != cuentaId && c.NumeroCuenta == numero))
            throw new ConflictoException($"La cuenta {numero} ya existe en otro banco.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { cuenta.NumeroCuenta, cuenta.Activa, cuenta.Saldo };

        cuenta.NumeroCuenta = numero;
        cuenta.Tipo = dto.Tipo;
        cuenta.CuentaContable = dto.CuentaContable;
        cuenta.Activa = dto.Activa;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"MODIFICA LA CUENTA {numero} DEL BANCO {bancoId}", "cuenta_banco",
            cuenta.Id.ToString(), previo);
        await tx.CommitAsync();

        return MapearCuenta(cuenta);
    }

    public async Task EliminarCuentaAsync(string bancoId, long cuentaId)
    {
        var cuenta = await _db.CuentasBanco.FirstOrDefaultAsync(c => c.Id == cuentaId && c.BancoId == bancoId)
            ?? throw new RecursoNoEncontradoException($"No existe la cuenta {cuentaId} del banco {bancoId}.");

        if (await _db.MovimientosCaja.AnyAsync(c => c.CuentaTran == cuenta.NumeroCuenta && c.Estado == "ACTIVO") ||
            await _db.Transferencias.AnyAsync(t => t.CuentaTran == cuenta.NumeroCuenta && t.Estado == "ACTIVO") ||
            await _db.OperacionesRecepcion.AnyAsync(r => r.CtaCredito == cuenta.NumeroCuenta || r.CtaDebito == cuenta.NumeroCuenta) ||
            await _db.OperacionesPago.AnyAsync(r => r.CtaCredito == cuenta.NumeroCuenta || r.CtaDebito == cuenta.NumeroCuenta))
            throw new ConflictoException("La cuenta está en uso en transacciones de banco, caja u operaciones: no puede eliminarse.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { cuenta.NumeroCuenta, cuenta.Saldo };
        _db.CuentasBanco.Remove(cuenta);
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ELIMINA LA CUENTA {previo.NumeroCuenta} DEL BANCO {bancoId}",
            "cuenta_banco", cuentaId.ToString(), previo);
        await tx.CommitAsync();
    }

    // ------------------------------------------------------------------
    private async Task ExigirBancoAsync(string id)
    {
        if (!await _db.Bancos.AnyAsync(b => b.Id == id))
            throw new RecursoNoEncontradoException($"No existe el banco {id}.");
    }

    private async Task<string> GenerarIdBancoAsync()
    {
        for (var i = 1; i < 100000; i++)
        {
            var id = i.ToString("D5");
            if (!await _db.Bancos.AnyAsync(b => b.Id == id)) return id;
        }
        throw new ValidacionException("No fue posible generar un id de banco disponible.");
    }

    private static string ValidarNumeroCuenta(string? numero)
    {
        var limpio = new string((numero ?? "").Where(char.IsDigit).ToArray());
        if (limpio.Length == 0)
            throw new ValidacionException("Debe indicar el número de cuenta (sólo dígitos).");
        if (limpio.Length > 20)
            throw new ValidacionException("El número de cuenta no puede exceder 20 dígitos.");
        return limpio;
    }

    private static CuentaBancoDto MapearCuenta(CuentaBanco c) => new()
    {
        Id = c.Id,
        BancoId = c.BancoId,
        NumeroCuenta = c.NumeroCuenta,
        Tipo = c.Tipo,
        Debitos = c.Debitos,
        Creditos = c.Creditos,
        Saldo = c.Saldo,
        CuentaContable = c.CuentaContable,
        Activa = c.Activa
    };
}
