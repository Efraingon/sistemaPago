using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Saldo de custodia cacheado por (cliente, moneda) (VFP: CUSTODIO).</summary>
[Table("custodio")]
public class Custodio
{
    [Key]
    [Column("cliente_id")]
    [MaxLength(10)]
    public string ClienteId { get; set; } = "";

    [Key]
    [Column("moneda_id")]
    [MaxLength(4)]
    public string MonedaId { get; set; } = "";

    [Column("disponible")]
    [Precision(14, 2)]
    public decimal Disponible { get; set; }

    [Column("disponible_bs")]
    [Precision(14, 2)]
    public decimal DisponibleBs { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.Now;

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }
}
