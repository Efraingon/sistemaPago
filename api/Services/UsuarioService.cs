using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IUsuarioService
{
    Task<List<UsuarioListaDto>> ListarAsync(string? buscar, bool? activo);
    Task<UsuarioListaDto> ObtenerAsync(string id);
    Task<UsuarioListaDto> CrearAsync(UsuarioCrearDto dto);
    Task<UsuarioListaDto> ActualizarAsync(string id, UsuarioActualizarDto dto);
    Task<UsuarioListaDto> AsignarRolesAsync(string id, List<string>? roles);

    Task<List<RolDto>> ListarRolesAsync();
    Task<List<PermisoDto>> ListarPermisosAsync();

    /// <summary>Valida credenciales y devuelve usuario + roles + permisos (para el login).</summary>
    Task<(Usuario usuario, List<string> roles, List<string> permisos)> AutenticarAsync(string usuario, string clave);

    /// <summary>Usuario + roles + permisos agregados (para GET /api/auth/me).</summary>
    Task<UsuarioActualDto> ObtenerActualAsync(string usuarioId);
}

/// <summary>Usuarios, roles y permisos declarativos (VFP: nuevousuario / modiuario + USUARIOS.ACCESO).</summary>
public sealed class UsuarioService : IUsuarioService
{
    private readonly MonrysDbContext _db;
    private readonly IPasswordHasherService _hasher;
    private readonly IAuditoriaService _auditoria;

    public UsuarioService(MonrysDbContext db, IPasswordHasherService hasher, IAuditoriaService auditoria)
    {
        _db = db;
        _hasher = hasher;
        _auditoria = auditoria;
    }

    public async Task<List<UsuarioListaDto>> ListarAsync(string? buscar, bool? activo)
    {
        var q = _db.Usuarios.AsNoTracking().AsQueryable();
        if (activo.HasValue) q = q.Where(u => u.Activo == activo.Value);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim().ToLower();
            q = q.Where(u => u.Id.ToLower().Contains(t) || u.Nombre.ToLower().Contains(t));
        }

        var usuarios = await q.OrderBy(u => u.Id).ToListAsync();
        var rolesPorUsuario = await CargarRolesAsync(usuarios.Select(u => u.Id).ToList());

