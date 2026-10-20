using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Catálogo de monedas (VFP: TIPOMONY).</summary>
[Table("tipo_moneda")]
public class TipoMoneda
{
    [Key]
    [Column("id")]
    [MaxLength(4)]
    public string Id { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(10)]
    public string Descripcion { get; set; } = "";

    [Column("monto_dolar")]
    [Precision(14, 7)]
    public decimal MontoDolar { get; set; } = 1m;

    [Column("principal")]
    public bool Principal { get; set; }

    [Column("valor_nacional")]
    [Precision(14, 7)]
    public decimal ValorNacional { get; set; } = 1m;

    [Column("nacional")]
    public bool Nacional { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;
}
