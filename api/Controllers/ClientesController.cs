using Microsoft.AspNetCore.Mvc;
using Monrys.Api.Dtos;
using Monrys.Api.Services;

namespace Monrys.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _servicio;

    public ClientesController(IClienteService servicio) => _servicio = servicio;

    /// <summary>Lista paginada de clientes. Query: buscar, activo, pagina, tamano.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ClienteListaDto>>> Listar(
        [FromQuery] string? buscar, [FromQuery] bool? activo,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
        => Ok(await _servicio.ListarAsync(buscar, activo, pagina, tamano));

    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDetalleDto>> Obtener(string id)
        => Ok(await _servicio.ObtenerAsync(id));

    [HttpPost]
    public async Task<ActionResult<ClienteDetalleDto>> Crear([FromBody] ClienteCrearDto dto)
    {
        var creado = await _servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClienteDetalleDto>> Actualizar(string id, [FromBody] ClienteCrearDto dto)
        => Ok(await _servicio.ActualizarAsync(id, dto));

    /// <summary>Borrado lógico: desactiva el cliente.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ClienteDetalleDto>> Eliminar(string id)
        => Ok(await _servicio.EliminarAsync(id));

    // ------------------------------------------------------- cuentas bancarias
    [HttpGet("{id}/cuentas")]
    public async Task<ActionResult<List<CuentaClienteDto>>> Cuentas(string id)
        => Ok(await _servicio.ListarCuentasAsync(id));

    [HttpPost("{id}/cuentas")]
    public async Task<ActionResult<CuentaClienteDto>> CrearCuenta(string id, [FromBody] CuentaClienteCrearDto dto)
        => Ok(await _servicio.CrearCuentaAsync(id, dto));

    [HttpPut("{id}/cuentas/{cuentaId:long}")]
    public async Task<ActionResult<CuentaClienteDto>> ActualizarCuenta(string id, long cuentaId, [FromBody] CuentaClienteCrearDto dto)
        => Ok(await _servicio.ActualizarCuentaAsync(id, cuentaId, dto));

    [HttpDelete("{id}/cuentas/{cuentaId:long}")]
    public async Task<IActionResult> EliminarCuenta(string id, long cuentaId)
    {
        await _servicio.EliminarCuentaAsync(id, cuentaId);
        return NoContent();
    }

    // -------------------------------------------------------- centros de costo
    [HttpPost("{id}/centros-costo")]
    public async Task<ActionResult<CentroCostoDto>> CrearCentro(string id, [FromBody] CentroCostoCrearDto dto)
        => Ok(await _servicio.CrearCentroAsync(id, dto));

    [HttpDelete("{id}/centros-costo/{centroId:long}")]
    public async Task<IActionResult> EliminarCentro(string id, long centroId)
    {
        await _servicio.EliminarCentroAsync(id, centroId);
        return NoContent();
    }
}