        return usuarios.Select(u =>
        {
            rolesPorUsuario.TryGetValue(u.Id, out var roles);
            return Mapear(u, roles ?? []);
        }).ToList();
    }

    public async Task<UsuarioListaDto> ObtenerAsync(string id)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el usuario {id}.");
        var mapa = await CargarRolesAsync([id]);
        mapa.TryGetValue(id, out var roles);
        return Mapear(usuario, roles ?? []);
    }

    public async Task<UsuarioListaDto> CrearAsync(UsuarioCrearDto dto)
    {
        var id = (dto.Id ?? "").Trim().ToUpperInvariant();
        if (id.Length == 0 || id.Length > 10)
            throw new ValidacionException("El id del usuario debe tener entre 1 y 10 caracteres.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidacionException("Debe indicar el nombre del usuario.");
        if (string.IsNullOrWhiteSpace(dto.Clave) || dto.Clave.Length < 6)
            throw new ValidacionException("La clave debe tener al menos 6 caracteres.");
        if (await _db.Usuarios.AnyAsync(u => u.Id == id))
            throw new ConflictoException($"Ya existe el usuario {id}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var usuario = new Usuario
        {
            Id = id,
            Nombre = dto.Nombre.Trim(),
            ClaveHash = _hasher.Hashear(dto.Clave),
            Tipo = (short)Math.Clamp(dto.Tipo, 1, 3),
            Activo = dto.Activo,
            Bloqueado = false,
            CreadoEn = DateTime.Now
        };
        _db.Usuarios.Add(usuario);

        var roles = await ResolverRolesAsync(dto.Roles);
        foreach (var rol in roles)
            _db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = id, RolId = rol.Id });

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"CREA EL USUARIO {id}", "usuario", id,
            new { id, nombre = usuario.Nombre, tipo = usuario.Tipo, roles = roles.Select(r => r.Nombre).ToList() });

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task<UsuarioListaDto> ActualizarAsync(string id, UsuarioActualizarDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el usuario {id}.");

        if (dto.Clave is not null && dto.Clave.Length < 6)
            throw new ValidacionException("La clave debe tener al menos 6 caracteres.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new { usuario.Nombre, usuario.Tipo, usuario.Activo, usuario.Bloqueado, cambioClave = dto.Clave != null };

        if (!string.IsNullOrWhiteSpace(dto.Nombre)) usuario.Nombre = dto.Nombre.Trim();
        if (dto.Tipo.HasValue) usuario.Tipo = (short)Math.Clamp(dto.Tipo.Value, 1, 3);
        if (dto.Activo.HasValue) usuario.Activo = dto.Activo.Value;
        if (dto.Bloqueado.HasValue) usuario.Bloqueado = dto.Bloqueado.Value;
        if (!string.IsNullOrWhiteSpace(dto.Clave)) usuario.ClaveHash = _hasher.Hashear(dto.Clave);

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"MODIFICA EL USUARIO {id}", "usuario", id, previo);

        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    public async Task<UsuarioListaDto> AsignarRolesAsync(string id, List<string>? roles)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new RecursoNoEncontradoException($"No existe el usuario {id}.");

        var nuevos = await ResolverRolesAsync(roles);
        var actuales = await _db.UsuarioRoles.AsNoTracking().Where(r => r.UsuarioId == id).ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        var previo = new { roles = actuales.Select(r => r.RolId).ToList() };

        _db.UsuarioRoles.RemoveRange(actuales);
        foreach (var rol in nuevos)
            _db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = id, RolId = rol.Id });

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ASIGNA ROLES AL USUARIO {id}: {string.Join(", ", nuevos.Select(r => r.Nombre))}",
            "usuario_rol", id, previo);

        await tx.CommitAsync();
        _ = usuario;
        return await ObtenerAsync(id);
    }

    public async Task<List<RolDto>> ListarRolesAsync()
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Nombre).ToListAsync();
        var vinculos = await _db.RolPermisos.AsNoTracking().ToListAsync();
        var permisos = await _db.Permisos.AsNoTracking().OrderBy(p => p.Clave).ToListAsync();

        return roles.Select(r => new RolDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            Permisos = permisos
                .Where(p => vinculos.Any(v => v.RolId == r.Id && v.PermisoId == p.Id))
                .Select(p => new PermisoDto { Id = p.Id, Clave = p.Clave, Descripcion = p.Descripcion })
                .ToList()
        }).ToList();
    }

    public async Task<List<PermisoDto>> ListarPermisosAsync()
        => (await _db.Permisos.AsNoTracking().OrderBy(p => p.Clave).ToListAsync())
            .Select(p => new PermisoDto { Id = p.Id, Clave = p.Clave, Descripcion = p.Descripcion })
            .ToList();

    public async Task<(Usuario usuario, List<string> roles, List<string> permisos)> AutenticarAsync(string usuario, string clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            throw new ValidacionException("Debe indicar usuario y clave.");

        var id = usuario.Trim();
        var fila = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id || u.Nombre == id)
            ?? throw new RecursoNoEncontradoException("Usuario o clave incorrectos.");

        if (!fila.Activo) throw new ValidacionException("El usuario está inactivo.");
        if (fila.Bloqueado) throw new ValidacionException("El usuario está bloqueado.");
        if (!_hasher.Verificar(clave, fila.ClaveHash))
            throw new RecursoNoEncontradoException("Usuario o clave incorrectos.");

        var roles = await CargarRolesAsync([fila.Id]);
        roles.TryGetValue(fila.Id, out var listaRoles);
        var permisos = await CargarPermisosAsync(listaRoles ?? []);

        return (fila, listaRoles ?? [], permisos);
    }

    public async Task<UsuarioActualDto> ObtenerActualAsync(string usuarioId)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId)
            ?? throw new RecursoNoEncontradoException($"No existe el usuario {usuarioId}.");

        var mapa = await CargarRolesAsync([usuarioId]);
        mapa.TryGetValue(usuarioId, out var roles);
        var listaRoles = roles ?? [];

        return new UsuarioActualDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Tipo = usuario.Tipo,
            Roles = listaRoles,
            Permisos = await CargarPermisosAsync(listaRoles)
        };
    }

    // ------------------------------------------------------------------
    private async Task<List<Rol>> ResolverRolesAsync(List<string>? roles)
    {
        if (roles is null || roles.Count == 0)
            return await _db.Roles.AsNoTracking()
                .Where(r => r.Nombre == "CAJERO")
                .ToListAsync();

        var buscados = roles.Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant()).Distinct().ToList();

        var encontrados = await _db.Roles.AsNoTracking()
            .Where(r => buscados.Contains(r.Nombre))
            .ToListAsync();

        var faltantes = buscados.Except(encontrados.Select(r => r.Nombre)).ToList();
        if (faltantes.Count > 0)
            throw new ValidacionException($"Roles inexistentes: {string.Join(", ", faltantes)}.");

        return encontrados;
    }

    private async Task<Dictionary<string, List<string>>> CargarRolesAsync(List<string> usuarioIds)
    {
        if (usuarioIds.Count == 0) return new Dictionary<string, List<string>>();

        var vinculos = await _db.UsuarioRoles.AsNoTracking()
            .Where(v => usuarioIds.Contains(v.UsuarioId))
            .Join(_db.Roles.AsNoTracking(), v => v.RolId, r => r.Id, (v, r) => new { v.UsuarioId, r.Nombre })
            .ToListAsync();

        return vinculos.GroupBy(v => v.UsuarioId)
            .ToDictionary(g => g.Key, g => g.Select(v => v.Nombre).OrderBy(n => n).ToList());
    }

    private async Task<List<string>> CargarPermisosAsync(List<string> roles)
    {
        if (roles.Count == 0) return [];

        var rolIds = await _db.Roles.AsNoTracking()
            .Where(r => roles.Contains(r.Nombre))
            .Select(r => r.Id)
            .ToListAsync();
        if (rolIds.Count == 0) return [];

        return await _db.RolPermisos.AsNoTracking()
            .Where(v => rolIds.Contains(v.RolId))
            .Join(_db.Permisos.AsNoTracking(), v => v.PermisoId, p => p.Id, (v, p) => p.Clave)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    private static UsuarioListaDto Mapear(Usuario u, List<string> roles) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Tipo = u.Tipo,
        Activo = u.Activo,
        Bloqueado = u.Bloqueado,
        UltimoAcceso = u.UltimoAcceso,
        CreadoEn = u.CreadoEn,
        Roles = roles
    };
}
