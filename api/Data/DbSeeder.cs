using Microsoft.EntityFrameworkCore;
using Monrys.Api.Domain;

namespace Monrys.Api.Data;

/// <summary>
/// Replicación en C# de database/02_seed.sql. Es idempotente: garantiza los datos
/// base (monedas, configuración con tasas, plan contable, codificaciones,
/// denominaciones, bancos, roles/permisos y el usuario ADMIN) esté la BD vacía o no.
/// </summary>
public static class DbSeeder
{
    /// <summary>Hash PBKDF2-SHA256 (100.000 iteraciones) de la clave "admin123".</summary>
    public const string HashAdminInicial = "PBKDF2|100000|myV3AhkBdYBiP8Y8G9iwgA==|QsArkqwlgep11ncbkVUmCEbJy0LEcuHVVHOusO4Bnfo=";

    public static void Sembrar(MonrysDbContext db)
    {
        SembrarMonedas(db);
        SembrarConfiguracion(db);
        SembrarPlanContable(db);
        SembrarCodificaciones(db);
        SembrarDenominaciones(db);
        SembrarBancos(db);
        SembrarSeguridad(db);
        db.SaveChanges();
    }

    private static void SembrarMonedas(MonrysDbContext db)
    {
        if (db.TipoMonedas.Any()) return;
        db.TipoMonedas.AddRange(
            new TipoMoneda { Id = "0001", Descripcion = "USD/BS", MontoDolar = 1m, Principal = true, ValorNacional = 1m, Nacional = false, Activa = true },
            new TipoMoneda { Id = "0002", Descripcion = "BS", MontoDolar = 1m, Principal = false, ValorNacional = 1m, Nacional = true, Activa = true });
    }

    private static void SembrarConfiguracion(MonrysDbContext db)
    {
        if (db.Configuraciones.Any()) return;
        db.Configuraciones.Add(new Configuracion
        {
            Id = 1,
            NroCliente = 23,
            NombreEmpresa = "MONRYS CAPITAL LLC",
            Direccion = "Caracas Vzla.",
            Telefono = "0212 00000000",
            Rif = "123456789",
            Iva = 16.00m,
            MonedaPrincipalId = "0001",
            MonedaNacionalId = "0002",
            TasaActiva = 220.0000000m,
            TasaCompra = 285.0000000m,
            TasaVenta = 300.0000000m,
            Version = "2.0.0"
        });
    }

    private static void SembrarPlanContable(MonrysDbContext db)
    {
        if (db.PlanContable.Any()) return;
        db.PlanContable.AddRange(
            new PlanContable { Cuenta = "4.1.01.002", Descripcion = "OTROS INGRESOS Y COMISIONES POR OPERACIONES DE TESORERIA" },
            new PlanContable { Cuenta = "4.1.03.001", Descripcion = "INGRESO POR CREDITOS VIGENTES" },
            new PlanContable { Cuenta = "4.1.03.002", Descripcion = "INGRESO POR CREDITOS REESTRUCTURADOS" },
            new PlanContable { Cuenta = "4.1.03.003", Descripcion = "INGRESO POR CREDITOS VENCIDOS" },
            new PlanContable { Cuenta = "4.1.03.004", Descripcion = "INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS" },
            new PlanContable { Cuenta = "6.1.01.002", Descripcion = "GASTOS COMISIONES BMN" },
            new PlanContable { Cuenta = "6.1.01.003", Descripcion = "GASTOS COMISIONES BME" },
            new PlanContable { Cuenta = "6.1.01.004", Descripcion = "PERDIDA EN COMPRA DE DIVISAS NO RECUPERADAS" },
            new PlanContable { Cuenta = "6.1.01.005", Descripcion = "PERDIDA SALDOS EN MN NO COBRADOS" },
            new PlanContable { Cuenta = "6.1.01.006", Descripcion = "OTROS GASTOS Y COMISIONES FINANCIERAS" },
            new PlanContable { Cuenta = "6.1.01.007", Descripcion = "INTERESES POR CAPTACIONES DE TESORERIA" },
            new PlanContable { Cuenta = "6.1.01.008", Descripcion = "INTERESES POR PRESTAMOS BANCARIOS" },
            new PlanContable { Cuenta = "6.1.01.009", Descripcion = "COMISIONES POR LIQUIDACION DE PRESTAMOS" },
            new PlanContable { Cuenta = "6.1.02.001", Descripcion = "SUELDOS Y SALARIOS AL PERSONAL" });
    }

