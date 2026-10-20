using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Rol declarativo (reemplaza el string posicional de permisos de VFP).</summary>
[Table("rol")]
public class Rol
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [MaxLength(50)]
    public string Nombre { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(200)]
    public string? Descripcion { get; set; }
}
