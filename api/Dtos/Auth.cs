namespace Monrys.Api.Dtos;

public record LoginRequest(string Usuario, string Clave);

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime Expira { get; set; }
    public UsuarioActualDto Usuario { get; set; } = new();
}

public class UsuarioActualDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Tipo { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permisos { get; set; } = [];
}