    private static void SembrarCodificaciones(MonrysDbContext db)
    {
        if (!db.CodificacionesIngreso.Any())
        {
            db.CodificacionesIngreso.AddRange(
                new CodificacionIngreso { Id = "00001", Descripcion = "OTROS INGRESOS FINANCIEROS", CuentaContable = "4.1.01.002" },
                new CodificacionIngreso { Id = "00002", Descripcion = "INGRESOS POR CAMBIO ENTRE DIVISAS", CuentaContable = "4.1.01.002" },
                new CodificacionIngreso { Id = "00003", Descripcion = "INGRESOS POR TRANSACCION", CuentaContable = "4.1.01.002" },
                new CodificacionIngreso { Id = "00004", Descripcion = "RESULTADO EN OPERACIONES DE TESORERIA", CuentaContable = "4.1.01.002" },
                new CodificacionIngreso { Id = "00005", Descripcion = "INGRESO POR CREDITOS VIGENTES", CuentaContable = "4.1.03.001" },
                new CodificacionIngreso { Id = "00006", Descripcion = "INGRESO POR CREDITOS REESTRUCTURADOS", CuentaContable = "4.1.03.002" },
                new CodificacionIngreso { Id = "00007", Descripcion = "INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS", CuentaContable = "4.1.03.003" },
                new CodificacionIngreso { Id = "00008", Descripcion = "INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS", CuentaContable = "4.1.03.004" });
        }

        if (!db.CodificacionesEgreso.Any())
        {
            db.CodificacionesEgreso.AddRange(
                new CodificacionEgreso { Id = "00001", Descripcion = "COMISIONES BANCARIAS", CuentaContable = "6.1.01.002" },
                new CodificacionEgreso { Id = "00002", Descripcion = "SUELDOS Y SALARIOS", CuentaContable = "6.1.02.001" },
                new CodificacionEgreso { Id = "00003", Descripcion = "GASTOS OPERATIVOS", CuentaContable = "6.1.01.006" },
                new CodificacionEgreso { Id = "00004", Descripcion = "INTERESES Y COMISIONES DE PRESTAMOS", CuentaContable = "6.1.01.008" },
                new CodificacionEgreso { Id = "00005", Descripcion = "PERDIDAS POR COMPRAS DE DIVISAS NO RECUPERADAS", CuentaContable = "6.1.01.004" },
                new CodificacionEgreso { Id = "00006", Descripcion = "COMISIONES BANCARIAS MNE", CuentaContable = "6.1.01.003" });
        }
    }

    private static void SembrarDenominaciones(MonrysDbContext db)
    {
        if (db.Denominaciones.Any()) return;
        db.Denominaciones.AddRange(
            new Denominacion { Id = "D001", MonedaId = "0001", Valor = 1.00m },
            new Denominacion { Id = "D002", MonedaId = "0001", Valor = 2.00m },
            new Denominacion { Id = "D005", MonedaId = "0001", Valor = 5.00m },
            new Denominacion { Id = "D010", MonedaId = "0001", Valor = 10.00m },
            new Denominacion { Id = "D020", MonedaId = "0001", Valor = 20.00m },
            new Denominacion { Id = "D050", MonedaId = "0001", Valor = 50.00m },
            new Denominacion { Id = "D100", MonedaId = "0001", Valor = 100.00m },
            new Denominacion { Id = "N010", MonedaId = "0002", Valor = 10.00m },
            new Denominacion { Id = "N020", MonedaId = "0002", Valor = 20.00m },
            new Denominacion { Id = "N050", MonedaId = "0002", Valor = 50.00m },
            new Denominacion { Id = "N100", MonedaId = "0002", Valor = 100.00m },
            new Denominacion { Id = "N200", MonedaId = "0002", Valor = 200.00m },
            new Denominacion { Id = "N500", MonedaId = "0002", Valor = 500.00m });
    }

    private static void SembrarBancos(MonrysDbContext db)
    {
        if (db.Bancos.Any()) return;
        db.Bancos.AddRange(
            new Banco { Id = "00001", Nombre = "BANESCO", Nacional = 1 },
            new Banco { Id = "00002", Nombre = "PROVINCIAL", Nacional = 1 },
            new Banco { Id = "00003", Nombre = "EXTERIOR", Nacional = 1 },
            new Banco { Id = "00005", Nombre = "BANCO PROVINCIAL", Nacional = 1 },
            new Banco { Id = "00006", Nombre = "BANCO MERCANTIL", Nacional = 1 },
            new Banco { Id = "00007", Nombre = "BANCO DE VENEZUELA", Nacional = 1 },
            new Banco { Id = "00008", Nombre = "WELLS FARGO", Nacional = 2 },
            new Banco { Id = "00009", Nombre = "CITI BANK", Nacional = 2 });
    }

