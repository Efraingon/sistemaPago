using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Centro de costo del cliente (VFP: CENTROSCLIE).</summary>
[Table("centro_costo")]
public class CentroCosto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("cliente_id")]
    [MaxLength(10)]
    public string ClienteId { get; set; } = "";

    [Column("descripcion")]
    [MaxLength(80)]
    public string Descripcion { get; set; } = "";

    [Column("principal")]
    public bool Principal { get; set; }

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }
}
