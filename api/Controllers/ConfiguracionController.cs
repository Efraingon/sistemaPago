using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _config;

    public ConfiguracionController(IConfiguracionService config) => _config = config;

    /// <summary>Configuración global de la casa (empresa, monedas, tasas, IVA).</summary>
    [HttpGet]
    public async Task<ActionResult<ConfiguracionDto>> Obtener()
        => Ok(await _config.ObtenerAsync());

    /// <summary>Actualización parcial: sólo los campos enviados se modifican.</summary>
    [HttpPut]
    [Authorize(Roles = "ADMIN,GERENTE")]
    public async Task<ActionResult<ConfiguracionDto>> Actualizar([FromBody] ConfiguracionActualizarDto dto)
        => Ok(await _config.ActualizarAsync(dto));

    /// <summary>Codificaciones de ingresos y egresos para el libro de caja.</summary>
    [HttpGet("codificaciones")]
    public async Task<ActionResult<CodificacionesDto>> Codificaciones()
        => Ok(await _config.CodificacionesAsync());
}
