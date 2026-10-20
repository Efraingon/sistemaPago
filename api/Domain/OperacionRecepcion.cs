using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Cuota de recepción de divisa sobre una operación
/// (VFP: COMPRECIB en compras / VENRECIB en ventas).
/// tipo_op: 'C' = compra, 'V' = venta.
/// </summary>
[Table("operacion_recepcion")]
public class OperacionRecepcion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("tipo_op")]
    [MaxLength(1)]
    public string TipoOp { get; set; } = "";

    [Column("operacion_id")]
    [MaxLength(16)]
    public string OperacionId { get; set; } = "";

    [Column("nroper")]
    public int NroPer { get; set; }

    [Column("ejercicio")]
    [MaxLength(4)]
    public string Ejercicio { get; set; } = "";

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("cantidad")]
    [Precision(14, 2)]
    public decimal Cantidad { get; set; }

    [Column("por_recibir")]
    [Precision(14, 2)]
    public decimal? PorRecibir { get; set; }

    [Column("recibo")]
    [MaxLength(20)]
    public string? Recibo { get; set; }

    [Column("recibo2")]
    [MaxLength(20)]
    public string? Recibo2 { get; set; }

    [Column("banco_credito_id")]
    [MaxLength(5)]
    public string? BancoCreditoId { get; set; }

    [Column("banco_debito_id")]
    [MaxLength(5)]
    public string? BancoDebitoId { get; set; }

    [Column("cta_credito")]
    [MaxLength(20)]
    public string? CtaCredito { get; set; }

    [Column("cta_debito")]
    [MaxLength(20)]
    public string? CtaDebito { get; set; }

    [Column("tipo_pago")]
    [MaxLength(15)]
    public string? TipoPago { get; set; }

    [Column("compensado")]
    public bool Compensado { get; set; }

    [Column("de_caja")]
    public bool DeCaja { get; set; }

    [Column("de_banco")]
    public bool DeBanco { get; set; }

    [Column("anulado_en")]
    public DateTime? AnuladoEn { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
