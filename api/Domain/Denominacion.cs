using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Denominaciones de billetes para el arqueo (VFP: CONODOL / CONONAC).</summary>
[Table("denominacion")]
public class Denominacion
{
    [Key]
    [Column("id")]
    [MaxLength(4)]
    public string Id { get; set; } = "";

    [Column("moneda_id")]
    [MaxLength(4)]
    public string MonedaId { get; set; } = "";

    [Column("tipo")]
    [MaxLength(10)]
    public string Tipo { get; set; } = "BILLETE";

    [Column("valor")]
    [Precision(14, 2)]
    public decimal Valor { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;
}
