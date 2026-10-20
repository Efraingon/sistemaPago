using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;

namespace Monrys.Api.Services;

/// <summary>Usuario autenticado extraído del JWT (claim "sub").</summary>
public interface IUsuarioActual
{
    string? Id { get; }
    string? Nombre { get; }
}

public sealed class UsuarioActualHttpContext : IUsuarioActual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioActualHttpContext(IHttpContextAccessor http) => _http = http;

    public string? Id => _http.HttpContext?.User.FindFirst("sub")?.Value;
    public string? Nombre => _http.HttpContext?.User.FindFirst("name")?.Value;
}

public interface IAuditoriaService
{
    /// <summary>Escribe en bitácora. Se ejecuta dentro de la transacción activa si la hay.</summary>
    Task RegistrarAsync(string accion, string? tabla = null, string? registroId = null,
        object? datosPrevios = null, string? observaciones = null);
}

/// <summary>Auditoría de mutaciones (tabla BITACORA).</summary>
public sealed class AuditoriaService : IAuditoriaService
{
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;
    private readonly MonrysDbContext _db;
    private readonly IUsuarioActual _usuarioActual;

    public AuditoriaService(MonrysDbContext db, IUsuarioActual usuarioActual)
    {
        _db = db;
        _usuarioActual = usuarioActual;
    }

    public Task RegistrarAsync(string accion, string? tabla = null, string? registroId = null,
        object? datosPrevios = null, string? observaciones = null)
    {
        var fila = new Bitacora
        {
            Fecha = DateTime.Now,
            UsuarioId = _usuarioActual.Id,
            Accion = Truncar(accion, 200) ?? accion,
            Observaciones = Truncar(observaciones, 200),
            Equipo = _usuarioActual.Nombre,
            Tabla = Truncar(tabla, 60),
            RegistroId = Truncar(registroId, 30),
            DatosPrevios = datosPrevios is null ? null : JsonSerializer.Serialize(datosPrevios, Json)
        };
        _db.Bitacoras.Add(fila);
        return _db.SaveChangesAsync();
    }

    private static string? Truncar(string? valor, int max)
        => valor is null ? null : (valor.Length <= max ? valor : valor[..max]);
}

/// <summary>Consulta de la bitácora (GET /api/auditoria).</summary>
public interface IAuditoriaConsultaService
{
    Task<(List<Bitacora> items, int total)> ListarAsync(AuditoriaFiltro filtro);
}

public sealed class AuditoriaFiltro
{
    public string? Tabla { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Usuario { get; set; }
    public string? Buscar { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
}

public sealed class AuditoriaConsultaService : IAuditoriaConsultaService
{
    private readonly MonrysDbContext _db;

    public AuditoriaConsultaService(MonrysDbContext db) => _db = db;

    public async Task<(List<Bitacora> items, int total)> ListarAsync(AuditoriaFiltro f)
    {
        var q = _db.Bitacoras.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Tabla)) q = q.Where(x => x.Tabla == f.Tabla);
        if (!string.IsNullOrWhiteSpace(f.Usuario)) q = q.Where(x => x.UsuarioId == f.Usuario);
        if (!string.IsNullOrWhiteSpace(f.Buscar))
        {
            var t = f.Buscar.Trim();
            q = q.Where(x => (x.Accion != null && x.Accion.Contains(t)) ||
                             (x.RegistroId != null && x.RegistroId.Contains(t)));
        }
        if (f.Desde.HasValue)
        {
            var d = f.Desde.Value.Date;
            q = q.Where(x => x.Fecha >= d);
        }
        if (f.Hasta.HasValue)
        {
            var h = f.Hasta.Value.Date.AddDays(1);
            q = q.Where(x => x.Fecha < h);
        }

        var total = await q.CountAsync();
        var pagina = Math.Max(1, f.Pagina);
        var tamano = Math.Clamp(f.Tamano, 1, 200);
        var items = await q.OrderByDescending(x => x.Fecha)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();
        return (items, total);
    }
}
