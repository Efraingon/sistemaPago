using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Usuario del sistema (VFP: USUARIOS + hash PBKDF2 y estado).</summary>
[Table("usuario")]
public class Usuario
{
    [Key]
    [Column("id")]
    [MaxLength(10)]
    public string Id { get; set; } = "";

    [Column("nombre")]
    [MaxLength(50)]
    public string Nombre { get; set; } = "";

    [Column("clave_hash")]
    [MaxLength(200)]
    public string ClaveHash { get; set; } = "";

    /// <summary>1 = Administrador.</summary>
    [Column("tipo")]
    public short Tipo { get; set; } = 2;

    [Column("historico_finanzas")]
    [MaxLength(4)]
    public string? HistoricoFinanzas { get; set; }

    [Column("ruta_pantalla")]
    [MaxLength(100)]
    public string? RutaPantalla { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("bloqueado")]
    public bool Bloqueado { get; set; }

    [Column("ultimo_acceso")]
    public DateTime? UltimoAcceso { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
