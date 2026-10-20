using Microsoft.EntityFrameworkCore;
using Monrys.Api.Domain;

namespace Monrys.Api.Data;

/// <summary>
/// Contexto EF Core del sistema Monrys. El proveedor se elige en Program.cs
/// (PostgreSQL con SearchPath=monrys, o SQLite para el modo demo).
/// </summary>
public class MonrysDbContext : DbContext
{
    public MonrysDbContext(DbContextOptions<MonrysDbContext> options) : base(options) { }

    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<TipoMoneda> TipoMonedas => Set<TipoMoneda>();
    public DbSet<CalendTasa> CalendTasas => Set<CalendTasa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CentroCosto> CentroCostos => Set<CentroCosto>();
    public DbSet<Banco> Bancos => Set<Banco>();
    public DbSet<CuentaBanco> CuentasBanco => Set<CuentaBanco>();
    public DbSet<CuentaCliente> CuentasCliente => Set<CuentaCliente>();
    public DbSet<PlanContable> PlanContable => Set<PlanContable>();
    public DbSet<CodificacionIngreso> CodificacionesIngreso => Set<CodificacionIngreso>();
    public DbSet<CodificacionEgreso> CodificacionesEgreso => Set<CodificacionEgreso>();
    public DbSet<Denominacion> Denominaciones => Set<Denominacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<LogError> LogsError => Set<LogError>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<OperacionRecepcion> OperacionesRecepcion => Set<OperacionRecepcion>();
    public DbSet<OperacionPago> OperacionesPago => Set<OperacionPago>();
    public DbSet<Caja> MovimientosCaja => Set<Caja>();
    public DbSet<Compensacion> Compensaciones => Set<Compensacion>();
    public DbSet<Custodio> Custodios => Set<Custodio>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<Correlativo> Correlativos => Set<Correlativo>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ------------------------------------------------------------------
        // CLAVES COMPUESTAS
        // ------------------------------------------------------------------
        mb.Entity<Caja>(e =>
        {
            e.HasKey(x => new { x.Ejercicio, x.Id });
            e.HasIndex(x => x.Recibo).IsUnique();
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.ClienteId, x.Fecha });
            e.HasIndex(x => new { x.Origen, x.Fecha });
        });

        mb.Entity<Compensacion>(e =>
        {
            e.HasKey(x => new { x.Ejercicio, x.Id });
            e.HasIndex(x => new { x.ClienteId, x.MonedaId, x.Fecha });
        });

        mb.Entity<Custodio>(e => e.HasKey(x => new { x.ClienteId, x.MonedaId }));

        mb.Entity<Correlativo>(e => e.HasKey(x => new { x.Tabla, x.Ejercicio }));

        mb.Entity<RolPermiso>(e => e.HasKey(x => new { x.RolId, x.PermisoId }));
        mb.Entity<UsuarioRol>(e => e.HasKey(x => new { x.UsuarioId, x.RolId }));

        // ------------------------------------------------------------------
        // RELACIONES Y ÍNDICES ÚNICOS
        // ------------------------------------------------------------------
        mb.Entity<Rol>(e =>
        {
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasMany<RolPermiso>().WithOne(x => x.Rol!).HasForeignKey(x => x.RolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Permiso>(e =>
        {
            e.HasIndex(x => x.Clave).IsUnique();
            e.HasMany<RolPermiso>().WithOne(x => x.Permiso!).HasForeignKey(x => x.PermisoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Usuario>(e =>
            e.HasMany<UsuarioRol>().WithOne(x => x.Usuario!).HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade));

        mb.Entity<Cliente>(e =>
        {
            e.HasIndex(x => new { x.RazonSocial, x.Apellidos, x.Nombres });
            e.HasIndex(x => x.Cedula);
            e.HasMany(x => x.Centros).WithOne(x => x.Cliente!).HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Cuentas).WithOne(x => x.Cliente!).HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<CentroCosto>(e => e.HasIndex(x => new { x.ClienteId, x.Principal }));

        mb.Entity<Banco>(e =>
        {
            e.HasIndex(x => x.Nacional);
            e.HasMany(x => x.Cuentas).WithOne(x => x.Banco!).HasForeignKey(x => x.BancoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<CuentaBanco>(e =>
            e.HasIndex(x => new { x.BancoId, x.NumeroCuenta }).IsUnique());

        mb.Entity<CuentaCliente>(e =>
            e.HasIndex(x => new { x.BancoId, x.ClienteId, x.NumeroCuenta }).IsUnique());

        mb.Entity<Compra>(e =>
        {
            e.HasIndex(x => new { x.ClienteId, x.Fecha });
            e.HasIndex(x => x.Fecha);
        });

        mb.Entity<Venta>(e =>
        {
            e.HasIndex(x => new { x.ClienteId, x.Fecha });
            e.HasIndex(x => x.Fecha);
        });

        mb.Entity<OperacionRecepcion>(e =>
        {
            e.HasIndex(x => new { x.TipoOp, x.OperacionId });
            e.HasIndex(x => new { x.TipoOp, x.OperacionId, x.NroPer }).IsUnique();
        });

        mb.Entity<OperacionPago>(e =>
        {
            e.HasIndex(x => new { x.TipoOp, x.OperacionId });
            e.HasIndex(x => new { x.TipoOp, x.OperacionId, x.NroPer }).IsUnique();
        });

        mb.Entity<Transferencia>(e =>
        {
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.BancoId, x.Fecha });
            e.HasIndex(x => new { x.ClienteId, x.Fecha });
        });

        mb.Entity<Bitacora>(e =>
        {
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.UsuarioId, x.Fecha });
        });

        // ------------------------------------------------------------------
        // TIPOS PROPIOS DE POSTGRESQL (en SQLite se usan los del proveedor)
        // ------------------------------------------------------------------
        if (!Database.IsSqlite())
        {
            // Solo las columnas de fecha pura (date) se mapean a 'date';
            // el resto son 'timestamp without time zone' (evita el Kind de timestamptz).
            var columnasFecha = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Fecha", "CierreFecha", "Desde", "Hasta", "FechaCorte"
            };

            foreach (var entidad in mb.Model.GetEntityTypes())
            {
                foreach (var propiedad in entidad.GetProperties())
                {
                    if (propiedad.ClrType == typeof(DateTime) || propiedad.ClrType == typeof(DateTime?))
                    {
                        propiedad.SetColumnType(
                            columnasFecha.Contains(propiedad.Name) ? "date" : "timestamp without time zone");
                    }
                }
            }

            // JSONB de la bitácora (en SQLite queda como TEXT).
            mb.Entity<Bitacora>().Property(x => x.DatosPrevios).HasColumnType("jsonb");

            // SMINT del esquema (id de configuracion).
            mb.Entity<Configuracion>().Property(x => x.Id).HasColumnType("smallint");
        }
    }
}
