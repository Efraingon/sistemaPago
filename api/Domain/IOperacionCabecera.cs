namespace Monrys.Api.Domain;

/// <summary>
/// Contrato común de las cabeceras de compra/venta (mismas columnas en ambas tablas).
/// Permite que el servicio de operaciones maneje ambos documentos con una sola lógica.
/// </summary>
public interface IOperacionCabecera
{
    string Id { get; set; }
    string Ejercicio { get; set; }
    string ClienteId { get; set; }
    DateTime Fecha { get; set; }
    string MonedaDivisaId { get; set; }
    string MonedaNacId { get; set; }
    decimal? MtoXDivisa { get; set; }
    decimal? MtoXNac { get; set; }
    decimal Tasa { get; set; }
    decimal Cantidad { get; set; }
    decimal MontoBs { get; set; }
    decimal PagadoBs { get; set; }
    decimal SaldoBs { get; set; }
    decimal RecibidoDivisa { get; set; }
    decimal PorRecibir { get; set; }
    string? Observaciones { get; set; }
    string? CentroCosto { get; set; }
    string Estado { get; set; }
    DateTime? AnuladoEn { get; set; }
    string? AnuladoMotivo { get; set; }
    DateTime CreadoEn { get; set; }
    DateTime ActualizadoEn { get; set; }
}
