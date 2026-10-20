namespace Monrys.Api.Dtos;

// ---------------------------------------------------------------------------
// CONSOLIDADO
// ---------------------------------------------------------------------------
public class ConsolidadoDto
{
    public DateTime Fecha { get; set; }
    public decimal TasaActiva { get; set; }
    public List<SaldoGrupoDto> Caja { get; set; } = [];
    public List<SaldoGrupoDto> Custodio { get; set; } = [];
    public decimal SaldoBancoBs { get; set; }
    public decimal TotalCajaBs { get; set; }
    public decimal TotalCustodioBs { get; set; }
    public decimal TotalBs { get; set; }
    public decimal TotalUsd { get; set; }
}

public class SaldoGrupoDto
{
    public string MonedaId { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Nacional { get; set; }
    public decimal Saldo { get; set; }
    public decimal SaldoBs { get; set; }
}

// ---------------------------------------------------------------------------
// ESTADO GENERAL POR CLIENTE
// ---------------------------------------------------------------------------
public class EstadoGeneralDto
{
    public DateTime FechaCorte { get; set; }
    public decimal TasaActiva { get; set; }
    public List<EstadoGeneralClienteDto> Clientes { get; set; } = [];
    public EstadoGeneralTotalesDto Totales { get; set; } = new();
}

public class EstadoGeneralClienteDto
{
    public string ClienteId { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string? Rif { get; set; }
    public string? Cedula { get; set; }
    public decimal ComprasBs { get; set; }
    public decimal PagadoComprasBs { get; set; }
    public decimal SaldoComprasBs { get; set; }
    public decimal VentasBs { get; set; }
    public decimal CobradoVentasBs { get; set; }
    public decimal SaldoVentasBs { get; set; }
    public decimal CustodioDivisa { get; set; }
    public decimal CustodioBs { get; set; }
    /// <summary>Saldo por cobrar (ventas) − saldo por pagar (compras) + custodio en Bs.</summary>
    public decimal ConsolidadoBs { get; set; }
    public List<SaldoGrupoDto> Custodios { get; set; } = [];
}

public class EstadoGeneralTotalesDto
{
    public decimal ComprasBs { get; set; }
    public decimal VentasBs { get; set; }
    public decimal SaldoComprasBs { get; set; }
    public decimal SaldoVentasBs { get; set; }
    public decimal CustodioBs { get; set; }
    public decimal ConsolidadoBs { get; set; }
}

// ---------------------------------------------------------------------------
// CUADRE DE CAJA
// ---------------------------------------------------------------------------
public class CuadreCajaDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public List<CuadreCajaMonedaDto> Monedas { get; set; } = [];
    public decimal EntradasBs { get; set; }
    public decimal SalidasBs { get; set; }
    public decimal SaldoBs { get; set; }
}

public class CuadreCajaMonedaDto
{
    public string MonedaId { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Nacional { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal Entradas { get; set; }
    public decimal Salidas { get; set; }
    public decimal SaldoFinal { get; set; }
    public decimal SaldoAnteriorBs { get; set; }
    public decimal EntradasBs { get; set; }
    public decimal SalidasBs { get; set; }
    public decimal SaldoFinalBs { get; set; }
}

// ---------------------------------------------------------------------------
// CAJA AGRUPADA POR ORIGEN
// ---------------------------------------------------------------------------
public class CajaPorOrigenDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string Tipo { get; set; } = "todos";
    public List<CajaOrigenGrupoDto> Grupos { get; set; } = [];
    public decimal Total { get; set; }
}

public class CajaOrigenGrupoDto
{
    public string Origen { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Sentido { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Entradas { get; set; }
    public decimal Salidas { get; set; }
    public decimal Neto { get; set; }
}

// ---------------------------------------------------------------------------
// ESTADO DE CUENTA DEL CLIENTE (línea de tiempo)
// ---------------------------------------------------------------------------
public class EstadoCuentaDto
{
    public string ClienteId { get; set; } = "";
    public string Cliente { get; set; } = "";
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public decimal Tasa { get; set; }
    public List<EstadoCuentaLineaDto> Lineas { get; set; } = [];
    public decimal TotalEntradasBs { get; set; }
    public decimal TotalSalidasBs { get; set; }
    public decimal SaldoBs { get; set; }
}

public class EstadoCuentaLineaDto
{
    public DateTime Fecha { get; set; }
    /// <summary>CAJA | COMPENSACION | TRANSFERENCIA.</summary>
    public string Tipo { get; set; } = "";
    public string Origen { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Referencia { get; set; }
    public decimal Monto { get; set; }
    public string? MonedaId { get; set; }
    public string? Moneda { get; set; }
    /// <summary>ENTRADA = valor que entra a la casa · SALIDA = valor que sale.</summary>
    public string Sentido { get; set; } = "";
    public decimal MontoBs { get; set; }
    /// <summary>Tasa Bs usada para derivar MontoBs; null cuando la moneda es nacional
    /// o cuando el movimiento ya traía su monto_bs registrado.</summary>
    public decimal? Tasa { get; set; }
    public decimal AcumuladoBs { get; set; }
}

// ---------------------------------------------------------------------------
// COMPROBANTE IMPRIMIBLE
// ---------------------------------------------------------------------------
public class ComprobanteDto
{
    /// <summary>recibo | compra | venta | ingreso | entrega | recepcion.</summary>
    public string Tipo { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
    public ComprobanteEmpresaDto Emisor { get; set; } = new();
    public ComprobanteClienteDto? Cliente { get; set; }
    public decimal? Tasa { get; set; }
    public string? Moneda { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? Monto { get; set; }
    public decimal? MontoBs { get; set; }
    public string? Observaciones { get; set; }
    public string? CentroCosto { get; set; }
    public DateTime? EmitidoEn { get; set; }
    public List<ComprobanteLineaDto> Lineas { get; set; } = [];
}

public class ComprobanteEmpresaDto
{
    public string Nombre { get; set; } = "";
    public string? Rif { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
}

public class ComprobanteClienteDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Rif { get; set; }
    public string? Cedula { get; set; }
    public string? Direccion { get; set; }
    public string? Telefonos { get; set; }
}

public class ComprobanteLineaDto
{
    public string Concepto { get; set; } = "";
    public string Valor { get; set; } = "";
}
