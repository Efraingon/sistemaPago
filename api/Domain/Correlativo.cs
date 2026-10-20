using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>
/// Contadores transaccionales de correlativos (numeración por año; los contadores
/// globales usan ejercicio = 'GLOBAL', por lo que la columna admite hasta 6 caracteres
/// aunque en el VFP fuera CHAR(4)).
/// </summary>
[Table("correlativo")]
public class Correlativo
{
    [Key]
    [Column("tabla")]
    [MaxLength(30)]
    public string Tabla { get; set; } = "";

    [Key]
    [Column("ejercicio")]
    [MaxLength(6)]
    public string Ejercicio { get; set; } = "";

    [Column("ultimo")]
    public long Ultimo { get; set; }
}
