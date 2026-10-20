using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Monrys.Api.Domain;

namespace Monrys.Api.Services;

public record TokenCreado(string Token, DateTime Expira);

public interface ITokenService
{
    TokenCreado Crear(Usuario usuario, IEnumerable<string> roles);
}

/// <summary>Emisión de JWT simétrico (HS256) con claims sub, name, tipo y roles.</summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public TokenCreado Crear(Usuario usuario, IEnumerable<string> roles)
    {
        var clave = _config["Jwt:Key"]
            ?? throw new ValidacionException("Falta la llave de firma JWT (Jwt:Key).");
        if (clave.Length < 32)
            throw new ValidacionException("La llave de firma JWT (Jwt:Key) debe tener al menos 32 caracteres.");

        var issuer = _config["Jwt:Issuer"] ?? "MonrysApi";
        var audience = _config["Jwt:Audience"] ?? "MonrysWeb";
        var horas = int.TryParse(_config["Jwt:HorasExpiracion"], out var h) && h > 0 ? h : 8;

        var ahora = DateTime.UtcNow;
        var expira = ahora.AddHours(horas);

        var claims = new List<Claim>
        {
            new("sub", usuario.Id),
            new("name", usuario.Nombre),
            new("tipo", usuario.Tipo.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(r => new Claim("roles", r)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: ahora,
            expires: expira,
            signingCredentials: credenciales);

        return new TokenCreado(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
