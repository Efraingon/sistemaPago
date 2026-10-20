using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IReportesService
{
    Task<ConsolidadoDto> ConsolidadoAsync(DateTime? fecha);
    Task<EstadoGeneralDto> EstadoGeneralAsync(DateTime? fechaCorte, string? clienteId);
    Task<CuadreCajaDto> CuadreCajaAsync(DateTime? desde, DateTime? hasta);
    Task<CajaPorOrigenDto> CajaPorOrigenAsync(DateTime? desde, DateTime? hasta, string? tipo);
    Task<EstadoCuentaDto> EstadoCuentaAsync(string clienteId, DateTime? desde, DateTime? hasta);
    Task<ComprobanteDto> ComprobanteAsync(string tipo, string id);
}

/// <summary>
/// Reportes gerenciales (familias del doc 04): consolidado, estado general,
/// cuadre de caja, caja por origen, estado de cuenta y comprobantes imprimibles.
/// Los importes se agregan en memoria (los totales son rangos acotados), lo que
/// además hace el mismo código válido sobre PostgreSQL y SQLite.
/// </summary>
public sealed class ReportesService : IReportesService
{
    private readonly MonrysDbContext _db;

    public ReportesService(MonrysDbContext db) => _db = db;

    public async Task<ConsolidadoDto> ConsolidadoAsync(DateTime? fecha)
    {
        var dia = (fecha ?? DateTime.Today).Date;
        var (config, monedas) = await CargarBaseAsync();

        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha <= dia && x.Estado == "ACTIVO")
            .Select(x => new { x.Origen, x.MonedaId, x.Monto })
            .ToListAsync();

        var compensaciones = await _db.Compensaciones.AsNoTracking()
            .Where(c => c.Fecha <= dia)
            .Select(c => new { c.MonedaId, c.Abonos, c.Retiros })
            .ToListAsync();

        var saldosBanco = await _db.Bancos.AsNoTracking()
            .Where(b => b.Activo)
            .Select(b => b.Saldo)
            .ToListAsync();

        var resultado = new ConsolidadoDto
        {
            Fecha = dia,
            TasaActiva = config.TasaActiva
        };

        foreach (var moneda in monedas)
        {
            var caja = movimientos
                .Where(x => x.MonedaId == moneda.Id)
                .Sum(x => OrigenesCaja.Signo(x.Origen) * x.Monto);
            var custodio = compensaciones
                .Where(x => x.MonedaId == moneda.Id)
                .Sum(x => x.Abonos - x.Retiros);

            if (caja == 0 && custodio == 0) continue;

            var cajaBs = A_Bs(caja, moneda, config.TasaActiva);
            var custodioBs = A_Bs(custodio, moneda, config.TasaActiva);

            resultado.Caja.Add(new SaldoGrupoDto
            {
                MonedaId = moneda.Id, Descripcion = moneda.Descripcion, Nacional = moneda.Nacional,
                Saldo = caja, SaldoBs = cajaBs
            });
            resultado.Custodio.Add(new SaldoGrupoDto
            {
                MonedaId = moneda.Id, Descripcion = moneda.Descripcion, Nacional = moneda.Nacional,
                Saldo = custodio, SaldoBs = custodioBs
            });

            resultado.TotalCajaBs += cajaBs;
            resultado.TotalCustodioBs += custodioBs;
        }

