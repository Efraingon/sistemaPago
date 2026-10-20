using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Asociación usuario ⇄ rol.</summary>
[Table("usuario_rol")]
public class UsuarioRol
{
    [Key]
    [Column("usuario_id")]
    [MaxLength(10)]
    public string UsuarioId { get; set; } = "";

    [Key]
    [Column("rol_id")]
    public int RolId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }

    [ForeignKey(nameof(RolId))]
    public Rol? Rol { get; set; }
}
