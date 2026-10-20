using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IOperacionService
{
    Task<PagedResult<OperacionListaDto>> ListarAsync(string tipo, OperacionFiltroDto filtro);
    Task<OperacionDetalleDto> ObtenerAsync(string tipo, string id);
    Task<OperacionDetalleDto> CrearAsync(string tipo, OperacionCrearDto dto);
    Task<OperacionDetalleDto> ActualizarAsync(string tipo, string id, OperacionCrearDto dto);
    Task<OperacionDetalleDto> AnularAsync(string tipo, string id, string? motivo);
    Task<OperacionCuotaResultadoDto> RegistrarRecepcionAsync(string tipo, string id, RecepcionCrearDto dto);
    Task<OperacionCuotaResultadoDto> RegistrarPagoAsync(string tipo, string id, PagoCrearDto dto);
}

/// <summary>
/// Operaciones de compra y venta de divisa (VFP: compras_new / ventas_new /
/// comprasrecipagos / ventasrecipagos). tipo: "C" = compra, "V" = venta.
/// Reglas conservadas: MONTO_BS = ROUND(CANTIDAD × TASA, 2), saldos de cabecera
/// actualizados por cuota, caja con orígenes RE/EN (divisa) y CO/VE (Bs).
/// </summary>
public sealed class OperacionService : IOperacionService
{
    private readonly MonrysDbContext _db;
    private readonly ICorrelativoService _correlativos;
    private readonly ICajaService _caja;
    private readonly ICustodioService _custodio;
    private readonly IAuditoriaService _auditoria;

    public OperacionService(MonrysDbContext db, ICorrelativoService correlativos,
        ICajaService caja, ICustodioService custodio, IAuditoriaService auditoria)
    {
        _db = db;
        _correlativos = correlativos;
        _caja = caja;
        _custodio = custodio;
        _auditoria = auditoria;
    }

    // ---------------------------------------------------------------------
    // CONSULTA
    // ---------------------------------------------------------------------
    public async Task<PagedResult<OperacionListaDto>> ListarAsync(string tipo, OperacionFiltroDto f)
    {
        ValidarTipo(tipo);
        var pagina = Math.Max(1, f.Pagina);
        var tamano = Math.Clamp(f.Tamano, 1, 200);

        List<IOperacionCabecera> filas;
        int total;

        if (tipo == "C")
        {
            var q = _db.Compras.AsNoTracking();
            if (f.Desde.HasValue) { var d = f.Desde.Value.Date; q = q.Where(x => x.Fecha >= d); }
            if (f.Hasta.HasValue) { var h = f.Hasta.Value.Date.AddDays(1); q = q.Where(x => x.Fecha < h); }
            if (!string.IsNullOrWhiteSpace(f.ClienteId)) q = q.Where(x => x.ClienteId == f.ClienteId);
            if (!string.IsNullOrWhiteSpace(f.Estado)) q = q.Where(x => x.Estado == f.Estado);
            if (!string.IsNullOrWhiteSpace(f.Buscar)) { var t = f.Buscar.Trim(); q = q.Where(x => x.Id.Contains(t)); }

            total = await q.CountAsync();
            var paginaCompras = await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
                .Skip((pagina - 1) * tamano).Take(tamano)
                .ToListAsync();
            filas = paginaCompras.Cast<IOperacionCabecera>().ToList();
        }
        else
        {
            var q = _db.Ventas.AsNoTracking();
            if (f.Desde.HasValue) { var d = f.Desde.Value.Date; q = q.Where(x => x.Fecha >= d); }
            if (f.Hasta.HasValue) { var h = f.Hasta.Value.Date.AddDays(1); q = q.Where(x => x.Fecha < h); }
            if (!string.IsNullOrWhiteSpace(f.ClienteId)) q = q.Where(x => x.ClienteId == f.ClienteId);
            if (!string.IsNullOrWhiteSpace(f.Estado)) q = q.Where(x => x.Estado == f.Estado);
            if (!string.IsNullOrWhiteSpace(f.Buscar)) { var t = f.Buscar.Trim(); q = q.Where(x => x.Id.Contains(t)); }

            total = await q.CountAsync();
            var paginaVentas = await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
                .Skip((pagina - 1) * tamano).Take(tamano)
                .ToListAsync();
            filas = paginaVentas.Cast<IOperacionCabecera>().ToList();
        }

        var (clientes, monedas) = await CargarDiccionariosAsync(filas);
        return new PagedResult<OperacionListaDto>
        {
            Total = total,
            Items = filas.Select(x => MapearLista(x, clientes, monedas)).ToList()
        };
    }

