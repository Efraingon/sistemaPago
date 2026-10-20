using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Entidad bancaria (VFP: BANCOS). saldo es un campo derivado cacheado.</summary>
[Table("banco")]
public class Banco
{
    [Key]
    [Column("id")]
    [MaxLength(5)]
    public string Id { get; set; } = "";

    [Column("nombre")]
    [MaxLength(100)]
    public string Nombre { get; set; } = "";

    [Column("direccion")]
    [MaxLength(250)]
    public string? Direccion { get; set; }

    [Column("telefonos")]
    [MaxLength(50)]
    public string? Telefonos { get; set; }

    [Column("fax")]
    [MaxLength(50)]
    public string? Fax { get; set; }

    [Column("correo")]
    [MaxLength(50)]
    public string? Correo { get; set; }

    [Column("contacto")]
    [MaxLength(50)]
    public string? Contacto { get; set; }

    /// <summary>1 = nacional, 2 = internacional.</summary>
    [Column("nacional")]
    public short Nacional { get; set; } = 1;

    [Column("saldo")]
    [Precision(14, 2)]
    public decimal Saldo { get; set; }

    [Column("cuenta_contable")]
    [MaxLength(10)]
    public string? CuentaContable { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    public List<CuentaBanco> Cuentas { get; set; } = [];
}
