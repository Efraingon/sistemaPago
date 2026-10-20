using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Histórico diario de tasas (VFP: CALENDTASA).</summary>
[Table("calend_tasa")]
public class CalendTasa
{
    [Key]
    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("nombre_dia")]
    [MaxLength(10)]
    public string? NombreDia { get; set; }

    [Column("tasa")]
    [Precision(14, 8)]
    public decimal? Tasa { get; set; }

    [Column("tasa_compra")]
    [Precision(14, 8)]
    public decimal? TasaCompra { get; set; }

    [Column("tasa_venta")]
    [Precision(14, 8)]
    public decimal? TasaVenta { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
