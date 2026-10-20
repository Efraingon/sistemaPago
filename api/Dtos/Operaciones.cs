namespace Monrys.Api.Dtos;

public class OperacionFiltroDto
{
    public string? ClienteId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    /// <summary>ACTIVO | ANULADO (vacío = todos).</summary>
    public string? Estado { get; set; }
    public string? Buscar { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
}

public class OperacionCrearDto
{
    public string ClienteId { get; set; } = "";
    public DateTime? Fecha { get; set; }
    /// <summary>Moneda divisa de la operación (por defecto 0001 = USD).</summary>
    public string? MonedaId { get; set; }
    public decimal Cantidad { get; set; }
    /// <summary>Tasa opcional; si no viene se usa tasa_compra (compra) o tasa_venta (venta).</summary>
    public decimal? Tasa { get; set; }
    public string? Observaciones { get; set; }
    public string? CentroCosto { get; set; }
}

public class OperacionListaDto
{
    public string Id { get; set; } = "";
    public string Ejercicio { get; set; } = "";
    public string ClienteId { get; set; } = "";
    public string? Cliente { get; set; }
    public DateTime Fecha { get; set; }
    public string MonedaDivisaId { get; set; } = "";
    public string? Moneda { get; set; }
    public decimal Tasa { get; set; }
    public decimal Cantidad { get; set; }
    public decimal MontoBs { get; set; }
    public decimal PagadoBs { get; set; }
    public decimal SaldoBs { get; set; }
    public decimal RecibidoDivisa { get; set; }
    public decimal PorRecibir { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public string? AnuladoMotivo { get; set; }
}

public class OperacionDetalleDto : OperacionListaDto
{
    public string MonedaNacId { get; set; } = "";
    public string? Observaciones { get; set; }
    public string? CentroCosto { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public List<RecepcionDto> Recepciones { get; set; } = [];
    public List<PagoDto> Pagos { get; set; } = [];
}

public class RecepcionDto
{
    public long Id { get; set; }
    public string TipoOp { get; set; } = "";
    public string OperacionId { get; set; } = "";
    public int NroPer { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? PorRecibir { get; set; }
    public string? Recibo { get; set; }
    public string? Recibo2 { get; set; }
    public string? TipoPago { get; set; }
    public bool Compensado { get; set; }
    public bool DeCaja { get; set; }
    public bool DeBanco { get; set; }
    public string? BancoDebitoId { get; set; }
    public string? BancoCreditoId { get; set; }
    public string? CtaDebito { get; set; }
    public string? CtaCredito { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class PagoDto
{
    public long Id { get; set; }
    public string TipoOp { get; set; } = "";
    public string OperacionId { get; set; } = "";
    public int NroPer { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public decimal? Saldo { get; set; }
    public string? Recibo { get; set; }
    public string? TipoPago { get; set; }
    public string? BancoDebitoId { get; set; }
    public string? BancoCreditoId { get; set; }
    public string? CtaDebito { get; set; }
    public string? CtaCredito { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class RecepcionCrearDto
{
    public DateTime? Fecha { get; set; }
    /// <summary>Cantidad de divisa recibida/cobrada (siempre positiva).</summary>
    public decimal Cantidad { get; set; }
    /// <summary>Genera el movimiento de caja (RE en compras / EN en ventas).</summary>
    public bool DeCaja { get; set; } = true;
    public bool DeBanco { get; set; }
    /// <summary>Opera contra el custodio del cliente (abono en venta / retiro en compra).</summary>
    public bool Custodio { get; set; }
    /// <summary>CAJA | BANCO | COMPENSACION (se deriva de las banderas si viene vacío).</summary>
    public string? TipoPago { get; set; }
    public string? BancoDebitoId { get; set; }
    public string? BancoCreditoId { get; set; }
    public string? CtaDebito { get; set; }
    public string? CtaCredito { get; set; }
    public string? Recibo { get; set; }
    public string? Descripcion { get; set; }
    public string? CentroCosto { get; set; }
}

public class PagoCrearDto
{
    public DateTime? Fecha { get; set; }
    /// <summary>Monto en Bs (siempre positiva).</summary>
    public decimal Monto { get; set; }
    /// <summary>Genera el movimiento de caja (CO en compras / VE en ventas).</summary>
    public bool DeCaja { get; set; } = true;
    public string? TipoPago { get; set; }
    public string? BancoDebitoId { get; set; }
    public string? BancoCreditoId { get; set; }
    public string? CtaDebito { get; set; }
    public string? CtaCredito { get; set; }
    public string? Recibo { get; set; }
}

public class OperacionCuotaResultadoDto
{
    public string OperacionId { get; set; } = "";
    public decimal RecibidoDivisa { get; set; }
    public decimal PorRecibir { get; set; }
    public decimal PagadoBs { get; set; }
    public decimal SaldoBs { get; set; }
    public int NroPer { get; set; }
    public string? Recibo { get; set; }
    public string? ReciboCaja { get; set; }
}
