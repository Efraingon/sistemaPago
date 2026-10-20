using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IClienteService
{
    Task<PagedResult<ClienteListaDto>> ListarAsync(string? buscar, bool? activo, int pagina, int tamano);
    Task<ClienteDetalleDto> ObtenerAsync(string id);
    Task<ClienteDetalleDto> CrearAsync(ClienteCrearDto dto);
    Task<ClienteDetalleDto> ActualizarAsync(string id, ClienteCrearDto dto);
    /// <summary>Borrado lógico: marca el cliente como inactivo.</summary>
    Task<ClienteDetalleDto> EliminarAsync(string id);

    Task<List<CuentaClienteDto>> ListarCuentasAsync(string clienteId);
    Task<CuentaClienteDto> CrearCuentaAsync(string clienteId, CuentaClienteCrearDto dto);
    Task<CuentaClienteDto> ActualizarCuentaAsync(string clienteId, long cuentaId, CuentaClienteCrearDto dto);
    Task EliminarCuentaAsync(string clienteId, long cuentaId);

    Task<CentroCostoDto> CrearCentroAsync(string clienteId, CentroCostoCrearDto dto);
    Task EliminarCentroAsync(string clienteId, long centroId);
}

/// <summary>Maestro de clientes (VFP: clieingreso / cliemodifica / listaclie) + cuentas y centros de costo.</summary>
public sealed class ClienteService : IClienteService
{
    private readonly MonrysDbContext _db;
    private readonly ICorrelativoService _correlativos;
    private readonly IAuditoriaService _auditoria;

    public ClienteService(MonrysDbContext db, ICorrelativoService correlativos, IAuditoriaService auditoria)
    {
        _db = db;
        _correlativos = correlativos;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<ClienteListaDto>> ListarAsync(string? buscar, bool? activo, int pagina, int tamano)
    {
        var q = _db.Clientes.AsNoTracking().AsQueryable();
        if (activo.HasValue) q = q.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim().ToLower();
            q = q.Where(c =>
                (c.RazonSocial != null && c.RazonSocial.ToLower().Contains(t)) ||
                (c.Nombres != null && c.Nombres.ToLower().Contains(t)) ||
                (c.Apellidos != null && c.Apellidos.ToLower().Contains(t)) ||
                (c.Cedula != null && c.Cedula.ToLower().Contains(t)) ||
                (c.Rif != null && c.Rif.ToLower().Contains(t)) ||
                c.Id.ToLower().Contains(t));
        }

        var total = await q.CountAsync();
        var filas = await q.OrderBy(c => c.RazonSocial).ThenBy(c => c.Apellidos).ThenBy(c => c.Nombres)
            .Skip((Math.Max(1, pagina) - 1) * Math.Clamp(tamano, 1, 200))
            .Take(Math.Clamp(tamano, 1, 200))
            .ToListAsync();

        return new PagedResult<ClienteListaDto> { Total = total, Items = filas.Select(Mapear).ToList() };
    }

    public async Task<ClienteDetalleDto> ObtenerAsync(string id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {id}.");
        return await CargarDetalleAsync(cliente);
    }

