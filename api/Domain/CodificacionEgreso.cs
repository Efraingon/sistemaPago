using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Codificador de egresos de caja (VFP: CODIEGRESO).</summary>
[Table("codificacion_egreso")]
public class CodificacionEgreso
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