    public async Task<OperacionDetalleDto> ObtenerAsync(string tipo, string id)
    {
        ValidarTipo(tipo);
        var cabecera = await CargarAsync(tipo, id)
            ?? throw new RecursoNoEncontradoException(NoExiste(tipo, id));

        var recepciones = await ListarRecepcionesAsync(tipo, id);
        var pagos = await ListarPagosAsync(tipo, id);
        var (clientes, monedas) = await CargarDiccionariosAsync([cabecera]);

        return MapearDetalle(cabecera, recepciones, pagos, clientes, monedas);
    }

    // ---------------------------------------------------------------------
    // ALTA
    // ---------------------------------------------------------------------
    public async Task<OperacionDetalleDto> CrearAsync(string tipo, OperacionCrearDto dto)
    {
        ValidarTipo(tipo);
        var (config, moneda) = await ValidarAltaAsync(tipo, dto);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fecha = dto.Fecha?.Date ?? DateTime.Today;
        var ejercicio = fecha.Year.ToString();
        var secuencia = await _correlativos.SiguienteNumeroAsync(
            tipo == "C" ? "COMPRA" : "VENTA", ejercicio);
        var id = ejercicio + secuencia.ToString("D12");

        IOperacionCabecera cabecera;
        if (tipo == "C")
        {
            var nueva = new Compra();
            _db.Compras.Add(nueva);
            cabecera = nueva;
        }
        else
        {
            var nueva = new Venta();
            _db.Ventas.Add(nueva);
            cabecera = nueva;
        }

        cabecera.Id = id;
        cabecera.Ejercicio = ejercicio;
        cabecera.ClienteId = dto.ClienteId.Trim();
        cabecera.Fecha = fecha;
        cabecera.MonedaDivisaId = moneda.Id;
        cabecera.MonedaNacId = config.MonedaNacionalId;
        cabecera.MtoXDivisa = dto.Tasa;
        cabecera.MtoXNac = dto.Tasa;
        cabecera.Tasa = dto.Tasa!.Value;
        cabecera.Cantidad = dto.Cantidad;
        cabecera.MontoBs = Redondear(dto.Cantidad * dto.Tasa.Value);
        cabecera.PagadoBs = 0m;
        cabecera.SaldoBs = Redondear(dto.Cantidad * dto.Tasa.Value);
        cabecera.RecibidoDivisa = 0m;
        cabecera.PorRecibir = dto.Cantidad;
        cabecera.Observaciones = dto.Observaciones;
        cabecera.CentroCosto = dto.CentroCosto;
        cabecera.Estado = "ACTIVO";
        cabecera.CreadoEn = DateTime.Now;
        cabecera.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            $"{(tipo == "C" ? "REGISTRA LA COMPRA" : "REGISTRA LA VENTA")} NRO. {id}",
            tipo == "C" ? "compra" : "venta", id,
            new { id, cliente = cabecera.ClienteId, fecha = fecha.ToString("yyyy-MM-dd"),
                  cantidad = cabecera.Cantidad, tasa = cabecera.Tasa, montoBs = cabecera.MontoBs });

