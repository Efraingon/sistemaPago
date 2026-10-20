using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

/// <summary>Motor del Centro de Reportes: catálogo e informes genéricos.</summary>
[ApiController]
[Route("api/informes")]
public class InformesController : ControllerBase
{
    private readonly IInformeService _informes;

    public InformesController(IInformeService informes) => _informes = informes;

    /// <summary>Catálogo de informes disponibles (agrupado por familias, con sus parámetros).</summary>
    [HttpGet]
    public async Task<ActionResult<InformeCatalogoDto>> Catalogo()
        => Ok(await _informes.CatalogoAsync());

    /// <summary>Ejecuta un informe por id con sus parámetros en query string.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InformeDto>> Ejecutar(string id, [FromQuery] InformeParametros p)
        => Ok(await _informes.EjecutarAsync(id, p));
}
