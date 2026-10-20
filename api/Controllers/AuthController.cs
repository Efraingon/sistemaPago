using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarios;
    private readonly ITokenService _token;
    private readonly IUsuarioActual _actual;

    public AuthController(IUsuarioService usuarios, ITokenService token, IUsuarioActual actual)
    {
        _usuarios = usuarios;
        _token = token;
        _actual = actual;
    }

    /// <summary>Inicio de sesión. Devuelve token JWT, expiración y usuario con roles/permisos.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Usuario) || string.IsNullOrWhiteSpace(dto.Clave))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Usuario y clave son obligatorios." });

        try
        {
            var (usuario, roles, permisos) = await _usuarios.AutenticarAsync(dto.Usuario.Trim(), dto.Clave);
            var token = _token.Crear(usuario, roles);
            return Ok(new LoginResponse
            {
                Token = token.Token,
                Expira = token.Expira,
                Usuario = new UsuarioActualDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Tipo = usuario.Tipo,
                    Roles = roles,
                    Permisos = permisos
                }
            });
        }
        catch (Exception ex) when (ex is ValidacionException or RecursoNoEncontradoException)
        {
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Credenciales inválidas." });
        }
    }

    /// <summary>Usuario autenticado actual (JWT) con sus roles y permisos.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioActualDto>> Me()
    {
        var id = _actual.Id;
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        return Ok(await _usuarios.ObtenerActualAsync(id));
    }
}
