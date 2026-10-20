using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

/// <summary>
/// Custodia de clientes (COMPENSACIONES). Signos según doc de reglas §3:
/// recibir = ABONOS (+), entregar = RETIROS (−).
/// </summary>
[ApiController]
[Route("api/custodios")]
public class CustodiosController : ControllerBase
{
    private readonly ICustodioService _custodio;

    public CustodiosController(ICustodioService custodio) => _custodio = custodio;

    /// <summary>Saldos disponibles por cliente y moneda. Query: clienteId.</summary>
    [HttpGet("saldos")]
    public async Task<ActionResult<List<CustodioSaldoDto>>> Saldos([FromQuery] string? clienteId)
        => Ok(await _custodio.SaldosAsync(clienteId));

    /// <summary>Movimientos (compensaciones) de un cliente. Query: desde, hasta, pagina, tamano.</summary>
    [HttpGet("{clienteId}/movimientos")]
    public async Task<ActionResult<object>> Movimientos(string clienteId,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
    {
        var (items, total, disponible) = await _custodio.MovimientosAsync(clienteId, desde, hasta, pagina, tamano);
        return Ok(new { items, total, disponible });
    }

    /// <summary>Recibe divisa del cliente en custodia (abono, disponible +).</summary>
    [HttpPost("recibir")]
    public async Task<ActionResult<CustodioMoverResultadoDto>> Recibir([FromBody] CustodioMoverDto dto)
        => Ok(await _custodio.RecibirAsync(dto));

    /// <summary>Entrega divisa al cliente desde su custodia (retiro, disponible −).</summary>
    [HttpPost("entregar")]
    public async Task<ActionResult<CustodioMoverResultadoDto>> Entregar([FromBody] CustodioMoverDto dto)
        => Ok(await _custodio.EntregarAsync(dto));
}
