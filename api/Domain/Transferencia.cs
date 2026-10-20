using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Libro de transferencias bancarias (VFP: CTASTRAN).</summary>
[Table("transferencia")]
public class Transferencia
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Origen: CO,VE,CU,DP,RT,T1..T6.</summary>
    [Column("origen")]
    [MaxLength(2)]
    public string Origen { get; set; } = "";

    [Column("tipo_tran")]
    [MaxLength(2)]
    public string? TipoTran { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("numero_tran")]
    [MaxLength(20)]
    public string? NumeroTran { get; set; }

    [Column("cuenta_tran")]
    [MaxLength(20)]
    public string? CuentaTran { get; set; }

    [Column("beneficiario")]
    [MaxLength(100)]
    public string? Beneficiario { get; set; }

    [Column("monto")]
    [Precision(14, 2)]
    public decimal Monto { get; set; }

    [Column("cliente_id")]
    [MaxLength(10)]
    public string? ClienteId { get; set; }

    [Column("motivo")]
    [MaxLength(100)]
    public string? Motivo { get; set; }

    [Column("compra_id")]
    [MaxLength(16)]
    public string? CompraId { get; set; }

    [Column("venta_id")]
    [MaxLength(16)]
    public string? VentaId { get; set; }

    [Column("moneda_id")]
    [MaxLength(4)]
    public string? MonedaId { get; set; }

    [Column("banco_id")]
    [MaxLength(5)]
    public string? BancoId { get; set; }

    [Column("cuenta_clie")]
    [MaxLength(20)]
    public string? CuentaClie { get; set; }

    [Column("de_caja")]
    public bool DeCaja { get; set; }

    [Column("tasa")]
    [Precision(14, 7)]
    public decimal? Tasa { get; set; }

    [Column("usd")]
    [Precision(14, 2)]
    public decimal? Usd { get; set; }

    [Column("monto_bs")]
    [Precision(14, 2)]
    public decimal? MontoBs { get; set; }

    [Column("codi_ingreso_id")]
    [MaxLength(5)]
    public string? CodiIngresoId { get; set; }

    [Column("codi_egreso_id")]
    [MaxLength(5)]
    public string? CodiEgresoId { get; set; }

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
}
