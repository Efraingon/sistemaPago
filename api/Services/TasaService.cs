using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface ITasaService
{
    Task<TasaDto> ObtenerAsync();
    Task<TasaDto> ActualizarAsync(TasaActualizarDto dto);
    Task<List<CalendTasaDto>> CalendarioAsync(DateTime? desde, DateTime? hasta);
    Task<List<MonedaDto>> ListarMonedasAsync();
}

/// <summary>
/// Parámetros de tasa globales (VFP: modifica_tasas.scx / tasacalendario.scx).
/// Tres tasas no intercambiables: activa (referencia), compra (default compras) y
/// venta (default ventas); al actualizar se propaga al calendario del día.
/// </summary>
public sealed class TasaService : ITasaService
{
    private readonly MonrysDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public TasaService(MonrysDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<TasaDto> ObtenerAsync()
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");
        return new TasaDto
        {
            TasaActiva = config.TasaActiva,
            TasaCompra = config.TasaCompra,
            TasaVenta = config.TasaVenta,
            MonedaPrincipalId = config.MonedaPrincipalId,
            MonedaNacionalId = config.MonedaNacionalId,
            ActualizadoEn = config.ActualizadoEn
        };
    }

    public async Task<TasaDto> ActualizarAsync(TasaActualizarDto dto)
    {
        if (dto.TasaActiva <= 0 || dto.TasaCompra <= 0 || dto.TasaVenta <= 0)
            throw new ValidacionException("Las tres tasas (activa, compra y venta) deben ser mayores que cero.");

        if (dto.TasaVenta < dto.TasaCompra)
            throw new ValidacionException("La tasa de venta no puede ser menor que la tasa de compra.");

        var config = await _db.Configuraciones.FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");

        var sinCambios = config.TasaActiva == dto.TasaActiva &&
                         config.TasaCompra == dto.TasaCompra &&
                         config.TasaVenta == dto.TasaVenta;
        if (sinCambios)
            throw new ValidacionException("No existen cambios a realizar.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new
        {
            tasaActiva = config.TasaActiva,
            tasaCompra = config.TasaCompra,
            tasaVenta = config.TasaVenta
        };

        config.TasaActiva = dto.TasaActiva;
        config.TasaCompra = dto.TasaCompra;
        config.TasaVenta = dto.TasaVenta;
        config.ActualizadoEn = DateTime.Now;

        if (dto.ActualizarCalendario)
            await ActualizarCalendarioDelDiaAsync(dto);

        await _auditoria.RegistrarAsync(
            $"MODIFICA TASAS ANTES: ACTIVA {previo.tasaActiva:0.00} COMPRA {previo.tasaCompra:0.00} VENTA {previo.tasaVenta:0.00} " +
            $"AHORA: ACTIVA {dto.TasaActiva:0.00} COMPRA {dto.TasaCompra:0.00} VENTA {dto.TasaVenta:0.00}",
            "configuracion", config.Id.ToString(), previo);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return await ObtenerAsync();
    }

    private async Task ActualizarCalendarioDelDiaAsync(TasaActualizarDto dto)
    {
        var hoy = DateTime.Today;
        var registro = await _db.CalendTasas.FirstOrDefaultAsync(c => c.Fecha == hoy);
        if (registro is null)
        {
            _db.CalendTasas.Add(new CalendTasa
            {
                Fecha = hoy,
                NombreDia = hoy.ToString("dddd").ToUpperInvariant()[..3],
                Tasa = dto.TasaActiva,
                TasaCompra = dto.TasaCompra,
                TasaVenta = dto.TasaVenta
            });
        }
        else
        {
            registro.Tasa = dto.TasaActiva;
            registro.TasaCompra = dto.TasaCompra;
            registro.TasaVenta = dto.TasaVenta;
            registro.NombreDia = hoy.ToString("dddd").ToUpperInvariant()[..3];
        }
    }

    public async Task<List<CalendTasaDto>> CalendarioAsync(DateTime? desde, DateTime? hasta)
    {
        var q = _db.CalendTasas.AsNoTracking().AsQueryable();
        if (desde.HasValue) { var d = desde.Value.Date; q = q.Where(c => c.Fecha >= d); }
        if (hasta.HasValue) { var h = hasta.Value.Date; q = q.Where(c => c.Fecha <= h); }

        var filas = await q.OrderBy(c => c.Fecha).Take(1000).ToListAsync();
        return filas.Select(c => new CalendTasaDto
        {
            Fecha = c.Fecha,
            NombreDia = c.NombreDia,
            Tasa = c.Tasa,
            TasaCompra = c.TasaCompra,
            TasaVenta = c.TasaVenta
        }).ToList();
    }

    public async Task<List<MonedaDto>> ListarMonedasAsync()
    {
        var monedas = await _db.TipoMonedas.AsNoTracking().OrderBy(m => m.Id).ToListAsync();
        return monedas.Select(m => new MonedaDto
        {
            Id = m.Id,
            Descripcion = m.Descripcion,
            MontoDolar = m.MontoDolar,
            Principal = m.Principal,
            ValorNacional = m.ValorNacional,
            Nacional = m.Nacional,
            Activa = m.Activa
        }).ToList();
    }
}
