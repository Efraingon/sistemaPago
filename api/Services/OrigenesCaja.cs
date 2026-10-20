namespace Monrys.Api.Services;

/// <summary>
/// Catálogo estático de CAJA.ORIGEN (equivale a la tabla RPTCAJA del VFP que
/// traducía el código de 2 letras a texto y definía el signo del saldo).
/// </summary>
public static class OrigenesCaja
{
    public const string Ingreso = "IN";
    public const string Egreso = "EG";
    public const string PagoCompra = "CO";
    public const string CobroVenta = "VE";
    public const string RecepcionDivisa = "RE";
    public const string EntregaDivisa = "EN";
    public const string RecepcionCustodio = "C1";
    public const string EntregaCustodio = "C2";
    public const string DepositoDivisa = "DE";
    public const string SalidaDivisa = "DS";
    public const string IngresoBanco = "IB";
    public const string EgresoBanco = "EB";
    public const string TransferenciaSalida = "T5";
    public const string TransferenciaEntrada = "T6";

    /// <summary>Orígenes que suman al saldo (entrada de valor a caja).</summary>
    public static readonly string[] Entradas = [Ingreso, DepositoDivisa, RecepcionDivisa, RecepcionCustodio, CobroVenta, IngresoBanco, TransferenciaEntrada];

    /// <summary>Orígenes que restan del saldo (salida de valor de caja).</summary>
    public static readonly string[] Salidas = [Egreso, SalidaDivisa, EntregaDivisa, EntregaCustodio, PagoCompra, EgresoBanco, TransferenciaSalida];

    public static bool EsEntrada(string origen) => Entradas.Contains(origen);
    public static bool EsSalida(string origen) => Salidas.Contains(origen);
    public static int Signo(string origen) => EsEntrada(origen) ? 1 : EsSalida(origen) ? -1 : 0;

    /// <summary>Orígenes considerados "divisa" (el resto opera en moneda nacional).</summary>
    public static readonly string[] Divisa = [DepositoDivisa, SalidaDivisa, RecepcionDivisa, EntregaDivisa, RecepcionCustodio, EntregaCustodio, TransferenciaSalida, TransferenciaEntrada];

    public static bool EsDivisa(string origen) => Divisa.Contains(origen);

    public static string Descripcion(string origen) => origen switch
    {
        Ingreso => "Ingreso de caja",
        Egreso => "Egreso de caja",
        DepositoDivisa => "Depósito de divisa",
        SalidaDivisa => "Salida de divisa",
        PagoCompra => "Pago en compras",
        CobroVenta => "Cobranza de venta",
        RecepcionDivisa => "Recepción de divisa (compra)",
        EntregaDivisa => "Entrega de divisa (venta)",
        RecepcionCustodio => "Recepción de custodio a caja",
        EntregaCustodio => "Entrega de custodio desde caja",
        IngresoBanco => "Ingreso desde banco",
        EgresoBanco => "Egreso hacia banco",
        TransferenciaSalida => "Transferencia internacional (salida)",
        TransferenciaEntrada => "Transferencia internacional (entrada)",
        _ => origen
    };
}
