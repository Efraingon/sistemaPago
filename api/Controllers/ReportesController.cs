using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportes;

    public ReportesController(IReportesService reportes) => _reportes = reportes;

    /// <summary>Saldos en Bs por grupo (caja, custodios, bancos, clientes, proveedores, IVA).</summary>
    [HttpGet("consolidado")]
    public async Task<ActionResult<ConsolidadoDto>> Consolidado([FromQuery] DateTime? fecha)
        => Ok(await _reportes.ConsolidadoAsync(fecha));

    /// <summary>Estado general de clientes (debe / haber / saldo en Bs y USD).</summary>
    [HttpGet("estado-general")]
    public async Task<ActionResult<EstadoGeneralDto>> EstadoGeneral([FromQuery] DateTime? fechaCorte, [FromQuery] string? clienteId)
        => Ok(await _reportes.EstadoGeneralAsync(fechaCorte, clienteId));

    /// <summary>Cuadre de caja por moneda para el rango.</summary>
    [HttpGet("cuadre-caja")]
    public async Task<ActionResult<CuadreCajaDto>> CuadreCaja([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _reportes.CuadreCajaAsync(desde, hasta));

    /// <summary>Movimientos de caja agrupados por origen (entradas/salidas).</summary>
    [HttpGet("caja-por-origen")]
    public async Task<ActionResult<CajaPorOrigenDto>> CajaPorOrigen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? tipo)
        => Ok(await _reportes.CajaPorOrigenAsync(desde, hasta, tipo));

    /// <summary>Estado de cuenta del cliente con saldos acumulados.</summary>
    [HttpGet("estado-cuenta/{clienteId}")]
    public async Task<ActionResult<EstadoCuentaDto>> EstadoCuenta(string clienteId,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _reportes.EstadoCuentaAsync(clienteId, desde, hasta));

    /// <summary>Comprobante imprimible de una operación. tipo: "C" | "V".</summary>
    [HttpGet("comprobante/{tipo}/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ComprobanteDto>> Comprobante(string tipo, string id)
        => Ok(await _reportes.ComprobanteAsync(tipo, id));
}