        await tx.CommitAsync();
        return await ObtenerAsync(tipo, id);
    }

    // ---------------------------------------------------------------------
    // MODIFICACIÓN (solo referencias/cabecera; montos requieren no tener cuotas)
    // ---------------------------------------------------------------------
    public async Task<OperacionDetalleDto> ActualizarAsync(string tipo, string id, OperacionCrearDto dto)
    {
        ValidarTipo(tipo);
        var cabecera = await CargarAsync(tipo, id)
            ?? throw new RecursoNoEncontradoException(NoExiste(tipo, id));

        if (cabecera.Estado != "ACTIVO")
            throw new ValidacionException("No se puede modificar una operación anulada.");

        var tieneCuotas = await TieneCuotasAsync(tipo, id);
        var (_, moneda) = await ValidarAltaAsync(tipo, dto, soloReferencias: true);

        var cambiaMontos = dto.Cantidad > 0 &&
                           (dto.Cantidad != cabecera.Cantidad ||
                            (dto.Tasa.HasValue && dto.Tasa.Value != cabecera.Tasa));
        if (cambiaMontos && tieneCuotas)
            throw new ValidacionException("La operación ya tiene cuotas: no se puede cambiar cantidad ni tasa.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new
        {
            cabecera.Id, cabecera.Cantidad, cabecera.Tasa, cabecera.MontoBs,
            cabecera.SaldoBs, cabecera.PorRecibir, cabecera.Fecha, cabecera.Estado
        };

        cabecera.Fecha = dto.Fecha?.Date ?? cabecera.Fecha;
        cabecera.Ejercicio = cabecera.Fecha.Year.ToString();
        cabecera.Observaciones = dto.Observaciones;
        cabecera.CentroCosto = dto.CentroCosto;
        cabecera.MonedaDivisaId = moneda?.Id ?? cabecera.MonedaDivisaId;

        if (cambiaMontos)
        {
            var tasa = dto.Tasa ?? cabecera.Tasa;
            var monto = Redondear(dto.Cantidad * tasa);
            cabecera.Cantidad = dto.Cantidad;
            cabecera.Tasa = tasa;
            cabecera.MtoXDivisa = tasa;
            cabecera.MtoXNac = tasa;
            cabecera.MontoBs = monto;
            cabecera.SaldoBs = monto - cabecera.PagadoBs;
            cabecera.PorRecibir = dto.Cantidad - cabecera.RecibidoDivisa;
        }

        cabecera.ActualizadoEn = DateTime.Now;
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync($"MODIFICA {NombreTipo(tipo)} NRO. {cabecera.Id}",
            tipo == "C" ? "compra" : "venta", cabecera.Id, previo);

        await tx.CommitAsync();
        return await ObtenerAsync(tipo, id);
    }

    // ---------------------------------------------------------------------
    // ANULACIÓN (solo ACTIVO y sin cuotas)
    // ---------------------------------------------------------------------
    public async Task<OperacionDetalleDto> AnularAsync(string tipo, string id, string? motivo)
    {
        ValidarTipo(tipo);
        var cabecera = await CargarAsync(tipo, id)
            ?? throw new RecursoNoEncontradoException(NoExiste(tipo, id));

        if (cabecera.Estado == "ANULADO")
            throw new ConflictoException($"La {NombreTipo(tipo)} {id} ya está anulada.");
        if (await TieneCuotasAsync(tipo, id))
            throw new ValidacionException("La operación tiene recepciones/pagos registrados: anule primero las cuotas.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var previo = new { cabecera.Estado, cabecera.MontoBs, cabecera.SaldoBs, cabecera.PorRecibir };
        cabecera.Estado = "ANULADO";
        cabecera.AnuladoEn = DateTime.Now;
        cabecera.AnuladoMotivo = string.IsNullOrWhiteSpace(motivo) ? "Sin motivo" : motivo.Trim();
        cabecera.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync($"ANULA {NombreTipo(tipo).ToUpper()} NRO. {id}",
            tipo == "C" ? "compra" : "venta", id, previo, cabecera.AnuladoMotivo);

        await tx.CommitAsync();
        return await ObtenerAsync(tipo, id);
    }

    // ---------------------------------------------------------------------
    // CUOTA: RECEPCIÓN DE DIVISA (compra) / ENTREGA DE DIVISA (venta)
    // ---------------------------------------------------------------------
    public async Task<OperacionCuotaResultadoDto> RegistrarRecepcionAsync(string tipo, string id, RecepcionCrearDto dto)
    {
        ValidarTipo(tipo);
        if (dto.Cantidad <= 0)
            throw new ValidacionException("La cantidad a recibir debe ser mayor que cero.");

        var cabecera = await CargarAsync(tipo, id)
            ?? throw new RecursoNoEncontradoException(NoExiste(tipo, id));

        if (cabecera.Estado != "ACTIVO")
            throw new ValidacionException("No se pueden registrar cuotas sobre una operación anulada.");
        if (dto.Custodio)
        {
            // El custodio excluye caja/banco: el destino de la divisa es la compensación.
            dto.DeCaja = false;
            dto.DeBanco = false;
        }
        else if (!dto.DeCaja && !dto.DeBanco)
            dto.DeCaja = true;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fecha = dto.Fecha?.Date ?? DateTime.Today;
        var nroPer = await SiguienteNroPerAsync(tipo, id);
        var porRecibir = cabecera.PorRecibir - dto.Cantidad;
        var montoBs = Redondear(dto.Cantidad * cabecera.Tasa);

        string? reciboBancoCompensacion = dto.Recibo;
        string? reciboCaja = null;
        string? idCaja = null;

        // --- Destino CUSTODIO: abono en venta (+) / retiro en compra (−) -------------
        if (dto.Custodio)
        {
            var monedaDivisa = await _db.TipoMonedas.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == cabecera.MonedaDivisaId)
                ?? throw new ValidacionException("La moneda divisa de la operación no existe.");

            var abonos = tipo == "V" ? dto.Cantidad : 0m;
            var retiros = tipo == "C" ? dto.Cantidad : 0m;

            var (_, reciboCompensacion) = await _custodio.InsertarCompensacionAsync(
                cabecera.ClienteId, monedaDivisa.Id, abonos, retiros, fecha,
                dto.Descripcion ?? $"{(tipo == "C" ? "RECEPCIÓN" : "ENTREGA")} DIVISA {NombreTipo(tipo)} {id}",
                dto.CentroCosto ?? cabecera.CentroCosto);

            await _custodio.RecalcularCustodioAsync(cabecera.ClienteId, monedaDivisa.Id);
            reciboBancoCompensacion = reciboCompensacion;
        }

        // --- Destino CAJA: RE en compras / EN en ventas --------------------------------
        if (dto.DeCaja)
        {
            var movimiento = await _caja.InsertarMovimientoAsync(new MovimientoCajaParametros
            {
                Origen = tipo == "C" ? OrigenesCaja.RecepcionDivisa : OrigenesCaja.EntregaDivisa,
                Fecha = fecha,
                Monto = dto.Cantidad,
                Descripcion = tipo == "C"
                    ? $"RECEPCIÓN DE DIVISAS COMPRA NRO. {id}"
                    : $"ENTREGA DE DIVISAS VENTA NRO.: {id}",
                MonedaId = cabecera.MonedaDivisaId,
                ClienteId = cabecera.ClienteId,
                CompraId = tipo == "C" ? id : null,
                VentaId = tipo == "V" ? id : null,
                Tasa = cabecera.Tasa,
                Usd = dto.Cantidad,
                MontoBs = montoBs,
                CentroCosto = dto.CentroCosto ?? cabecera.CentroCosto
            });
            reciboCaja = movimiento.Recibo;
            idCaja = movimiento.Id;
        }

        var tipoPago = dto.Custodio ? "COMPENSACION"
            : dto.DeCaja && dto.DeBanco ? "BANCO / CAJA"
            : dto.DeBanco ? "BANCO" : "CAJA";

        var recepcion = new OperacionRecepcion
        {
            TipoOp = tipo,
            OperacionId = id,
            NroPer = nroPer,
            Ejercicio = cabecera.Ejercicio,
            Fecha = fecha,
            Cantidad = dto.Cantidad,
            PorRecibir = porRecibir < 0 ? 0 : porRecibir,
            Recibo = reciboBancoCompensacion,
            Recibo2 = reciboCaja,
            BancoCreditoId = dto.BancoCreditoId,
            BancoDebitoId = dto.BancoDebitoId,
            CtaCredito = dto.CtaCredito,
            CtaDebito = dto.CtaDebito,
            TipoPago = tipoPago,
            Compensado = dto.Custodio,
            DeCaja = dto.DeCaja,
            DeBanco = dto.DeBanco
        };
        _db.OperacionesRecepcion.Add(recepcion);

        cabecera.RecibidoDivisa += dto.Cantidad;
        cabecera.PorRecibir = cabecera.Cantidad - cabecera.RecibidoDivisa;
        cabecera.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            $"REGISTRA {(tipo == "C" ? "RECEPCIÓN" : "ENTREGA")} NRO. {nroPer:0} DOCUMENTO: {reciboCaja ?? reciboBancoCompensacion ?? "-"} {NombreTipo(tipo)} Nro. {id}",
            tipo == "C" ? "compra" : "venta", id,
            new { operacionId = id, nroPer, cantidad = dto.Cantidad, tipoPago, deCaja = dto.DeCaja, custodio = dto.Custodio });

        await tx.CommitAsync();

        return new OperacionCuotaResultadoDto
        {
            OperacionId = id,
            RecibidoDivisa = cabecera.RecibidoDivisa,
            PorRecibir = cabecera.PorRecibir,
            PagadoBs = cabecera.PagadoBs,
            SaldoBs = cabecera.SaldoBs,
            NroPer = (int)nroPer,
            Recibo = reciboBancoCompensacion,
            ReciboCaja = reciboCaja
        };
    }

    // ---------------------------------------------------------------------
    // CUOTA: PAGO / COBRO EN Bs
    // ---------------------------------------------------------------------
    public async Task<OperacionCuotaResultadoDto> RegistrarPagoAsync(string tipo, string id, PagoCrearDto dto)
    {
        ValidarTipo(tipo);
        if (dto.Monto <= 0)
            throw new ValidacionException("El monto debe ser mayor que cero.");

        var cabecera = await CargarAsync(tipo, id)
            ?? throw new RecursoNoEncontradoException(NoExiste(tipo, id));

        if (cabecera.Estado != "ACTIVO")
            throw new ValidacionException("No se pueden registrar cuotas sobre una operación anulada.");
        if (dto.Monto > cabecera.SaldoBs)
            throw new ValidacionException($"El monto a {(tipo == "C" ? "pagar" : "cobrar")} ({dto.Monto:0.00}) es mayor al saldo ({cabecera.SaldoBs:0.00}).");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fecha = dto.Fecha?.Date ?? DateTime.Today;
        var nroPer = await SiguienteNroPerPagoAsync(tipo, id);
        var saldo = cabecera.SaldoBs - dto.Monto;
        string? reciboCaja = null;

        if (dto.DeCaja)
        {
            var movimiento = await _caja.InsertarMovimientoAsync(new MovimientoCajaParametros
            {
                Origen = tipo == "C" ? OrigenesCaja.PagoCompra : OrigenesCaja.CobroVenta,
                Fecha = fecha,
                Monto = dto.Monto,
                Descripcion = tipo == "C"
                    ? $"PAGO EN COMPRAS: {id}"
                    : $"COBRANZA DE VENTA NRO. {id}",
                MonedaId = cabecera.MonedaNacId,
                ClienteId = cabecera.ClienteId,
                CompraId = tipo == "C" ? id : null,
                VentaId = tipo == "V" ? id : null,
                MontoBs = dto.Monto,
                CentroCosto = cabecera.CentroCosto
            });
            reciboCaja = movimiento.Recibo;
        }

        var pago = new OperacionPago
        {
            TipoOp = tipo,
            OperacionId = id,
            NroPer = nroPer,
            Ejercicio = cabecera.Ejercicio,
            Fecha = fecha,
            Monto = dto.Monto,
            BancoDebitoId = dto.BancoDebitoId,
            CtaDebito = dto.CtaDebito,
            BancoCreditoId = dto.BancoCreditoId,
            CtaCredito = dto.CtaCredito,
            Recibo = dto.Recibo ?? reciboCaja,
            TipoPago = dto.TipoPago ?? (dto.DeCaja ? "CAJA" : "BANCO"),
            Saldo = saldo < 0 ? 0 : saldo
        };
        _db.OperacionesPago.Add(pago);

        cabecera.PagadoBs += dto.Monto;
        cabecera.SaldoBs = cabecera.MontoBs - cabecera.PagadoBs;
        cabecera.ActualizadoEn = DateTime.Now;

        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            $"REGISTRA {(tipo == "C" ? "PAGO" : "COBRANZA")} NRO. {nroPer:0} DOCUMENTO: {pago.Recibo ?? "-"} {NombreTipo(tipo)} Nro. {id}",
            tipo == "C" ? "compra" : "venta", id,
            new { operacionId = id, nroPer, monto = dto.Monto, tipoPago = pago.TipoPago, deCaja = dto.DeCaja });

        await tx.CommitAsync();

        return new OperacionCuotaResultadoDto
        {
            OperacionId = id,
            RecibidoDivisa = cabecera.RecibidoDivisa,
            PorRecibir = cabecera.PorRecibir,
            PagadoBs = cabecera.PagadoBs,
            SaldoBs = cabecera.SaldoBs,
            NroPer = (int)nroPer,
            Recibo = pago.Recibo,
            ReciboCaja = reciboCaja
        };
    }

    // ---------------------------------------------------------------------
    // AUXILIARES
    // ---------------------------------------------------------------------
    private static void ValidarTipo(string tipo)
    {
        if (tipo is not ("C" or "V"))
            throw new ValidacionException("Tipo de operación inválido (se espera 'C' de compra o 'V' de venta).");
    }

    private static string NombreTipo(string tipo) => tipo == "C" ? "COMPRA" : "VENTA";
    private static string NoExiste(string tipo, string id) => $"No existe la {(tipo == "C" ? "compra" : "venta")} {id}.";
    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private async Task<IOperacionCabecera?> CargarAsync(string tipo, string id)
    {
        if (tipo == "C")
            return await _db.Compras.FirstOrDefaultAsync(x => x.Id == id);
        return await _db.Ventas.FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<bool> TieneCuotasAsync(string tipo, string id)
    {
        if (tipo == "C")
        {
            if (await _db.OperacionesRecepcion.AnyAsync(r => r.TipoOp == "C" && r.OperacionId == id)) return true;
            return await _db.OperacionesPago.AnyAsync(r => r.TipoOp == "C" && r.OperacionId == id);
        }
        if (await _db.OperacionesRecepcion.AnyAsync(r => r.TipoOp == "V" && r.OperacionId == id)) return true;
        return await _db.OperacionesPago.AnyAsync(r => r.TipoOp == "V" && r.OperacionId == id);
    }

    private async Task<int> SiguienteNroPerAsync(string tipo, string id)
    {
        var actual = await _db.OperacionesRecepcion.AsNoTracking()
            .Where(r => r.TipoOp == tipo && r.OperacionId == id)
            .MaxAsync(r => (int?)r.NroPer) ?? 0;
        return actual + 1;
    }

    private async Task<int> SiguienteNroPerPagoAsync(string tipo, string id)
    {
        var actual = await _db.OperacionesPago.AsNoTracking()
            .Where(r => r.TipoOp == tipo && r.OperacionId == id)
            .MaxAsync(r => (int?)r.NroPer) ?? 0;
        return actual + 1;
    }

    /// <summary>Valida cliente/moneda/tasa y devuelve la configuración y la divisa resueltas.</summary>
    private async Task<(Configuracion config, TipoMoneda moneda)> ValidarAltaAsync(
        string tipo, OperacionCrearDto dto, bool soloReferencias = false)
    {
        if (string.IsNullOrWhiteSpace(dto.ClienteId))
            throw new ValidacionException("Debe indicar el cliente.");

        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Trim())
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {dto.ClienteId}.");
        if (!cliente.Activo)
            throw new ValidacionException("El cliente está inactivo.");

        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");

        var monedaId = string.IsNullOrWhiteSpace(dto.MonedaId) ? config.MonedaPrincipalId : dto.MonedaId.Trim();
        var moneda = await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == monedaId)
            ?? throw new ValidacionException($"La moneda '{monedaId}' no existe.");
        if (moneda.Nacional)
            throw new ValidacionException("La operación debe realizarse sobre la divisa, no sobre la moneda nacional.");
        if (!moneda.Activa)
            throw new ValidacionException($"La moneda '{monedaId}' está inactiva.");

        if (!soloReferencias)
        {
            if (dto.Cantidad <= 0)
                throw new ValidacionException("La cantidad de divisa debe ser mayor que cero.");
            if (dto.Fecha.HasValue && dto.Fecha.Value.Date > DateTime.Today.AddDays(1))
                throw new ValidacionException("La fecha de la operación no puede ser futura.");
            dto.Tasa ??= tipo == "C" ? config.TasaCompra : config.TasaVenta;
            if (dto.Tasa.Value <= 0)
                throw new ValidacionException("La tasa debe ser mayor que cero.");
        }

        return (config, moneda);
    }

    private async Task<(Dictionary<string, Cliente> clientes, Dictionary<string, TipoMoneda> monedas)>
        CargarDiccionariosAsync(IReadOnlyList<IOperacionCabecera> filas)
    {
        var clienteIds = filas.Select(x => x.ClienteId).Where(x => x != null).Select(x => x!).Distinct().ToList();
        var monedaIds = filas.Select(x => x.MonedaDivisaId).Where(x => x != null).Select(x => x!).Distinct().ToList();

        var clientes = await _db.Clientes.AsNoTracking()
            .Where(c => clienteIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var monedas = await _db.TipoMonedas.AsNoTracking()
            .Where(m => monedaIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        return (clientes, monedas);
    }

    private async Task<List<RecepcionDto>> ListarRecepcionesAsync(string tipo, string id)
    {
        var filas = await _db.OperacionesRecepcion.AsNoTracking()
            .Where(r => r.TipoOp == tipo && r.OperacionId == id)
            .OrderBy(r => r.NroPer)
            .ToListAsync();

        return filas.Select(r => new RecepcionDto
        {
            Id = r.Id,
            TipoOp = r.TipoOp,
            OperacionId = r.OperacionId,
            NroPer = (int)r.NroPer,
            Fecha = r.Fecha,
            Cantidad = r.Cantidad,
            PorRecibir = r.PorRecibir,
            Recibo = r.Recibo,
            Recibo2 = r.Recibo2,
            TipoPago = r.TipoPago,
            Compensado = r.Compensado,
            DeCaja = r.DeCaja,
            DeBanco = r.DeBanco,
            BancoDebitoId = r.BancoDebitoId,
            BancoCreditoId = r.BancoCreditoId,
            CtaDebito = r.CtaDebito,
            CtaCredito = r.CtaCredito,
            CreadoEn = r.CreadoEn
        }).ToList();
    }

    private async Task<List<PagoDto>> ListarPagosAsync(string tipo, string id)
    {
        var filas = await _db.OperacionesPago.AsNoTracking()
            .Where(r => r.TipoOp == tipo && r.OperacionId == id)
            .OrderBy(r => r.NroPer)
            .ToListAsync();

        return filas.Select(r => new PagoDto
        {
            Id = r.Id,
            TipoOp = r.TipoOp,
            OperacionId = r.OperacionId,
            NroPer = (int)r.NroPer,
            Fecha = r.Fecha,
            Monto = r.Monto,
            Saldo = r.Saldo,
            Recibo = r.Recibo,
            TipoPago = r.TipoPago,
            BancoDebitoId = r.BancoDebitoId,
            BancoCreditoId = r.BancoCreditoId,
            CtaDebito = r.CtaDebito,
            CtaCredito = r.CtaCredito,
            CreadoEn = r.CreadoEn
        }).ToList();
    }

    private static OperacionListaDto MapearLista(IOperacionCabecera c,
        Dictionary<string, Cliente> clientes, Dictionary<string, TipoMoneda> monedas)
    {
        clientes.TryGetValue(c.ClienteId, out var cliente);
        monedas.TryGetValue(c.MonedaDivisaId, out var moneda);

        return new OperacionListaDto
        {
            Id = c.Id,
            Ejercicio = c.Ejercicio,
            ClienteId = c.ClienteId,
            Cliente = cliente?.NombreCompleto ?? c.ClienteId,
            Fecha = c.Fecha,
            MonedaDivisaId = c.MonedaDivisaId,
            Moneda = moneda?.Descripcion ?? c.MonedaDivisaId,
            Tasa = c.Tasa,
            Cantidad = c.Cantidad,
            MontoBs = c.MontoBs,
            PagadoBs = c.PagadoBs,
            SaldoBs = c.SaldoBs,
            RecibidoDivisa = c.RecibidoDivisa,
            PorRecibir = c.PorRecibir,
            Estado = c.Estado,
            AnuladoMotivo = c.AnuladoMotivo
        };
    }

    private OperacionDetalleDto MapearDetalle(IOperacionCabecera c, List<RecepcionDto> recepciones,
        List<PagoDto> pagos, Dictionary<string, Cliente> clientes, Dictionary<string, TipoMoneda> monedas)
    {
        var dto = MapearLista(c, clientes, monedas);
        return new OperacionDetalleDto
        {
            Id = dto.Id,
            Ejercicio = dto.Ejercicio,
            ClienteId = dto.ClienteId,
            Cliente = dto.Cliente,
            Fecha = dto.Fecha,
            MonedaDivisaId = dto.MonedaDivisaId,
            Moneda = dto.Moneda,
            Tasa = dto.Tasa,
            Cantidad = dto.Cantidad,
            MontoBs = dto.MontoBs,
            PagadoBs = dto.PagadoBs,
            SaldoBs = dto.SaldoBs,
            RecibidoDivisa = dto.RecibidoDivisa,
            PorRecibir = dto.PorRecibir,
            Estado = dto.Estado,
            AnuladoMotivo = dto.AnuladoMotivo,
            MonedaNacId = c.MonedaNacId,
            Observaciones = c.Observaciones,
            CentroCosto = c.CentroCosto,
            CreadoEn = c.CreadoEn,
            ActualizadoEn = c.ActualizadoEn,
            AnuladoEn = c.AnuladoEn,
            Recepciones = recepciones,
            Pagos = pagos
        };
    }
}
