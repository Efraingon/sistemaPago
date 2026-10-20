using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

/// <summary>Consulta de la bitácora de auditoría (tabla BITACORA).</summary>
[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = "ADMIN,GERENTE")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaConsultaService _auditoria;

    public AuditoriaController(IAuditoriaConsultaService auditoria) => _auditoria = auditoria;

    /// <summary>Query: tabla, desde, hasta, usuario, buscar, pagina, tamano.</summary>
    [HttpGet]
    public async Task<ActionResult<object>> Listar([FromQuery] AuditoriaFiltro filtro)
    {
        var (items, total) = await _auditoria.ListarAsync(filtro);
        return Ok(new { items, total });
    }
}
