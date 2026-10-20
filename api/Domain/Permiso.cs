using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Permiso granular del sistema (ej. 'clientes.crear').</summary>
[Table("permiso")]
public class Permiso
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("clave")]
    [MaxLength(60)]
    public string Clave { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(100)]
    public string Descripcion { get; set; } = "";
}
