using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Log de errores del sistema (VFP: VER_ERROR).</summary>
[Table("log_error")]
public class LogError
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column("usuario_id")]
    [MaxLength(10)]
    public string? UsuarioId { get; set; }

    [Column("nombre_usuario")]
    [MaxLength(50)]
    public string? NombreUsuario { get; set; }

    [Column("mensaje")]
    public string? Mensaje { get; set; }

    [Column("programa")]
    [MaxLength(100)]
    public string? Programa { get; set; }

    [Column("linea")]
    public int? Linea { get; set; }

    [Column("equipo")]
    [MaxLength(50)]
    public string? Equipo { get; set; }

    [Column("version")]
    [MaxLength(10)]
    public string? Version { get; set; }
}
