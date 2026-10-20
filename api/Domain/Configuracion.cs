using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Fila única de configuración global (VFP: DOLARMAIN).</summary>
[Table("configuracion")]
public class Configuracion
{
    [Key]
    [Column("id")]
    public int Id { get; set; } = 1;

    [Column("nro_cliente")]
    public int? NroCliente { get; set; }

    [Column("nombre_empresa")]
    [MaxLength(100)]
    public string NombreEmpresa { get; set; } = "";

    [Column("direccion")]
    [MaxLength(120)]
    public string? Direccion { get; set; }

    [Column("telefono")]
    [MaxLength(50)]
    public string? Telefono { get; set; }

    [Column("fax")]
    [MaxLength(20)]
    public string? Fax { get; set; }

    [Column("rif")]
    [MaxLength(20)]
    public string? Rif { get; set; }

    [Column("logo")]
    public byte[]? Logo { get; set; }

    [Column("iva")]
    [Precision(6, 2)]
    public decimal Iva { get; set; } = 16.00m;

    [Column("impute_deba")]
    [MaxLength(1)]
    public string? ImputeDeba { get; set; }

    [Column("porc_deba")]
    [Precision(6, 2)]
    public decimal? PorcDeba { get; set; }

    [Column("agente_retencion")]
    public bool AgenteRetencion { get; set; }

    [Column("serial_contable")]
    [MaxLength(20)]
    public string? SerialContable { get; set; }

    [Column("reteiva_se")]
    [Precision(6, 2)]
    public decimal? ReteIvaSe { get; set; }

    [Column("ruta_data")]
    [MaxLength(80)]
    public string? RutaData { get; set; }

    [Column("moneda_principal_id")]
    [MaxLength(4)]
    public string MonedaPrincipalId { get; set; } = "0001";

    [Column("moneda_nacional_id")]
    [MaxLength(4)]
    public string MonedaNacionalId { get; set; } = "0002";

    [Column("tasa_activa")]
    [Precision(14, 7)]
    public decimal TasaActiva { get; set; }

    [Column("tasa_compra")]
    [Precision(14, 7)]
    public decimal TasaCompra { get; set; }

    [Column("tasa_venta")]
    [Precision(14, 7)]
    public decimal TasaVenta { get; set; }

    [Column("version")]
    [MaxLength(10)]
    public string? Version { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.Now;
}
