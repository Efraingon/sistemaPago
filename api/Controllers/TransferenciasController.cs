using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/transferencias")]
public class TransferenciasController : ControllerBase
{
    private readonly ITransferenciaService _transferencias;

    public TransferenciasController(ITransferenciaService transferencias) => _transferencias = transferencias;

    [HttpGet]
    public async Task<ActionResult<PagedResult<TransferenciaDto>>> Listar([FromQuery] TransferenciaFiltroDto filtro)
        => Ok(await _transferencias.ListarAsync(filtro));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransferenciaDto>> Obtener(long id)
        => Ok(await _transferencias.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<TransferenciaDto>> Crear([FromBody] TransferenciaCrearDto dto)
    {
        var creada = await _transferencias.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<TransferenciaDto>> Anular(long id, [FromBody] AnulacionDto? dto)
        => Ok(await _transferencias.AnularAsync(id, dto?.Motivo));
}