        resultado.SaldoBancoBs = saldosBanco.Sum();
        resultado.TotalBs = resultado.TotalCajaBs + resultado.TotalCustodioBs + resultado.SaldoBancoBs;
        resultado.TotalUsd = config.TasaActiva > 0 ? Math.Round(resultado.TotalBs / config.TasaActiva, 2) : 0m;
        return resultado;
    }

    public async Task<EstadoGeneralDto> EstadoGeneralAsync(DateTime? fechaCorte, string? clienteId)
    {
        var corte = (fechaCorte ?? DateTime.Today).Date;
        var (config, monedas) = await CargarBaseAsync();

        var qClientes = _db.Clientes.AsNoTracking().Where(c => c.Activo);
        if (!string.IsNullOrWhiteSpace(clienteId)) qClientes = qClientes.Where(c => c.Id == clienteId);
        var clientes = await qClientes.OrderBy(c => c.RazonSocial).ThenBy(c => c.Apellidos).ToListAsync();
        var ids = clientes.Select(c => c.Id).ToList();
        if (ids.Count == 0)
            return new EstadoGeneralDto { FechaCorte = corte, TasaActiva = config.TasaActiva };

        var compras = await _db.Compras.AsNoTracking()
            .Where(c => c.Estado == "ACTIVO" && c.Fecha <= corte && ids.Contains(c.ClienteId))
            .Select(c => new { c.ClienteId, c.MontoBs, c.PagadoBs, c.SaldoBs })
            .ToListAsync();

        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "ACTIVO" && v.Fecha <= corte && ids.Contains(v.ClienteId))
            .Select(v => new { v.ClienteId, v.MontoBs, v.PagadoBs, v.SaldoBs })
            .ToListAsync();

        var compensaciones = await _db.Compensaciones.AsNoTracking()
            .Where(c => c.Fecha <= corte && ids.Contains(c.ClienteId))
            .Select(c => new { c.ClienteId, c.MonedaId, c.Abonos, c.Retiros })
            .ToListAsync();

        var resultado = new EstadoGeneralDto { FechaCorte = corte, TasaActiva = config.TasaActiva };

        foreach (var cliente in clientes)
        {
            var lc = compras.Where(c => c.ClienteId == cliente.Id).ToList();
            var lv = ventas.Where(v => v.ClienteId == cliente.Id).ToList();
            var custodioCliente = compensaciones.Where(c => c.ClienteId == cliente.Id).ToList();

            var comprasBs = lc.Sum(c => c.MontoBs);
            var pagadoBs = lc.Sum(c => c.PagadoBs);
            var saldoComprasBs = lc.Sum(c => c.SaldoBs);
            var ventasBs = lv.Sum(v => v.MontoBs);
            var cobradoBs = lv.Sum(v => v.PagadoBs);
            var saldoVentasBs = lv.Sum(v => v.SaldoBs);

            var grupos = new List<SaldoGrupoDto>();
            var custodioBs = 0m;
            var custodioDivisa = 0m;

            foreach (var moneda in monedas)
            {
                var disponible = custodioCliente
                    .Where(c => c.MonedaId == moneda.Id)
                    .Sum(c => c.Abonos - c.Retiros);
                if (disponible == 0) continue;

                var bs = A_Bs(disponible, moneda, config.TasaActiva);
                grupos.Add(new SaldoGrupoDto
                {
                    MonedaId = moneda.Id, Descripcion = moneda.Descripcion, Nacional = moneda.Nacional,
                    Saldo = disponible, SaldoBs = bs
                });
                custodioBs += bs;
                if (!moneda.Nacional) custodioDivisa += disponible;
            }

            resultado.Clientes.Add(new EstadoGeneralClienteDto
            {
                ClienteId = cliente.Id,
                Cliente = cliente.NombreCompleto,
                Rif = cliente.Rif,
                Cedula = cliente.Cedula,
                ComprasBs = comprasBs,
                PagadoComprasBs = pagadoBs,
                SaldoComprasBs = saldoComprasBs,
                VentasBs = ventasBs,
                CobradoVentasBs = cobradoBs,
                SaldoVentasBs = saldoVentasBs,
                CustodioDivisa = custodioDivisa,
                CustodioBs = custodioBs,
                ConsolidadoBs = saldoVentasBs - saldoComprasBs + custodioBs,
                Custodios = grupos
            });

            resultado.Totales.ComprasBs += comprasBs;
            resultado.Totales.VentasBs += ventasBs;
            resultado.Totales.SaldoComprasBs += saldoComprasBs;
            resultado.Totales.SaldoVentasBs += saldoVentasBs;
            resultado.Totales.CustodioBs += custodioBs;
            resultado.Totales.ConsolidadoBs += saldoVentasBs - saldoComprasBs + custodioBs;
        }

        return resultado;
    }

    public async Task<CuadreCajaDto> CuadreCajaAsync(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        if (h < d) throw new ValidacionException("El rango de fechas es inválido (hasta < desde).");

        var (config, monedas) = await CargarBaseAsync();

        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha < h.AddDays(1) && x.Estado == "ACTIVO")
            .Select(x => new { x.Fecha, x.Origen, x.MonedaId, x.Monto })
            .ToListAsync();

        var resultado = new CuadreCajaDto { Desde = d, Hasta = h };

        foreach (var moneda in monedas)
        {
            var filas = movimientos.Where(x => x.MonedaId == moneda.Id).ToList();
            var anterior = filas.Where(x => x.Fecha < d).Sum(x => OrigenesCaja.Signo(x.Origen) * x.Monto);
            var enRango = filas.Where(x => x.Fecha >= d).ToList();
            var entradas = enRango.Where(x => OrigenesCaja.EsEntrada(x.Origen)).Sum(x => x.Monto);
            var salidas = enRango.Where(x => OrigenesCaja.EsSalida(x.Origen)).Sum(x => x.Monto);

            if (anterior == 0 && entradas == 0 && salidas == 0) continue;

            var grupo = new CuadreCajaMonedaDto
            {
                MonedaId = moneda.Id,
                Descripcion = moneda.Descripcion,
                Nacional = moneda.Nacional,
                SaldoAnterior = anterior,
                Entradas = entradas,
                Salidas = salidas,
                SaldoFinal = anterior + entradas - salidas,
                SaldoAnteriorBs = A_Bs(anterior, moneda, config.TasaActiva),
                EntradasBs = A_Bs(entradas, moneda, config.TasaActiva),
                SalidasBs = A_Bs(salidas, moneda, config.TasaActiva)
            };
            grupo.SaldoFinalBs = grupo.SaldoAnteriorBs + grupo.EntradasBs - grupo.SalidasBs;
            resultado.Monedas.Add(grupo);

            resultado.EntradasBs += grupo.EntradasBs;
            resultado.SalidasBs += grupo.SalidasBs;
            resultado.SaldoBs += grupo.SaldoFinalBs;
        }

        return resultado;
    }

    public async Task<CajaPorOrigenDto> CajaPorOrigenAsync(DateTime? desde, DateTime? hasta, string? tipo)
    {
        var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        if (h < d) throw new ValidacionException("El rango de fechas es inválido (hasta < desde).");

        var filtro = (tipo ?? "todos").Trim().ToLowerInvariant();
        if (filtro is not ("ingreso" or "egreso" or "todos"))
            throw new ValidacionException("El parámetro tipo debe ser ingreso, egreso o todos.");

        var (config, monedas) = await CargarBaseAsync();

        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha >= d && x.Fecha < h.AddDays(1) && x.Estado == "ACTIVO")
            .Select(x => new { x.Origen, x.MonedaId, x.Monto })
            .ToListAsync();

        var porMoneda = monedas.ToDictionary(m => m.Id, m => m);

        var resultado = new CajaPorOrigenDto { Desde = d, Hasta = h, Tipo = filtro };

        foreach (var grupo in movimientos.GroupBy(x => x.Origen).OrderBy(g => g.Key))
        {
            var esEntrada = OrigenesCaja.EsEntrada(grupo.Key);
            if (filtro == "ingreso" && !esEntrada) continue;
            if (filtro == "egreso" && esEntrada) continue;

            decimal entradas = 0, salidas = 0;
            foreach (var fila in grupo)
            {
                var moneda = fila.MonedaId is not null && porMoneda.TryGetValue(fila.MonedaId, out var m)
                    ? m : null;
                var montoBs = moneda is null ? fila.Monto : A_Bs(fila.Monto, moneda, config.TasaActiva);
                if (esEntrada) entradas += montoBs;
                else salidas += montoBs;
            }

            resultado.Grupos.Add(new CajaOrigenGrupoDto
            {
                Origen = grupo.Key,
                Descripcion = OrigenesCaja.Descripcion(grupo.Key),
                Sentido = esEntrada ? "ENTRADA" : "SALIDA",
                Cantidad = grupo.Count(),
                Entradas = entradas,
                Salidas = salidas,
                Neto = entradas - salidas
            });
        }

        resultado.Total = resultado.Grupos.Sum(g => g.Neto);
        return resultado;
    }

    public async Task<EstadoCuentaDto> EstadoCuentaAsync(string clienteId, DateTime? desde, DateTime? hasta)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
            throw new ValidacionException("Debe indicar el cliente.");

        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {clienteId}.");

        var (config, monedas) = await CargarBaseAsync();
        var d = desde?.Date;
        var h = hasta?.Date.AddDays(1);

        var qCaja = _db.MovimientosCaja.AsNoTracking().Where(x => x.ClienteId == clienteId && x.Estado == "ACTIVO");
        if (d.HasValue) qCaja = qCaja.Where(x => x.Fecha >= d.Value);
        if (h.HasValue) qCaja = qCaja.Where(x => x.Fecha < h.Value);
        var caja = await qCaja.ToListAsync();

        var qComp = _db.Compensaciones.AsNoTracking().Where(c => c.ClienteId == clienteId);
        if (d.HasValue) qComp = qComp.Where(c => c.Fecha >= d.Value);
        if (h.HasValue) qComp = qComp.Where(c => c.Fecha < h.Value);
        var compensaciones = await qComp.ToListAsync();

        var qTran = _db.Transferencias.AsNoTracking().Where(t => t.ClienteId == clienteId && t.Estado == "ACTIVO");
        if (d.HasValue) qTran = qTran.Where(t => t.Fecha >= d.Value);
        if (h.HasValue) qTran = qTran.Where(t => t.Fecha < h.Value);
        var transferencias = await qTran.ToListAsync();

        var porMoneda = monedas.ToDictionary(m => m.Id, m => m);
        var lineas = new List<EstadoCuentaLineaDto>();

        foreach (var movimiento in caja)
        {
            var sentido = OrigenesCaja.EsEntrada(movimiento.Origen) ? "ENTRADA" : "SALIDA";
            lineas.Add(NuevaLinea(movimiento.Fecha, "CAJA", movimiento.Origen, movimiento.Descripcion,
                movimiento.Recibo, movimiento.Monto, movimiento.MonedaId, sentido, config.TasaActiva, porMoneda,
                movimiento.MontoBs, movimiento.Tasa));
        }

        foreach (var compensacion in compensaciones)
        {
            var esAbono = compensacion.Abonos > 0;
            lineas.Add(NuevaLinea(compensacion.Fecha, "COMPENSACION", esAbono ? "ABONO" : "RETIRO",
                compensacion.Descripcion, compensacion.ReciboTran,
                esAbono ? compensacion.Abonos : compensacion.Retiros,
                compensacion.MonedaId, esAbono ? "ENTRADA" : "SALIDA", config.TasaActiva, porMoneda));
        }

        foreach (var transferencia in transferencias)
        {
            var sentido = transferencia.TipoTran == "DB" ? "SALIDA" : "ENTRADA";
            lineas.Add(NuevaLinea(transferencia.Fecha, "TRANSFERENCIA", transferencia.Origen,
                transferencia.Motivo ?? transferencia.Beneficiario,
                transferencia.NumeroTran, transferencia.Monto, transferencia.MonedaId,
                sentido, config.TasaActiva, porMoneda, transferencia.MontoBs, transferencia.Tasa));
        }

        lineas = lineas.OrderBy(l => l.Fecha).ThenBy(l => l.Tipo).ThenBy(l => l.Referencia).ToList();

        decimal acumulado = 0, entradas = 0, salidas = 0;
        foreach (var linea in lineas)
        {
            acumulado += linea.Sentido == "ENTRADA" ? linea.MontoBs : -linea.MontoBs;
            linea.AcumuladoBs = acumulado;
            if (linea.Sentido == "ENTRADA") entradas += linea.MontoBs;
            else salidas += linea.MontoBs;
        }

        return new EstadoCuentaDto
        {
            ClienteId = cliente.Id,
            Cliente = cliente.NombreCompleto,
            Desde = desde?.Date,
            Hasta = hasta?.Date,
            Tasa = config.TasaActiva,
            Lineas = lineas,
            TotalEntradasBs = entradas,
            TotalSalidasBs = salidas,
            SaldoBs = acumulado
        };
    }

    public async Task<ComprobanteDto> ComprobanteAsync(string tipo, string id)
    {
        var t = (tipo ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidacionException("Debe indicar el id del comprobante.");

        var (config, _) = await CargarBaseAsync();
        var emisor = new ComprobanteEmpresaDto
        {
            Nombre = config.NombreEmpresa,
            Rif = config.Rif,
            Direccion = config.Direccion,
            Telefono = config.Telefono
        };

        return t switch
        {
            "compra" or "c" => await ComprobanteOperacionAsync("C", id, emisor),
            "venta" or "v" => await ComprobanteOperacionAsync("V", id, emisor),
            "recibo" => await ComprobanteCajaAsync(id, emisor, soloIngresos: false),
            "ingreso" => await ComprobanteCajaAsync(id, emisor, soloIngresos: true),
            "entrega" => await ComprobanteCuotaAsync("V", id, emisor),
            "recepcion" => await ComprobanteCuotaAsync("C", id, emisor),
            _ => throw new ValidacionException($"Tipo de comprobante inválido: {tipo}. Use recibo, compra (o C), venta (o V), ingreso, entrega o recepcion.")
        };
    }

    // ---------------------------------------------------------------------
    private async Task<ComprobanteDto> ComprobanteOperacionAsync(string tipo, string id, ComprobanteEmpresaDto emisor)
    {
        var esCompra = tipo == "C";
        string? clienteId;
        ComprobanteDto dto;

        decimal tasa = 0, cantidad = 0, montoBs = 0, pagado = 0, saldo = 0, recibido = 0, porLiquidar = 0;
        DateTime fecha = default;
        string estado = "", observaciones = "", centro = "", monedaId = "0001";
        var recepciones = new List<OperacionRecepcion>();
        var pagos = new List<OperacionPago>();

        if (esCompra)
        {
            var op = await _db.Compras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new RecursoNoEncontradoException($"No existe la compra {id}.");
            clienteId = op.ClienteId; fecha = op.Fecha; tasa = op.Tasa; cantidad = op.Cantidad;
            montoBs = op.MontoBs; pagado = op.PagadoBs; saldo = op.SaldoBs; recibido = op.RecibidoDivisa;
            porLiquidar = op.PorRecibir; estado = op.Estado; observaciones = op.Observaciones ?? "";
            centro = op.CentroCosto ?? "";
            monedaId = op.MonedaDivisaId;
            recepciones = await _db.OperacionesRecepcion.AsNoTracking()
                .Where(r => r.TipoOp == "C" && r.OperacionId == id).OrderBy(r => r.NroPer).ToListAsync();
            pagos = await _db.OperacionesPago.AsNoTracking()
                .Where(r => r.TipoOp == "C" && r.OperacionId == id).OrderBy(r => r.NroPer).ToListAsync();
            dto = new ComprobanteDto { Numero = $"COMPRA {id}" };
        }
        else
        {
            var op = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new RecursoNoEncontradoException($"No existe la venta {id}.");
            clienteId = op.ClienteId; fecha = op.Fecha; tasa = op.Tasa; cantidad = op.Cantidad;
            montoBs = op.MontoBs; pagado = op.PagadoBs; saldo = op.SaldoBs; recibido = op.RecibidoDivisa;
            porLiquidar = op.PorRecibir; estado = op.Estado; observaciones = op.Observaciones ?? "";
            centro = op.CentroCosto ?? "";
            monedaId = op.MonedaDivisaId;
            recepciones = await _db.OperacionesRecepcion.AsNoTracking()
                .Where(r => r.TipoOp == "V" && r.OperacionId == id).OrderBy(r => r.NroPer).ToListAsync();
            pagos = await _db.OperacionesPago.AsNoTracking()
                .Where(r => r.TipoOp == "V" && r.OperacionId == id).OrderBy(r => r.NroPer).ToListAsync();
            dto = new ComprobanteDto { Numero = $"VENTA {id}" };
        }

        var monedaOp = await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == monedaId);

        dto.Tipo = esCompra ? "compra" : "venta";
        dto.Fecha = fecha;
        dto.Estado = estado;
        dto.Emisor = emisor;
        dto.Tasa = tasa;
        dto.Moneda = monedaOp?.Descripcion ?? "USD";
        dto.Cantidad = cantidad;
        dto.MontoBs = montoBs;
        dto.Observaciones = observaciones;
        dto.CentroCosto = centro;
        dto.EmitidoEn = DateTime.Now;
        dto.Cliente = await ClienteComprobanteAsync(clienteId);

        dto.Lineas =
        [
            new() { Concepto = "Cantidad de divisa", Valor = cantidad.ToString("0.00") },
            new() { Concepto = "Tasa aplicada", Valor = tasa.ToString("0.0000") },
            new() { Concepto = "Monto en Bs", Valor = montoBs.ToString("0.00") },
            new() { Concepto = esCompra ? "Pagado en Bs" : "Cobrado en Bs", Valor = pagado.ToString("0.00") },
            new() { Concepto = "Saldo en Bs", Valor = saldo.ToString("0.00") },
            new() { Concepto = esCompra ? "Divisa recibida" : "Divisa entregada", Valor = recibido.ToString("0.00") },
            new() { Concepto = esCompra ? "Divisa por recibir" : "Divisa por entregar", Valor = porLiquidar.ToString("0.00") },
            new() { Concepto = "Cuotas de divisa", Valor = recepciones.Count.ToString() },
            new() { Concepto = "Cuotas de Bs", Valor = pagos.Count.ToString() }
        ];
        return dto;
    }

    private async Task<ComprobanteDto> ComprobanteCajaAsync(string id, ComprobanteEmpresaDto emisor, bool soloIngresos)
    {
        var movimiento = await BuscarCajaAsync(id);
        if (movimiento is null)
            throw new RecursoNoEncontradoException($"No existe el recibo de caja {id}.");
        if (soloIngresos && !OrigenesCaja.EsEntrada(movimiento.Origen))
            throw new RecursoNoEncontradoException($"El recibo {movimiento.Recibo} no corresponde a un ingreso de caja.");

        var moneda = movimiento.MonedaId is null ? null
            : await _db.TipoMonedas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movimiento.MonedaId);
        var codigoIngreso = movimiento.CodiIngresoId is null ? null
            : await _db.CodificacionesIngreso.AsNoTracking().FirstOrDefaultAsync(c => c.Id == movimiento.CodiIngresoId);
        var codigoEgreso = movimiento.CodiEgresoId is null ? null
            : await _db.CodificacionesEgreso.AsNoTracking().FirstOrDefaultAsync(c => c.Id == movimiento.CodiEgresoId);

        var dto = new ComprobanteDto
        {
            Tipo = soloIngresos ? "ingreso" : "recibo",
            Numero = movimiento.Recibo,
            Fecha = movimiento.Fecha,
            Estado = movimiento.Estado,
            Emisor = emisor,
            Cliente = movimiento.ClienteId is null ? null : await ClienteComprobanteAsync(movimiento.ClienteId),
            Moneda = moneda?.Descripcion,
            Cantidad = moneda is { Nacional: false } ? movimiento.Monto : null,
            Monto = movimiento.Monto,
            MontoBs = movimiento.MontoBs is > 0 ? movimiento.MontoBs.Value : movimiento.Monto,
            Observaciones = movimiento.Observaciones,
            CentroCosto = movimiento.CentroCosto,
            EmitidoEn = movimiento.CreadoEn
        };

        dto.Lineas =
        [
            new() { Concepto = "Origen", Valor = $"{movimiento.Origen} — {OrigenesCaja.Descripcion(movimiento.Origen)}" },
            new() { Concepto = "Descripción", Valor = movimiento.Descripcion ?? "" },
            new() { Concepto = "Monto", Valor = movimiento.Monto.ToString("0.00") + (moneda is null ? "" : $" {moneda.Descripcion}") },
            new() { Concepto = "Tasa", Valor = movimiento.Tasa?.ToString("0.0000") ?? "" },
            new() { Concepto = "Equivalente en Bs", Valor = movimiento.MontoBs?.ToString("0.00") ?? "" },
            new() { Concepto = "Código de ingreso", Valor = codigoIngreso is null ? "" : $"{codigoIngreso.Id} {codigoIngreso.Descripcion}" },
            new() { Concepto = "Código de egreso", Valor = codigoEgreso is null ? "" : $"{codigoEgreso.Id} {codigoEgreso.Descripcion}" },
            new() { Concepto = "Estado", Valor = movimiento.Estado + (movimiento.AnuladoMotivo is null ? "" : $" ({movimiento.AnuladoMotivo})") }
        ];
        return dto;
    }

    private async Task<ComprobanteDto> ComprobanteCuotaAsync(string tipoOp, string id, ComprobanteEmpresaDto emisor)
    {
        OperacionRecepcion? cuota = null;

        if (long.TryParse(id, out var numero))
            cuota = await _db.OperacionesRecepcion.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == numero && r.TipoOp == tipoOp);

        cuota ??= await _db.OperacionesRecepcion.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Recibo == id || r.Recibo2 == id);

        if (cuota is null)
            throw new RecursoNoEncontradoException(
                $"No existe la cuota de {(tipoOp == "C" ? "recepción" : "entrega")} {id}.");

        var moneda = await _db.TipoMonedas.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == (tipoOp == "C" ? "0001" : "0001"));
        string? clienteId = null;
        if (tipoOp == "C")
            clienteId = await _db.Compras.AsNoTracking().Where(c => c.Id == cuota.OperacionId)
                .Select(c => c.ClienteId).FirstOrDefaultAsync();
        else
            clienteId = await _db.Ventas.AsNoTracking().Where(c => c.Id == cuota.OperacionId)
                .Select(c => c.ClienteId).FirstOrDefaultAsync();

        var dto = new ComprobanteDto
        {
            Tipo = tipoOp == "C" ? "recepcion" : "entrega",
            Numero = cuota.Recibo2 ?? cuota.Recibo ?? $"{cuota.Ejercicio}-{cuota.NroPer}",
            Fecha = cuota.Fecha,
            Estado = cuota.AnuladoEn is null ? "ACTIVO" : "ANULADO",
            Emisor = emisor,
            Cliente = clienteId is null ? null : await ClienteComprobanteAsync(clienteId),
            Moneda = moneda?.Descripcion,
            Cantidad = cuota.Cantidad,
            EmitidoEn = cuota.CreadoEn
        };

        dto.Lineas =
        [
            new() { Concepto = "Operación", Valor = cuota.OperacionId },
            new() { Concepto = "Cuota Nro.", Valor = cuota.NroPer.ToString("0") },
            new() { Concepto = "Cantidad de divisa", Valor = cuota.Cantidad.ToString("0.00") },
            new() { Concepto = "Divisa por recibir", Valor = cuota.PorRecibir?.ToString("0.00") ?? "" },
            new() { Concepto = "Forma de pago", Valor = cuota.TipoPago ?? "" },
            new() { Concepto = "Recibo (banco/compensación)", Valor = cuota.Recibo ?? "" },
            new() { Concepto = "Recibo (caja)", Valor = cuota.Recibo2 ?? "" },
            new() { Concepto = "De caja", Valor = cuota.DeCaja ? "Sí" : "No" },
            new() { Concepto = "De banco", Valor = cuota.DeBanco ? "Sí" : "No" }
        ];
        return dto;
    }

    private async Task<Caja?> BuscarCajaAsync(string id)
    {
        var directo = await _db.MovimientosCaja.AsNoTracking().FirstOrDefaultAsync(x => x.Recibo == id);
        if (directo is not null) return directo;

        var partes = id.Split('-', 2);
        if (partes.Length == 2)
        {
            return await _db.MovimientosCaja.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Ejercicio == partes[0] && x.Id == partes[1]);
        }

        return await _db.MovimientosCaja.AsNoTracking()
            .OrderByDescending(x => x.Fecha)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<ComprobanteClienteDto?> ClienteComprobanteAsync(string? clienteId)
    {
        if (string.IsNullOrWhiteSpace(clienteId)) return null;
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente is null) return null;

        return new ComprobanteClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.NombreCompleto,
            Rif = cliente.Rif,
            Cedula = cliente.Cedula,
            Direccion = cliente.Direccion,
            Telefonos = cliente.Telefonos
        };
    }

    private static EstadoCuentaLineaDto NuevaLinea(DateTime fecha, string tipo, string origen,
        string? descripcion, string? referencia, decimal monto, string? monedaId, string sentido,
        decimal tasa, Dictionary<string, TipoMoneda> porMoneda,
        decimal? montoBsPropio = null, decimal? tasaPropia = null)
    {
        TipoMoneda? moneda = null;
        if (monedaId is not null) porMoneda.TryGetValue(monedaId, out moneda);

        // Prioridad: monto_bs propio del movimiento (registrado en VFP) > tasa del
        // movimiento > tasa global. El origen histórico VFP guarda 0 cuando el monto
        // "no estaba calculado", así que un 0 no se considera valor y se recalcula.
        var montoBs = montoBsPropio is > 0
            ? montoBsPropio.Value
            : moneda is null
                ? monto
                : A_Bs(monto, moneda, tasaPropia is > 0 ? tasaPropia.Value : tasa);

        var tasaUsada = moneda is { Nacional: false } && montoBsPropio is not > 0
            ? (tasaPropia is > 0 ? tasaPropia.Value : tasa)
            : (decimal?)null;

        return new EstadoCuentaLineaDto
        {
            Fecha = fecha,
            Tipo = tipo,
            Origen = origen,
            Descripcion = descripcion,
            Referencia = referencia,
            Monto = monto,
            MonedaId = monedaId,
            Moneda = moneda?.Descripcion,
            Sentido = sentido,
            MontoBs = montoBs,
            Tasa = tasaUsada
        };
    }

    private static decimal A_Bs(decimal monto, TipoMoneda moneda, decimal tasaActiva)
        => moneda.Nacional || tasaActiva <= 0
            ? monto
            : Math.Round(monto * tasaActiva, 2, MidpointRounding.AwayFromZero);

    private async Task<(Configuracion config, List<TipoMoneda> monedas)> CargarBaseAsync()
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new ValidacionException("Falta la configuración general del sistema.");
        var monedas = await _db.TipoMonedas.AsNoTracking().Where(m => m.Activa).OrderBy(m => m.Id).ToListAsync();
        return (config, monedas);
    }
}
