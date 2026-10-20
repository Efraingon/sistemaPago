namespace Monrys.Api.Dtos;

// ---------------------------------------------------------------------------
// CLIENTES
// ---------------------------------------------------------------------------
public class ClienteCrearDto
{
    /// <summary>1 = natural, 2 = jurídica.</summary>
    public int Persona { get; set; } = 1;
    public string? RazonSocial { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Rif { get; set; }
    public string? Cedula { get; set; }
    public string? Direccion { get; set; }
    public string? Telefonos { get; set; }
    public string? Email { get; set; }
    public string? Observaciones { get; set; }
    public string? CuentaContable { get; set; }
    public bool Activo { get; set; } = true;
}

public class ClienteListaDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? RazonSocial { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public int Persona { get; set; }
    public string? Rif { get; set; }
    public string? Cedula { get; set; }
    public string? Telefonos { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class ClienteDetalleDto : ClienteListaDto
{
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
    public string? CuentaContable { get; set; }
    public DateTime ActualizadoEn { get; set; }
    public List<CentroCostoDto> Centros { get; set; } = [];
    public List<CuentaClienteDto> Cuentas { get; set; } = [];
}

public class CentroCostoDto
{
    public long Id { get; set; }
    public string ClienteId { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Principal { get; set; }
}

public class CentroCostoCrearDto
{
    public string Descripcion { get; set; } = "";
    public bool Principal { get; set; }
}

public class CuentaClienteDto
{
    public long Id { get; set; }
    public string BancoId { get; set; } = "";
    public string? BancoNombre { get; set; }
    public string ClienteId { get; set; } = "";
    public string NumeroCuenta { get; set; } = "";
    public string? CuentaContable { get; set; }
    public bool Activa { get; set; }
}

public class CuentaClienteCrearDto
{
    public string BancoId { get; set; } = "";
    public string NumeroCuenta { get; set; } = "";
    public string? CuentaContable { get; set; }
    public bool Activa { get; set; } = true;
}

// ---------------------------------------------------------------------------
// BANCOS Y CUENTAS DE LA CASA
// ---------------------------------------------------------------------------
public class BancoCrearDto
{
    public string? Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefonos { get; set; }
    public string? Fax { get; set; }
    public string? Correo { get; set; }
    public string? Contacto { get; set; }
    /// <summary>1 = nacional, 2 = internacional.</summary>
    public int Nacional { get; set; } = 1;
    public string? CuentaContable { get; set; }
    public bool Activo { get; set; } = true;
}

public class BancoListaDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefonos { get; set; }
    public string? Correo { get; set; }
    public string? Contacto { get; set; }
    public int Nacional { get; set; }
    public decimal Saldo { get; set; }
    public string? CuentaContable { get; set; }
    public bool Activo { get; set; }
    public List<CuentaBancoDto> Cuentas { get; set; } = [];
}

public class CuentaBancoDto
{
    public long Id { get; set; }
    public string BancoId { get; set; } = "";
    public string NumeroCuenta { get; set; } = "";
    public decimal? Tipo { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    public decimal Saldo { get; set; }
    public string? CuentaContable { get; set; }
    public bool Activa { get; set; }
}

public class CuentaBancoCrearDto
{
    public string NumeroCuenta { get; set; } = "";
    public decimal? Tipo { get; set; }
    public string? CuentaContable { get; set; }
    public bool Activa { get; set; } = true;
}

// ---------------------------------------------------------------------------
// MONEDAS, TASAS Y CONFIGURACIÓN
// ---------------------------------------------------------------------------
public class MonedaDto
{
    public string Id { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal MontoDolar { get; set; }
    public bool Principal { get; set; }
    public decimal ValorNacional { get; set; }
    public bool Nacional { get; set; }
    public bool Activa { get; set; }
}

public class TasaDto
{
    public decimal TasaActiva { get; set; }
    public decimal TasaCompra { get; set; }
    public decimal TasaVenta { get; set; }
    public string MonedaPrincipalId { get; set; } = "";
    public string MonedaNacionalId { get; set; } = "";
    public DateTime ActualizadoEn { get; set; }
}

public class TasaActualizarDto
{
    public decimal TasaActiva { get; set; }
    public decimal TasaCompra { get; set; }
    public decimal TasaVenta { get; set; }
    /// <summary>Actualiza (o crea) el registro del día en calend_tasa. Por defecto true.</summary>
    public bool ActualizarCalendario { get; set; } = true;
}

public class CalendTasaDto
{
    public DateTime Fecha { get; set; }
    public string? NombreDia { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? TasaCompra { get; set; }
    public decimal? TasaVenta { get; set; }
}

public class ConfiguracionDto
{
    public int Id { get; set; }
    public int? NroCliente { get; set; }
    public string NombreEmpresa { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Fax { get; set; }
    public string? Rif { get; set; }
    public decimal Iva { get; set; }
    public bool AgenteRetencion { get; set; }
    public string? SerialContable { get; set; }
    public string MonedaPrincipalId { get; set; } = "";
    public string MonedaNacionalId { get; set; } = "";
    public decimal TasaActiva { get; set; }
    public decimal TasaCompra { get; set; }
    public decimal TasaVenta { get; set; }
    public string? Version { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public class ConfiguracionActualizarDto
{
    public string? NombreEmpresa { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Fax { get; set; }
    public string? Rif { get; set; }
    public decimal? Iva { get; set; }
    public bool? AgenteRetencion { get; set; }
    public string? SerialContable { get; set; }
    public string? MonedaPrincipalId { get; set; }
    public string? MonedaNacionalId { get; set; }
    public decimal? TasaActiva { get; set; }
    public decimal? TasaCompra { get; set; }
    public decimal? TasaVenta { get; set; }
    public string? Version { get; set; }
    public int? NroCliente { get; set; }
}

// ---------------------------------------------------------------------------
// USUARIOS, ROLES Y PERMISOS
// ---------------------------------------------------------------------------
public class UsuarioListaDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Tipo { get; set; }
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UsuarioCrearDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Clave { get; set; } = "";
    public int Tipo { get; set; } = 2;
    public bool Activo { get; set; } = true;
    public List<string>? Roles { get; set; }
}

public class UsuarioActualizarDto
{
    public string? Nombre { get; set; }
    public string? Clave { get; set; }
    public int? Tipo { get; set; }
    public bool? Activo { get; set; }
    public bool? Bloqueado { get; set; }
}

public class RolesUsuarioDto
{
    public List<string> Roles { get; set; } = [];
}

public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public List<PermisoDto> Permisos { get; set; } = [];
}

public class PermisoDto
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

public class CodificacionesDto
{
    public List<CodificacionOpcionDto> Ingresos { get; set; } = [];
    public List<CodificacionOpcionDto> Egresos { get; set; } = [];
}

public class CodificacionOpcionDto
{
    public string Id { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? CuentaContable { get; set; }
}
