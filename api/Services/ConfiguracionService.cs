using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IConfiguracionService
{
    Task<ConfiguracionDto> ObtenerAsync();
    /// <summary>Actualización parcial: sólo los campos enviados (no nulos) se modifican.</summary>
    Task<ConfiguracionDto> ActualizarAsync(ConfiguracionActualizarDto dto);
    Task<CodificacionesDto> CodificacionesAsync();
    Task<(List<PlanContable> cuentas, Configuracion config)> PlanContableAsync();
}

/// <summary>Configuración global (VFP: DOLARMAIN / configuracion.scx) y codificaciones de caja.</summary>
public sealed class ConfiguracionService : IConfiguracionService
{
    private readonly MonrysDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public ConfiguracionService(MonrysDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<ConfiguracionDto> ObtenerAsync()
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");
        return Mapear(config);
    }

    public async Task<ConfiguracionDto> ActualizarAsync(ConfiguracionActualizarDto dto)
    {
        var config = await _db.Configuraciones.FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");

        var cambios = new Dictionary<string, object?>();
        void Set(string campo, object? nuevo, object? actual)
        {
            if (nuevo is not null && !Equals(nuevo, actual)) cambios[campo] = new { anterior = actual, nuevo };
        }

        Set("nombreEmpresa", dto.NombreEmpresa, config.NombreEmpresa);
        Set("direccion", dto.Direccion, config.Direccion);
        Set("telefono", dto.Telefono, config.Telefono);
        Set("fax", dto.Fax, config.Fax);
        Set("rif", dto.Rif, config.Rif);
        Set("iva", dto.Iva, config.Iva);
        Set("agenteRetencion", dto.AgenteRetencion, config.AgenteRetencion);
        Set("serialContable", dto.SerialContable, config.SerialContable);
        Set("monedaPrincipalId", dto.MonedaPrincipalId, config.MonedaPrincipalId);
        Set("monedaNacionalId", dto.MonedaNacionalId, config.MonedaNacionalId);
        Set("tasaActiva", dto.TasaActiva, config.TasaActiva);
        Set("tasaCompra", dto.TasaCompra, config.TasaCompra);
        Set("tasaVenta", dto.TasaVenta, config.TasaVenta);
        Set("version", dto.Version, config.Version);
        Set("nroCliente", dto.NroCliente, config.NroCliente);

        if (cambios.Count == 0)
            throw new ValidacionException("No existen cambios a realizar.");

        if (dto.TasaActiva is <= 0 || dto.TasaCompra is <= 0 || dto.TasaVenta is <= 0)
            throw new ValidacionException("Las tasas deben ser mayores que cero.");

        // Las monedas referenciadas deben existir: el origen VFP (DOLARMAIN.ID_MONY2)
        // traía '0003', que no está en TIPOMONY, y rompería las FK de compras/ventas.
        if (dto.MonedaPrincipalId is not null &&
            !await _db.TipoMonedas.AsNoTracking().AnyAsync(m => m.Id == dto.MonedaPrincipalId))
            throw new ValidacionException($"La moneda principal '{dto.MonedaPrincipalId}' no existe.");
        if (dto.MonedaNacionalId is not null &&
            !await _db.TipoMonedas.AsNoTracking().AnyAsync(m => m.Id == dto.MonedaNacionalId))
            throw new ValidacionException($"La moneda nacional '{dto.MonedaNacionalId}' no existe.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (dto.NombreEmpresa is not null) config.NombreEmpresa = dto.NombreEmpresa;
        if (dto.Direccion is not null) config.Direccion = dto.Direccion;
        if (dto.Telefono is not null) config.Telefono = dto.Telefono;
        if (dto.Fax is not null) config.Fax = dto.Fax;
        if (dto.Rif is not null) config.Rif = dto.Rif;
        if (dto.Iva.HasValue) config.Iva = dto.Iva.Value;
        if (dto.AgenteRetencion.HasValue) config.AgenteRetencion = dto.AgenteRetencion.Value;
        if (dto.SerialContable is not null) config.SerialContable = dto.SerialContable;
        if (dto.MonedaPrincipalId is not null) config.MonedaPrincipalId = dto.MonedaPrincipalId;
        if (dto.MonedaNacionalId is not null) config.MonedaNacionalId = dto.MonedaNacionalId;
        if (dto.TasaActiva.HasValue) config.TasaActiva = dto.TasaActiva.Value;
        if (dto.TasaCompra.HasValue) config.TasaCompra = dto.TasaCompra.Value;
        if (dto.TasaVenta.HasValue) config.TasaVenta = dto.TasaVenta.Value;
        if (dto.Version is not null) config.Version = dto.Version;
        if (dto.NroCliente.HasValue) config.NroCliente = dto.NroCliente.Value;
        config.ActualizadoEn = DateTime.Now;

        await _auditoria.RegistrarAsync("MODIFICA LA CONFIGURACIÓN DEL SISTEMA", "configuracion",
            config.Id.ToString(), cambios);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Mapear(config);
    }

    public async Task<CodificacionesDto> CodificacionesAsync()
    {
        var ingresos = await _db.CodificacionesIngreso.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        var egresos = await _db.CodificacionesEgreso.AsNoTracking().OrderBy(c => c.Id).ToListAsync();

        return new CodificacionesDto
        {
            Ingresos = ingresos.Select(c => new CodificacionOpcionDto
            {
                Id = c.Id, Descripcion = c.Descripcion, CuentaContable = c.CuentaContable
            }).ToList(),
            Egresos = egresos.Select(c => new CodificacionOpcionDto
            {
                Id = c.Id, Descripcion = c.Descripcion, CuentaContable = c.CuentaContable
            }).ToList()
        };
    }

    public async Task<(List<PlanContable> cuentas, Configuracion config)> PlanContableAsync()
    {
        var cuentas = await _db.PlanContable.AsNoTracking().OrderBy(c => c.Cuenta).ToListAsync();
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");
        return (cuentas, config);
    }

    private static ConfiguracionDto Mapear(Configuracion c) => new()
    {
        Id = c.Id,
        NroCliente = c.NroCliente,
        NombreEmpresa = c.NombreEmpresa,
        Direccion = c.Direccion,
        Telefono = c.Telefono,
        Fax = c.Fax,
        Rif = c.Rif,
        Iva = c.Iva,
        AgenteRetencion = c.AgenteRetencion,
        SerialContable = c.SerialContable,
        MonedaPrincipalId = c.MonedaPrincipalId,
        MonedaNacionalId = c.MonedaNacionalId,
        TasaActiva = c.TasaActiva,
        TasaCompra = c.TasaCompra,
        TasaVenta = c.TasaVenta,
        Version = c.Version,
        ActualizadoEn = c.ActualizadoEn
    };
}
