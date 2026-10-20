using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Cuentas propias de la casa en cada banco (VFP: CTAS / CTASBANCO).</summary>
[Table("cuenta_banco")]
public class CuentaBanco
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("banco_id")]
    [MaxLength(5)]
    public string BancoId { get; set; } = "";

    [Column("numero_cuenta")]
    [MaxLength(20)]
    public string NumeroCuenta { get; set; } = "";

    [Column("tipo")]
    [Precision(1, 0)]
    public decimal? Tipo { get; set; }

    [Column("debitos")]
    [Precision(14, 2)]
    public decimal Debitos { get; set; }

    [Column("creditos")]
    [Precision(14, 2)]
    public decimal Creditos { get; set; }

    [Column("saldo")]
    [Precision(14, 2)]
    public decimal Saldo { get; set; }

    [Column("cuenta_contable")]
    [MaxLength(10)]
    public string? CuentaContable { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;

    [ForeignKey(nameof(BancoId))]
    public Banco? Banco { get; set; }
}
