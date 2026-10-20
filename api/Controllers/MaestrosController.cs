using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

/// <summary>Maestros de monedas, tasas de cambio y bancos/cuentas de la casa.</summary>
[ApiController]
[Route("api")]
public class MaestrosController : ControllerBase
{
    private readonly ITasaService _tasas;
    private readonly IBancoService _bancos;

    public MaestrosController(ITasaService tasas, IBancoService bancos)
    {
        _tasas = tasas;
        _bancos = bancos;
    }

    // ------------------------------------------------------------- monedas
    /// <summary>TipoMoneda activo (0001 USD, 0002 Bs, ...).</summary>
    [HttpGet("monedas")]
    public async Task<ActionResult<List<MonedaDto>>> Monedas()
        => Ok(await _tasas.ListarMonedasAsync());

    // --------------------------------------------------------------- tasas
    /// <summary>Tasas globales vigentes (activa / compra / venta).</summary>
    [HttpGet("tasas")]
    public async Task<ActionResult<TasaDto>> Tasas()
        => Ok(await _tasas.ObtenerAsync());

    /// <summary>Actualiza las tasas globales y las propaga al calendario del día.</summary>
    [HttpPut("tasas")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<TasaDto>> ActualizarTasas([FromBody] TasaActualizarDto dto)
        => Ok(await _tasas.ActualizarAsync(dto));

    /// <summary>Calendario histórico de tasas. Query: desde, hasta.</summary>
    [HttpGet("tasas/calendario")]
    public async Task<ActionResult<List<CalendTasaDto>>> Calendario([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => Ok(await _tasas.CalendarioAsync(desde, hasta));

    // --------------------------------------------------------------- bancos
    [HttpGet("bancos")]
    public async Task<ActionResult<List<BancoListaDto>>> Bancos([FromQuery] string? buscar, [FromQuery] bool? activo)
        => Ok(await _bancos.ListarAsync(buscar, activo));

    [HttpGet("bancos/{id}")]
    public async Task<ActionResult<BancoListaDto>> Banco(string id)
        => Ok(await _bancos.ObtenerAsync(id));

    [HttpPost("bancos")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<BancoListaDto>> CrearBanco([FromBody] BancoCrearDto dto)
        => Ok(await _bancos.CrearAsync(dto));

    [HttpPut("bancos/{id}")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<BancoListaDto>> ActualizarBanco(string id, [FromBody] BancoCrearDto dto)
        => Ok(await _bancos.ActualizarAsync(id, dto));

    [HttpDelete("bancos/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> EliminarBanco(string id)
    {
        await _bancos.EliminarAsync(id);
        return NoContent();
    }

    // --------------------------------------------------- cuentas de la casa
    [HttpGet("cuentas-banco")]
    public async Task<ActionResult<List<CuentaBancoDto>>> CuentasBanco([FromQuery] string? bancoId)
        => Ok(string.IsNullOrEmpty(bancoId)
            ? await _bancos.ListarTodasLasCuentasAsync()
            : await _bancos.ListarCuentasAsync(bancoId));

    [HttpPost("bancos/{bancoId}/cuentas")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<CuentaBancoDto>> CrearCuenta(string bancoId, [FromBody] CuentaBancoCrearDto dto)
        => Ok(await _bancos.CrearCuentaAsync(bancoId, dto));

    [HttpPut("bancos/{bancoId}/cuentas/{cuentaId:long}")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<CuentaBancoDto>> ActualizarCuenta(string bancoId, long cuentaId, [FromBody] CuentaBancoCrearDto dto)
        => Ok(await _bancos.ActualizarCuentaAsync(bancoId, cuentaId, dto));

    [HttpDelete("bancos/{bancoId}/cuentas/{cuentaId:long}")]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<IActionResult> EliminarCuenta(string bancoId, long cuentaId)
    {
        await _bancos.EliminarCuentaAsync(bancoId, cuentaId);
        return NoContent();
    }
}
