using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/caja")]
public class CajaController : ControllerBase
{
    private readonly ICajaService _caja;

    public CajaController(ICajaService caja) => _caja = caja;

    /// <summary>Movimientos del libro de caja. Query: desde, hasta, origen, clienteId, estado, buscar, pagina, tamano.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MovimientoCajaDto>>> Listar([FromQuery] MovimientoCajaFiltroDto filtro)
        => Ok(await _caja.ListarAsync(filtro));

    [HttpGet("{ejercicio}/{id}")]
    public async Task<ActionResult<MovimientoCajaDto>> Obtener(string ejercicio, string id)
        => Ok(await _caja.ObtenerAsync(ejercicio, id));

    /// <summary>Ingreso general (origen IN, o DE/DS si la moneda es divisa).</summary>
    [HttpPost("ingresos")]
    public async Task<ActionResult<MovimientoCajaDto>> Ingreso([FromBody] MovimientoCajaCrearDto dto)
        => Ok(await _caja.CrearIngresoEgresoAsync(esIngreso: true, dto));

    /// <summary>Egreso general (origen EG, o DS si la moneda es divisa).</summary>
    [HttpPost("egresos")]
    public async Task<ActionResult<MovimientoCajaDto>> Egreso([FromBody] MovimientoCajaCrearDto dto)
        => Ok(await _caja.CrearIngresoEgresoAsync(esIngreso: false, dto));

    [HttpDelete("{ejercicio}/{id}")]
    public async Task<ActionResult<MovimientoCajaDto>> Anular(string ejercicio, string id, [FromBody] AnulacionDto? dto)
        => Ok(await _caja.AnularAsync(ejercicio, id, dto?.Motivo));

    /// <summary>Cierra el libro del día (marca CIERRE_FECHA a los movimientos abiertos).</summary>
    [HttpPost("cierre")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<CierreCajaResultadoDto>> Cerrar([FromBody] CierreCajaDto dto)
        => Ok(await _caja.CerrarAsync(dto.Fecha));

    [HttpGet("cierre")]
    [HttpGet("cierre/estado")] // alias consumido por el frontend (GET /api/caja/cierre/estado?fecha=)
    public async Task<ActionResult<CierreCajaEstadoDto>> EstadoCierre([FromQuery] DateTime fecha)
        => Ok(await _caja.EstadoCierreAsync(fecha));

    /// <summary>Arqueo físico por moneda y denominación para la fecha.</summary>
    [HttpGet("arqueo")]
    public async Task<ActionResult<ArqueoDto>> Arqueo([FromQuery] DateTime fecha)
        => Ok(await _caja.ArqueoAsync(fecha));
}
