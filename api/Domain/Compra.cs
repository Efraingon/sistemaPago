using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Cabecera de compra de divisa (VFP: COMPRAS).
/// El id se forma como ejercicio + 12 dígitos secuenciales (ej. '20260000000001').
/// </summary>
[Table("compra")]
public class Compra : IOperacionCabecera
{
    [Key]
    [Column("id")]
    [MaxLength(16)]
    public string Id { get; set; } = "";

    [Column("ejercicio")]
    [MaxLength(4)]
    public string Ejercicio { get; set; } = "";

    [Column("cliente_id")]
    [MaxLength(10)]
    public string ClienteId { get; set; } = "";

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("moneda_divisa_id")]
    [MaxLength(4)]
    public string MonedaDivisaId { get; set; } = "0001";

    [Column("moneda_nac_id")]
    [MaxLength(4)]
    public string MonedaNacId { get; set; } = "0002";

    [Column("mto_x_divisa")]
    [Precision(14, 2)]
    public decimal? MtoXDivisa { get; set; }

    [Column("mto_x_nac")]
    [Precision(14, 2)]
    public decimal? MtoXNac { get; set; }

    [Column("tasa")]
    [Precision(14, 7)]
    public decimal Tasa { get; set; }

    [Column("cantidad")]
    [Precision(14, 2)]
    public decimal Cantidad { get; set; }

    [Column("monto_bs")]
    [Precision(14, 2)]
    public decimal MontoBs { get; set; }

    [Column("pagado_bs")]
    [Precision(14, 2)]
    public decimal PagadoBs { get; set; }

    [Column("saldo_bs")]
    [Precision(14, 2)]
    public decimal SaldoBs { get; set; }

    [Column("recibido_divisa")]
    [Precision(14, 2)]
    public decimal RecibidoDivisa { get; set; }

    [Column("por_recibir")]
    [Precision(14, 2)]
    public decimal PorRecibir { get; set; }

    [Column("observaciones")]
    [MaxLength(80)]
    public string? Observaciones { get; set; }

    [Column("centro_costo")]
    [MaxLength(80)]
    public string? CentroCosto { get; set; }

    [Column("estado")]
    [MaxLength(10)]
    public string Estado { get; set; } = "ACTIVO";

    [Column("anulado_en")]
    public DateTime? AnuladoEn { get; set; }

    [Column("anulado_motivo")]
    [MaxLength(200)]
    public string? AnuladoMotivo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.Now;
}
