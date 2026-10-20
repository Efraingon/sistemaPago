using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Auditoría de cada mutación (VFP: BITACORA).</summary>
[Table("bitacora")]
public class Bitacora
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column("usuario_id")]
    [MaxLength(10)]
    public string? UsuarioId { get; set; }

    [Column("tipo")]
    public int? Tipo { get; set; }

    [Column("accion")]
    [MaxLength(200)]
    public string Accion { get; set; } = "";

    [Column("observaciones")]
    [MaxLength(200)]
    public string? Observaciones { get; set; }

    [Column("equipo")]
    [MaxLength(50)]
    public string? Equipo { get; set; }

    [Column("tabla")]
    [MaxLength(60)]
    public string? Tabla { get; set; }

    [Column("registro_id")]
    [MaxLength(30)]
    public string? RegistroId { get; set; }

    /// <summary>JSON con el estado previo del registro afectado (JSONB en PostgreSQL).</summary>
    [Column("datos_previos")]
    public string? DatosPrevios { get; set; }
}