    private static void SembrarSeguridad(MonrysDbContext db)
    {
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Rol { Nombre = "ADMIN", Descripcion = "Acceso total al sistema" },
                new Rol { Nombre = "GERENTE", Descripcion = "Consulta gerencial, reportes y cierre de caja" },
                new Rol { Nombre = "CAJERO", Descripcion = "Operación de punto de caja: cobros, pagos, recibos" });
            db.SaveChanges();
        }

        if (!db.Permisos.Any())
        {
            var claves = new (string clave, string descripcion)[]
            {
                ("clientes.ver", "Ver clientes"),
                ("clientes.crear", "Crear clientes"),
                ("clientes.editar", "Editar clientes"),
                ("operaciones.ver", "Ver compras/ventas"),
                ("operaciones.crear", "Crear compras/ventas"),
                ("operaciones.anular", "Anular compras/ventas"),
                ("caja.ver", "Ver movimientos de caja"),
                ("caja.mover", "Registrar ingresos/egresos/cobros/pagos"),
                ("caja.cerrar", "Realizar cierre de caja"),
                ("caja.anular", "Anular movimientos de caja"),
                ("bancos.ver", "Ver bancos y cuentas"),
                ("bancos.gestionar", "Gestionar bancos y cuentas"),
                ("transferencias.ver", "Ver transferencias"),
                ("transferencias.crear", "Crear transferencias"),
                ("custodios.ver", "Ver custodios"),
                ("custodios.mover", "Entregar/recibir custodio"),
                ("tasas.gestionar", "Modificar tasas de cambio"),
                ("reportes.gerenciales", "Ver reportes gerenciales"),
                ("usuarios.gestionar", "Gestionar usuarios y permisos"),
                ("config.gestionar", "Modificar configuración del sistema")
            };
            db.Permisos.AddRange(claves.Select(p => new Permiso { Clave = p.clave, Descripcion = p.descripcion }));
            db.SaveChanges();
        }

        if (!db.RolPermisos.Any())
        {
            var permisos = db.Permisos.AsNoTracking().ToList();
            var admin = db.Roles.AsNoTracking().First(r => r.Nombre == "ADMIN");
            var gerente = db.Roles.AsNoTracking().First(r => r.Nombre == "GERENTE");
            var cajero = db.Roles.AsNoTracking().First(r => r.Nombre == "CAJERO");

            // ADMIN: todos los permisos.
            db.RolPermisos.AddRange(permisos.Select(p => new RolPermiso { RolId = admin.Id, PermisoId = p.Id }));

            // GERENTE: todo menos gestión de usuarios y configuración.
            var excluyeGerente = new[] { "usuarios.gestionar", "config.gestionar" };
            db.RolPermisos.AddRange(permisos
                .Where(p => !excluyeGerente.Contains(p.Clave))
                .Select(p => new RolPermiso { RolId = gerente.Id, PermisoId = p.Id }));

            // CAJERO: punto de caja + consulta.
            var cajeroClaves = new[]
            {
                "clientes.ver", "clientes.crear", "operaciones.ver", "operaciones.crear",
                "caja.ver", "caja.mover", "bancos.ver", "transferencias.ver", "custodios.ver"
            };
            db.RolPermisos.AddRange(permisos
                .Where(p => cajeroClaves.Contains(p.Clave))
                .Select(p => new RolPermiso { RolId = cajero.Id, PermisoId = p.Id }));
            db.SaveChanges();
        }

        if (!db.Usuarios.Any(u => u.Id == "ADMIN"))
        {
            db.Usuarios.Add(new Usuario
            {
                Id = "ADMIN",
                Nombre = "Administrador",
                ClaveHash = HashAdminInicial,
                Tipo = 1,
                Activo = true,
                Bloqueado = false
            });
            db.SaveChanges();

            var rolAdmin = db.Roles.AsNoTracking().First(r => r.Nombre == "ADMIN");
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = "ADMIN", RolId = rolAdmin.Id });
        }
    }
}
