namespace Monrys.Api.Dtos;

// ---------------------------------------------------------------------------
// CAJA
// ---------------------------------------------------------------------------
public class MovimientoCajaFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Origen { get; set; }
    public string? ClienteId { get; set; }
    /// <summary>ACTIVO | ANULADO (vacío = todos).</summary>
    public string? Estado { get; set; }
    public string? Buscar { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
}

public class MovimientoCajaDto
{
    public string Ejercicio { get; set; } = "";
    public string Id { get; set; } = "";
    public string Origen { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Recibo { get; set; } = "";
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public string? Observaciones { get; set; }
    public string? MonedaId { get; set; }
    public string? Moneda { get; set; }
    public string? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public string? CompraId { get; set; }
    public string? VentaId { get; set; }
    public string? BancoId { get; set; }
    public string? CuentaTran { get; set; }
    public string? NumeroTran { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Usd { get; set; }
    public decimal? MontoBs { get; set; }
    public string? CodiIngresoId { get; set; }
    public string? CodiEgresoId { get; set; }
    public string? CentroCosto { get; set; }
    public DateTime? CierreFecha { get; set; }
    public string? CierreId { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public string? AnuladoMotivo { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class MovimientoCajaCrearDto
{
    public DateTime? Fecha { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public string? Observaciones { get; set; }
    /// <summary>Por defecto la moneda nacional (Bs). Con una divisa el origen pasa a DE/DS.</summary>
    public string? MonedaId { get; set; }
    public string? ClienteId { get; set; }
    public string? BancoId { get; set; }
    public string? CuentaTran { get; set; }
    public string? NumeroTran { get; set; }
    public string? CodiIngresoId { get; set; }
    public string? CodiEgresoId { get; set; }
    public string? CentroCosto { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Usd { get; set; }
    public decimal? MontoBs { get; set; }
}

public class AnulacionDto
{
    public string? Motivo { get; set; }
}

public class CierreCajaDto
{
    public DateTime Fecha { get; set; }
}

public class CierreCajaEstadoDto
{
    public DateTime Fecha { get; set; }
    public bool Cerrado { get; set; }
    public int Movimientos { get; set; }
    public decimal Total { get; set; }
    public string? CerradoPor { get; set; }
}

public class CierreCajaResultadoDto
{
    public DateTime Fecha { get; set; }
    public int MovimientosCerrados { get; set; }
    public string CerradoPor { get; set; } = "";
}

public class ArqueoDto
{
    public DateTime Fecha { get; set; }
    public List<ArqueoMonedaDto> Monedas { get; set; } = [];
}

public class ArqueoMonedaDto
{
    public string MonedaId { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Nacional { get; set; }
    /// <summary>Saldo en caja de esa moneda a la fecha indicada (no anulados, sin cerrar).</summary>
    public decimal TotalCaja { get; set; }
    public List<ArqueoDenominacionDto> Denominaciones { get; set; } = [];
}

public class ArqueoDenominacionDto
{
    public string Id { get; set; } = "";
    public string Tipo { get; set; } = "BILLETE";
    public decimal Valor { get; set; }
    /// <summary>Cantidad contada por el cajero (la llena el frontend; 0 = sin contar).</summary>
    public int Cantidad { get; set; }
    /// <summary>Valor total contado (Valor × Cantidad).</summary>
    public decimal Total { get; set; }
}

// ---------------------------------------------------------------------------
// CUSTODIO (COMPENSACIONES)
// ---------------------------------------------------------------------------
public class CustodioSaldoDto
{
    public string ClienteId { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string MonedaId { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal Disponible { get; set; }
    public decimal DisponibleBs { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public class CustodioMovimientoDto
{
    public string Id { get; set; } = "";
    public string Ejercicio { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string ClienteId { get; set; } = "";
    public string MonedaId { get; set; } = "";
    public decimal Abonos { get; set; }
    public decimal Retiros { get; set; }
    public string? ReciboTran { get; set; }
    public string? CentroCosto { get; set; }
    public string? Observaciones { get; set; }
    public string? ReversoDe { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class CustodioMoverDto
{
    public string ClienteId { get; set; } = "";
    public string MonedaId { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string? CentroCosto { get; set; }
    /// <summary>Registra también el movimiento de caja C1/C2 (por defecto sí, como en VFP).</summary>
    public bool DeCaja { get; set; } = true;
}

public class CustodioMoverResultadoDto
{
    public string CompensacionId { get; set; } = "";
    public string Recibo { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal Monto { get; set; }
    public decimal Disponible { get; set; }
    public string? ReciboCaja { get; set; }
    public string? IdCaja { get; set; }
}

// ---------------------------------------------------------------------------
// TRANSFERENCIAS
// ---------------------------------------------------------------------------
public class TransferenciaFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? ClienteId { get; set; }
    public string? Origen { get; set; }
    public string? BancoId { get; set; }
    public string? Estado { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 20;
}

public class TransferenciaCrearDto
{
    /// <summary>CO, VE, CU, DP, RT, T1..T6.</summary>
    public string Origen { get; set; } = "";
    public string? TipoTran { get; set; }
    public DateTime? Fecha { get; set; }
    public string? NumeroTran { get; set; }
    public string? CuentaTran { get; set; }
    public string? Beneficiario { get; set; }
    public decimal Monto { get; set; }
    public string? ClienteId { get; set; }
    public string? Motivo { get; set; }
    public string? CompraId { get; set; }
    public string? VentaId { get; set; }
    public string? MonedaId { get; set; }
    public string? BancoId { get; set; }
    public string? CuentaClie { get; set; }
    public bool DeCaja { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Usd { get; set; }
    public decimal? MontoBs { get; set; }
    public string? CodiIngresoId { get; set; }
    public string? CodiEgresoId { get; set; }
}

public class TransferenciaDto
{
    public long Id { get; set; }
    public string Origen { get; set; } = "";
    public string? TipoTran { get; set; }
    public DateTime Fecha { get; set; }
    public string? NumeroTran { get; set; }
    public string? CuentaTran { get; set; }
    public string? Beneficiario { get; set; }
    public decimal Monto { get; set; }
    public string? ClienteId { get; set; }
    public string? Cliente { get; set; }
    public string? Motivo { get; set; }
    public string? CompraId { get; set; }
    public string? VentaId { get; set; }
    public string? MonedaId { get; set; }
    public string? BancoId { get; set; }
    public string? Banco { get; set; }
    public string? CuentaClie { get; set; }
    public bool DeCaja { get; set; }
    public decimal? Tasa { get; set; }
    public decimal? Usd { get; set; }
    public decimal? MontoBs { get; set; }
    public string? CodiIngresoId { get; set; }
    public string? CodiEgresoId { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public string? AnuladoMotivo { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public DateTime CreadoEn { get; set; }
}