    public async Task<ClienteDetalleDto> CrearAsync(ClienteCrearDto dto)
    {
        Validar(dto);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var id = await _correlativos.SiguienteIdAsync("CLIENTE", CorrelativoService.EjercicioGlobal, 10);
        var cliente = new Cliente
        {
            Id = id,
            Persona = dto.Persona,
            RazonSocial = dto.RazonSocial,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Rif = dto.Rif,
            Cedula = dto.Cedula,
            Direccion = dto.Direccion,
            Telefonos = dto.Telefonos,
            Email = dto.Email,
            Observaciones = dto.Observaciones,
            CuentaContable = dto.CuentaContable,
            Activo = dto.Activo,
            CreadoEn = DateTime.Now,
            ActualizadoEn = DateTime.Now
        };
        _db.Clientes.Add(cliente);

        // El alta crea el centro de costo principal (VFP: clieingreso).
        _db.CentroCostos.Add(new CentroCosto
        {
            ClienteId = id,
            Descripcion = dto.Persona == 2
                ? (dto.RazonSocial ?? "CENTRO PRINCIPAL")
                : $"{dto.Nombres} {dto.Apellidos}".Trim(),
            Principal = true
        });

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"CREA EL CLIENTE {id} {cliente.NombreCompleto}", "cliente", id,
            new { id, dto.Persona, dto.Rif, dto.Cedula });

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task<ClienteDetalleDto> ActualizarAsync(string id, ClienteCrearDto dto)
    {
        Validar(dto);
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {id}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new { cliente.Persona, cliente.RazonSocial, cliente.Nombres, cliente.Apellidos, cliente.Rif, cliente.Cedula, cliente.Activo };

        cliente.Persona = dto.Persona;
        cliente.RazonSocial = dto.RazonSocial;
        cliente.Nombres = dto.Nombres;
        cliente.Apellidos = dto.Apellidos;
        cliente.Rif = dto.Rif;
        cliente.Cedula = dto.Cedula;
        cliente.Direccion = dto.Direccion;
        cliente.Telefonos = dto.Telefonos;
        cliente.Email = dto.Email;
        cliente.Observaciones = dto.Observaciones;
        cliente.CuentaContable = dto.CuentaContable;
        cliente.Activo = dto.Activo;
        cliente.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"MODIFICA EL CLIENTE {id}", "cliente", id, previo);

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task<ClienteDetalleDto> EliminarAsync(string id)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {id}.");

