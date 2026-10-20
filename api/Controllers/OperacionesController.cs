using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

/// <summary>
/// Espejo de compras y ventas. Tipo "C" = compra, "V" = venta.
/// ComprasController y VentasController heredan de aquí con su ruta propia.
/// </summary>
[ApiController]
public abstract class OperacionesControllerBase : ControllerBase
{
    protected readonly IOperacionService Operaciones;

    protected OperacionesControllerBase(IOperacionService operaciones) => Operaciones = operaciones;

    /// <summary>Código de operación: "C" o "V".</summary>
    protected abstract string Tipo { get; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OperacionListaDto>>> Listar([FromQuery] OperacionFiltroDto filtro)
        => Ok(await Operaciones.ListarAsync(Tipo, filtro));

    [HttpGet("{id}")]
    public async Task<ActionResult<OperacionDetalleDto>> Obtener(string id)
        => Ok(await Operaciones.ObtenerAsync(Tipo, id));

    [HttpPost]
    public async Task<ActionResult<OperacionDetalleDto>> Crear([FromBody] OperacionCrearDto dto)
    {
        var creada = await Operaciones.CrearAsync(Tipo, dto);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OperacionDetalleDto>> Actualizar(string id, [FromBody] OperacionCrearDto dto)
        => Ok(await Operaciones.ActualizarAsync(Tipo, id, dto));

    [HttpDelete("{id}")]
    public async Task<ActionResult<OperacionDetalleDto>> Anular(string id, [FromBody] AnulacionDto? dto)
        => Ok(await Operaciones.AnularAsync(Tipo, id, dto?.Motivo));

    /// <summary>Cuota de recepción de divisa (recibido del cliente).</summary>
    [HttpPost("{id}/recepciones")]
    public async Task<ActionResult<OperacionCuotaResultadoDto>> Recepcion(string id, [FromBody] RecepcionCrearDto dto)
        => Ok(await Operaciones.RegistrarRecepcionAsync(Tipo, id, dto));

    /// <summary>Cuota de pago en bolívares (pagado al cliente).</summary>
    [HttpPost("{id}/pagos")]
    public async Task<ActionResult<OperacionCuotaResultadoDto>> Pago(string id, [FromBody] PagoCrearDto dto)
        => Ok(await Operaciones.RegistrarPagoAsync(Tipo, id, dto));
}

[Route("api/compras")]
public class ComprasController : OperacionesControllerBase
{
    public ComprasController(IOperacionService operaciones) : base(operaciones) { }
    protected override string Tipo => "C";
}

[Route("api/ventas")]
public class VentasController : OperacionesControllerBase
{
    public VentasController(IOperacionService operaciones) : base(operaciones) { }
    protected override string Tipo => "V";
}
