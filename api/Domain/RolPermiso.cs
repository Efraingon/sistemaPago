using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Asociación rol ⇄ permiso.</summary>
[Table("rol_permiso")]
public class RolPermiso
{
    [Key]
    [Column("rol_id")]
    public int RolId { get; set; }

    [Key]
    [Column("permiso_id")]
    public int PermisoId { get; set; }

    [ForeignKey(nameof(RolId))]
    public Rol? Rol { get; set; }

    [ForeignKey(nameof(PermisoId))]
    public Permiso? Permiso { get; set; }
}