        if (!cliente.Activo)
            throw new ConflictoException($"El cliente {id} ya está inactivo.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new { cliente.Activo };
        cliente.Activo = false;
        cliente.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"DESACTIVA EL CLIENTE {id} {cliente.NombreCompleto}", "cliente", id, previo);

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    // ------------------------------------------------------------------
    // CUENTAS BANCARIAS DEL CLIENTE (VFP: ctasclie)
    // ------------------------------------------------------------------
    public async Task<List<CuentaClienteDto>> ListarCuentasAsync(string clienteId)
    {
        await ExigirClienteAsync(clienteId);
        var cuentas = await _db.CuentasCliente.AsNoTracking()
            .Where(c => c.ClienteId == clienteId).OrderBy(c => c.BancoId).ToListAsync();
        var bancos = await _db.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id);
        return cuentas.Select(c =>
        {
            bancos.TryGetValue(c.BancoId, out var banco);
            return new CuentaClienteDto
            {
                Id = c.Id,
                BancoId = c.BancoId,
                BancoNombre = banco?.Nombre,
                ClienteId = c.ClienteId,
                NumeroCuenta = c.NumeroCuenta,
                CuentaContable = c.CuentaContable,
                Activa = c.Activa
            };
        }).ToList();
    }

    public async Task<CuentaClienteDto> CrearCuentaAsync(string clienteId, CuentaClienteCrearDto dto)
    {
        await ExigirClienteAsync(clienteId);
        var numero = ValidarNumeroCuenta(dto.NumeroCuenta);
        await ExigirBancoAsync(dto.BancoId);

        if (await _db.CuentasCliente.AnyAsync(c =>
                c.BancoId == dto.BancoId && c.ClienteId == clienteId && c.NumeroCuenta == numero))
            throw new ConflictoException($"Ya existe la cuenta {numero} del banco {dto.BancoId} para este cliente.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var cuenta = new CuentaCliente
        {
            BancoId = dto.BancoId,
            ClienteId = clienteId,
            NumeroCuenta = numero,
            CuentaContable = dto.CuentaContable,
            Activa = dto.Activa
        };
        _db.CuentasCliente.Add(cuenta);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync($"AGREGA CUENTA {numero} AL CLIENTE {clienteId}", "cuenta_cliente",
            cuenta.Id.ToString(), new { banco = dto.BancoId, numero });

        await tx.CommitAsync();
        return new CuentaClienteDto
        {
            Id = cuenta.Id,
            BancoId = cuenta.BancoId,
            ClienteId = cuenta.ClienteId,
            NumeroCuenta = cuenta.NumeroCuenta,
            CuentaContable = cuenta.CuentaContable,
            Activa = cuenta.Activa
        };
    }

    public async Task<CuentaClienteDto> ActualizarCuentaAsync(string clienteId, long cuentaId, CuentaClienteCrearDto dto)
    {
        var cuenta = await _db.CuentasCliente.FirstOrDefaultAsync(c => c.Id == cuentaId && c.ClienteId == clienteId)
            ?? throw new RecursoNoEncontradoException($"No existe la cuenta {cuentaId} del cliente {clienteId}.");

        var numero = ValidarNumeroCuenta(dto.NumeroCuenta);
        await ExigirBancoAsync(dto.BancoId);

        if (await _db.CuentasCliente.AnyAsync(c =>
                c.Id != cuentaId && c.BancoId == dto.BancoId && c.ClienteId == clienteId && c.NumeroCuenta == numero))
            throw new ConflictoException($"Ya existe la cuenta {numero} del banco {dto.BancoId} para este cliente.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { cuenta.BancoId, cuenta.NumeroCuenta, cuenta.Activa };

        cuenta.BancoId = dto.BancoId;
        cuenta.NumeroCuenta = numero;
        cuenta.CuentaContable = dto.CuentaContable;
        cuenta.Activa = dto.Activa;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"MODIFICA CUENTA {numero} DEL CLIENTE {clienteId}", "cuenta_cliente",
            cuenta.Id.ToString(), previo);
        await tx.CommitAsync();

        return new CuentaClienteDto
        {
            Id = cuenta.Id,
            BancoId = cuenta.BancoId,
            ClienteId = cuenta.ClienteId,
            NumeroCuenta = cuenta.NumeroCuenta,
            CuentaContable = cuenta.CuentaContable,
            Activa = cuenta.Activa
        };
    }

    public async Task EliminarCuentaAsync(string clienteId, long cuentaId)
    {
        var cuenta = await _db.CuentasCliente.FirstOrDefaultAsync(c => c.Id == cuentaId && c.ClienteId == clienteId)
            ?? throw new RecursoNoEncontradoException($"No existe la cuenta {cuentaId} del cliente {clienteId}.");

        if (await EnUsoAsync(cuenta.NumeroCuenta))
            throw new ConflictoException("La cuenta está en uso en movimientos de caja, transferencias u operaciones: no puede eliminarse.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { cuenta.BancoId, cuenta.NumeroCuenta };
        _db.CuentasCliente.Remove(cuenta);
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ELIMINA CUENTA {previo.NumeroCuenta} DEL CLIENTE {clienteId}",
            "cuenta_cliente", cuentaId.ToString(), previo);
        await tx.CommitAsync();
    }

    private async Task<bool> EnUsoAsync(string numero)
    {
        if (await _db.MovimientosCaja.AnyAsync(c => c.CuentaTran == numero && c.Estado == "ACTIVO")) return true;
        if (await _db.Transferencias.AnyAsync(t => t.CuentaClie == numero && t.Estado == "ACTIVO")) return true;
        if (await _db.OperacionesRecepcion.AnyAsync(r => r.CtaCredito == numero || r.CtaDebito == numero)) return true;
        if (await _db.OperacionesPago.AnyAsync(r => r.CtaCredito == numero || r.CtaDebito == numero)) return true;
        return false;
    }

    // ------------------------------------------------------------------
    // CENTROS DE COSTO
    // ------------------------------------------------------------------
    public async Task<CentroCostoDto> CrearCentroAsync(string clienteId, CentroCostoCrearDto dto)
    {
        await ExigirClienteAsync(clienteId);
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            throw new ValidacionException("Debe indicar la descripción del centro de costo.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (dto.Principal && await _db.CentroCostos.AnyAsync(c => c.ClienteId == clienteId && c.Principal))
            throw new ConflictoException("El cliente ya tiene un centro de costo principal.");

        var centro = new CentroCosto
        {
            ClienteId = clienteId,
            Descripcion = dto.Descripcion.Trim(),
            Principal = dto.Principal
        };
        _db.CentroCostos.Add(centro);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync($"AGREGA CENTRO DE COSTO {centro.Descripcion} AL CLIENTE {clienteId}",
            "centro_costo", centro.Id.ToString());
        await tx.CommitAsync();

        return new CentroCostoDto { Id = centro.Id, ClienteId = clienteId, Descripcion = centro.Descripcion, Principal = centro.Principal };
    }

    public async Task EliminarCentroAsync(string clienteId, long centroId)
    {
        var centro = await _db.CentroCostos.FirstOrDefaultAsync(c => c.Id == centroId && c.ClienteId == clienteId)
            ?? throw new RecursoNoEncontradoException($"No existe el centro de costo {centroId} del cliente {clienteId}.");
        if (centro.Principal)
            throw new ValidacionException("No se puede eliminar el centro de costo principal.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { centro.Descripcion, centro.Principal };
        _db.CentroCostos.Remove(centro);
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ELIMINA CENTRO DE COSTO {centro.Descripcion}", "centro_costo",
            centroId.ToString(), previo);
        await tx.CommitAsync();
    }

    // ------------------------------------------------------------------
    // AUXILIARES
    // ------------------------------------------------------------------
    private async Task<Cliente> ExigirClienteAsync(string id)
        => await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {id}.");

    private async Task ExigirBancoAsync(string bancoId)
    {
        if (string.IsNullOrWhiteSpace(bancoId))
            throw new ValidacionException("Debe indicar el banco.");
        if (!await _db.Bancos.AnyAsync(b => b.Id == bancoId && b.Activo))
            throw new RecursoNoEncontradoException($"No existe el banco {bancoId}.");
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

    private static void Validar(ClienteCrearDto dto)
    {
        if (dto.Persona is not (1 or 2))
            throw new ValidacionException("El tipo de persona debe ser 1 (natural) o 2 (jurídica).");
        if (string.IsNullOrWhiteSpace(dto.RazonSocial) &&
            string.IsNullOrWhiteSpace(dto.Nombres) &&
            string.IsNullOrWhiteSpace(dto.Apellidos))
            throw new ValidacionException("Debe indicar la razón social o los nombres y apellidos del cliente.");
    }

    private async Task<ClienteDetalleDto> CargarDetalleAsync(Cliente cliente)
    {
        var centros = await _db.CentroCostos.AsNoTracking().Where(c => c.ClienteId == cliente.Id).ToListAsync();
        var cuentas = await _db.CuentasCliente.AsNoTracking().Where(c => c.ClienteId == cliente.Id).ToListAsync();
        var bancos = await _db.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id);

        var dto = Mapear(cliente);
        return new ClienteDetalleDto
        {
            Id = dto.Id,
            Nombre = dto.Nombre,
            RazonSocial = dto.RazonSocial,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Persona = dto.Persona,
            Rif = dto.Rif,
            Cedula = dto.Cedula,
            Telefonos = dto.Telefonos,
            Email = dto.Email,
            Activo = dto.Activo,
            CreadoEn = dto.CreadoEn,
            Direccion = cliente.Direccion,
            Observaciones = cliente.Observaciones,
            CuentaContable = cliente.CuentaContable,
            ActualizadoEn = cliente.ActualizadoEn,
            Centros = centros.Select(c => new CentroCostoDto
            {
                Id = c.Id, ClienteId = c.ClienteId, Descripcion = c.Descripcion, Principal = c.Principal
            }).ToList(),
            Cuentas = cuentas.Select(c =>
            {
                bancos.TryGetValue(c.BancoId, out var banco);
                return new CuentaClienteDto
                {
                    Id = c.Id,
                    BancoId = c.BancoId,
                    BancoNombre = banco?.Nombre,
                    ClienteId = c.ClienteId,
                    NumeroCuenta = c.NumeroCuenta,
                    CuentaContable = c.CuentaContable,
                    Activa = c.Activa
                };
            }).ToList()
        };
    }

    private static ClienteListaDto Mapear(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.NombreCompleto,
        RazonSocial = c.RazonSocial,
        Nombres = c.Nombres,
        Apellidos = c.Apellidos,
        Persona = c.Persona,
        Rif = c.Rif,
        Cedula = c.Cedula,
        Telefonos = c.Telefonos,
        Email = c.Email,
        Activo = c.Activo,
        CreadoEn = c.CreadoEn
    };
}
