using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monrys.Api.Domain;

/// <summary>Cuentas bancarias de los clientes (VFP: CTASCLIE).</summary>
[Table("cuenta_cliente")]
public class CuentaCliente
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("banco_id")]
    [MaxLength(5)]
    public string BancoId { get; set; } = "";

    [Column("cliente_id")]
    [MaxLength(10)]
    public string ClienteId { get; set; } = "";

    [Column("numero_cuenta")]
    [MaxLength(20)]
    public string NumeroCuenta { get; set; } = "";

    [Column("cuenta_contable")]
    [MaxLength(10)]
    public string? CuentaContable { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;

    [ForeignKey(nameof(BancoId))]
    public Banco? Banco { get; set; }

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }
}
