using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Cliente (VFP: CLIENTES). persona: 1 = natural, 2 = jurídica.</summary>
[Table("cliente")]
public class Cliente
{
    [Key]
    [Column("id")]
    [MaxLength(10)]
    public string Id { get; set; } = "";

    [Column("persona")]
    public int Persona { get; set; } = 1;

    [Column("razon_social")]
    [MaxLength(200)]
    public string? RazonSocial { get; set; }

    [Column("nombres")]
    [MaxLength(50)]
    public string? Nombres { get; set; }

    [Column("apellidos")]
    [MaxLength(50)]
    public string? Apellidos { get; set; }

    [Column("rif")]
    [MaxLength(15)]
    public string? Rif { get; set; }

    [Column("cedula")]
    [MaxLength(15)]
    public string? Cedula { get; set; }

    [Column("direccion")]
    [MaxLength(200)]
    public string? Direccion { get; set; }

    [Column("telefonos")]
    [MaxLength(100)]
    public string? Telefonos { get; set; }

    [Column("email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Column("observaciones")]
    [MaxLength(200)]
    public string? Observaciones { get; set; }

    [Column("cuenta_contable")]
    [MaxLength(10)]
    public string? CuentaContable { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.Now;

    [NotMapped]
    public string NombreCompleto =>
        Persona == 2
            ? (RazonSocial ?? "").Trim()
            : string.Join(" ", new[] { Nombres, Apellidos }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

    public List<CentroCosto> Centros { get; set; } = [];
    public List<CuentaCliente> Cuentas { get; set; } = [];
}
