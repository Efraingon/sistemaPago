using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Codificador de ingresos de caja (VFP: CODIINGRESO).</summary>
[Table("codificacion_ingreso")]
public class CodificacionIngreso
{
    [Key]
    [Column("id")]
    [MaxLength(5)]
    public string Id { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(50)]
    public string Descripcion { get; set; } = "";

    [Column("cuenta_contable")]
    [MaxLength(10)]
    public string? CuentaContable { get; set; }
}
