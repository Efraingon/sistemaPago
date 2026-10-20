using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Cuota de pago en Bs sobre una operación
/// (VFP: COMPAGAR en compras / VENPAGAR en ventas).
/// tipo_op: 'C' = compra, 'V' = venta.
/// </summary>
[Table("operacion_pago")]
public class OperacionPago
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

    [Column("monto")]
    [Precision(14, 2)]
    public decimal Monto { get; set; }

    [Column("banco_debito_id")]
    [MaxLength(5)]
    public string? BancoDebitoId { get; set; }

    [Column("cta_debito")]
    [MaxLength(20)]
    public string? CtaDebito { get; set; }

    [Column("banco_credito_id")]
    [MaxLength(5)]
    public string? BancoCreditoId { get; set; }

    [Column("cta_credito")]
    [MaxLength(20)]
    public string? CtaCredito { get; set; }

    [Column("recibo")]
    [MaxLength(20)]
    public string? Recibo { get; set; }

    [Column("tipo_pago")]
    [MaxLength(15)]
    public string? TipoPago { get; set; }

    [Column("saldo")]
    [Precision(14, 2)]
    public decimal? Saldo { get; set; }

    [Column("anulado_en")]
    public DateTime? AnuladoEn { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
