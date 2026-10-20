using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Catálogo de plan contable (VFP: MAESTRO).</summary>
[Table("plan_contable")]
public class PlanContable
{
    [Key]
    [Column("cuenta")]
    [MaxLength(10)]
    public string Cuenta { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(100)]
    public string Descripcion { get; set; } = "";
}
