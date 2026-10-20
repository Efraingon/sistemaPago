using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Libro de caja (VFP: CAJA). Clave compuesta (ejercicio, id); el recibo es único global.
/// origen ∈ IN,EG,CO,VE,RE,EN,C1,C2,DE,DS,IB,EB,T5,T6.
/// </summary>
[Table("caja")]
public class Caja
{
    /// <summary>Correlativo de 10 dígitos por año.</summary>
    [Column("id")]
    [MaxLength(10)]
    public string Id { get; set; } = "";

    [Column("ejercicio")]
    [MaxLength(4)]
    public string Ejercicio { get; set; } = "";

    [Column("origen")]
    [MaxLength(2)]
    public string Origen { get; set; } = "";

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    /// <summary>Correlativo de 20 dígitos global.</summary>
    [Column("recibo")]
    [MaxLength(20)]
    public string Recibo { get; set; } = "";

    [Column("monto")]
    [Precision(14, 2)]
    public decimal Monto { get; set; }

    [Column("descripcion")]
    [MaxLength(100)]
    public string? Descripcion { get; set; }

    [Column("observaciones")]
    [MaxLength(200)]
    public string? Observaciones { get; set; }

    [Column("moneda_id")]
    [MaxLength(4)]
    public string? MonedaId { get; set; }

    [Column("cliente_id")]
    [MaxLength(10)]
    public string? ClienteId { get; set; }

    [Column("compra_id")]
    [MaxLength(16)]
    public string? CompraId { get; set; }

    [Column("venta_id")]
    [MaxLength(16)]
    public string? VentaId { get; set; }

    [Column("banco_id")]
    [MaxLength(5)]
    public string? BancoId { get; set; }

    [Column("cuenta_tran")]
    [MaxLength(20)]
    public string? CuentaTran { get; set; }

    [Column("numero_tran")]
    [MaxLength(20)]
    public string? NumeroTran { get; set; }

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

    [Column("centro_costo")]
    [MaxLength(80)]
    public string? CentroCosto { get; set; }

    [Column("cierre_fecha")]
    public DateTime? CierreFecha { get; set; }

    [Column("cierre_id")]
    [MaxLength(15)]
    public string? CierreId { get; set; }

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
