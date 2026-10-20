using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Libro mayor de custodia de divisa por cliente (VFP: COMPENSAC).
/// Invariante: custodio.disponible = Σ abonos − Σ retiros.
/// abonos  = el cliente entrega divisa al sistema (custodia aumenta);
/// retiros = el cliente retira divisa de su custodia (custodia disminuye).
/// </summary>
[Table("compensacion")]
public class Compensacion
{
    /// <summary>Correlativo de 10 dígitos por año (VFP: ID_TRANS).</summary>
    [Column("id")]
    [MaxLength(10)]
    public string Id { get; set; } = "";

    [Column("ejercicio")]
    [MaxLength(4)]
    public string Ejercicio { get; set; } = "";

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("descripcion")]
    [MaxLength(50)]
    public string? Descripcion { get; set; }

    [Column("cliente_id")]
    [MaxLength(10)]
    public string ClienteId { get; set; } = "";

    [Column("moneda_id")]
    [MaxLength(4)]
    public string MonedaId { get; set; } = "";

    [Column("abonos")]
    [Precision(14, 2)]
    public decimal Abonos { get; set; }

    [Column("retiros")]
    [Precision(14, 2)]
    public decimal Retiros { get; set; }

    [Column("recibo_tran")]
    [MaxLength(20)]
    public string? ReciboTran { get; set; }

    [Column("cta_tran")]
    [MaxLength(20)]
    public string? CtaTran { get; set; }

    [Column("centro_costo")]
    [MaxLength(80)]
    public string? CentroCosto { get; set; }

    [Column("observaciones")]
    [MaxLength(30)]
    public string? Observaciones { get; set; }

    [Column("reverso_de")]
    [MaxLength(20)]
    public string? ReversoDe { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
