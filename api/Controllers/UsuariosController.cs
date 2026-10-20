using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "ADMIN,GERENTE")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet]
    public async Task<ActionResult<List<UsuarioListaDto>>> Listar([FromQuery] string? buscar, [FromQuery] bool? activo)
        => Ok(await _usuarios.ListarAsync(buscar, activo));

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioListaDto>> Obtener(string id)
        => Ok(await _usuarios.ObtenerAsync(id));

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<UsuarioListaDto>> Crear([FromBody] UsuarioCrearDto dto)
        => Ok(await _usuarios.CrearAsync(dto));

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<UsuarioListaDto>> Actualizar(string id, [FromBody] UsuarioActualizarDto dto)
        => Ok(await _usuarios.ActualizarAsync(id, dto));

    /// <summary>Reemplaza la lista de roles del usuario.</summary>
    [HttpPut("{id}/roles")]
    public async Task<ActionResult<UsuarioListaDto>> AsignarRoles(string id, [FromBody] RolesUsuarioDto dto)
        => Ok(await _usuarios.AsignarRolesAsync(id, dto.Roles));
}

/// <summary>Catálogo de roles y permisos declarativos.</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "ADMIN,GERENTE")]
public class RolesController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public RolesController(IUsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet("roles")]
    public async Task<ActionResult<List<RolDto>>> Roles()
        => Ok(await _usuarios.ListarRolesAsync());

    [HttpGet("permisos")]
    public async Task<ActionResult<List<PermisoDto>>> Permisos()
        => Ok(await _usuarios.ListarPermisosAsync());
}
