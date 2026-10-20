using Microsoft.EntityFrameworkCore;
using Monrys.Api.Data;
using Monrys.Api.Domain;
using Monrys.Api.Dtos;

namespace Monrys.Api.Services;

public interface IInformeService
{
    /// <summary>Catálogo de informes disponibles agrupados por familias.</summary>
    Task<InformeCatalogoDto> CatalogoAsync();

    /// <summary>Ejecuta un informe del catálogo con sus parámetros.</summary>
    Task<InformeDto> EjecutarAsync(string id, InformeParametros p);
}

/// <summary>
/// Motor genérico de informes (Centro de Reportes). Reproduce la estructura de
/// los reportes VFP de C:\Mundodolar\Reportes: cada informe devuelve secciones
/// con columnas tipadas, filas y totales; el web los renderiza, imprime y
/// exporta. Los importes se agregan en memoria (rangos acotados), válido sobre
/// PostgreSQL y SQLite.
/// </summary>
public sealed class InformeService : IInformeService
{
    private readonly MonrysDbContext _db;
    private readonly IReportesService _reportes;

    public InformeService(MonrysDbContext db, IReportesService reportes)
    {
        _db = db;
        _reportes = reportes;
    }

    // =====================================================================
    // CATÁLOGO
    // =====================================================================
    public Task<InformeCatalogoDto> CatalogoAsync()
    {
        // «desde» nace con -30 días y «hasta» con hoy (el web resuelve estos tokens).
        var fFecha = (string clave, string etiqueta) =>
            new InformeParametroDto
            {
                Clave = clave,
                Etiqueta = etiqueta,
                Tipo = "fecha",
                Defecto = clave == "desde" ? "-30d" : clave == "hasta" ? "hoy" : null
            };

        var catalogo = new InformeCatalogoDto
        {
            Familias =
            [
                new()
                {
                    Codigo = "resumen", Nombre = "Resumen gerencial",
                    Descripcion = "Consolidados y estados globales del negocio.",
                    Informes =
                    [
                        new()
                        {
                            Id = "consolidado-operaciones", Nombre = "Consolidado de operaciones",
                            Descripcion = "Compras, ventas, circulante y compensaciones en Bs y divisas.",
                            OrigenVfp = "consolidado.FRX",
                            Parametros = [fFecha("desde", "Desde"), fFecha("hasta", "Hasta")]
                        },
                        new()
                        {
                            Id = "estado-general", Nombre = "Estado general por cliente",
                            Descripcion = "Debe/haber/saldo por cliente: compras, ventas y custodio.",
                            OrigenVfp = "estadogeneral.FRX",
                            RutaPantalla = "/reportes/estado-general"
                        }
                    ]
                },
                new()
                {
                    Codigo = "caja", Nombre = "Caja",
                    Descripcion = "Cuadre por rango y libro de caja agrupado por origen.",
                    Informes =
                    [
                        new()
                        {
                            Id = "cuadre-rango", Nombre = "Cuadre de caja por rango",
                            Descripcion = "Movimientos día a día con saldo anterior y acumulado (nacional o divisas).",
                            OrigenVfp = "cuadrebs.FRX / cuadredv.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new()
                                {
                                    Clave = "moneda", Etiqueta = "Moneda", Tipo = "select", Defecto = "nacional",
                                    Opciones =
                                    [
                                        new InformeOpcionDto { Valor = "nacional", Etiqueta = "Divisa nacional (Bs)" },
                                        new InformeOpcionDto { Valor = "divisas", Etiqueta = "Todas las divisas" }
                                    ]
                                }
                            ]
                        },
                        new()
                        {
                            Id = "ingresos-caja", Nombre = "Libro de caja por origen",
                            Descripcion = "Movimientos agrupados por origen (ingresos/egresos) con totales por grupo.",
                            OrigenVfp = "ingresos_caja.FRX / ingresos_caja_cs.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new()
                                {
                                    Clave = "tipo", Etiqueta = "Tipo", Tipo = "select", Defecto = "todos",
                                    Opciones =
                                    [
                                        new InformeOpcionDto { Valor = "todos", Etiqueta = "Todos" },
                                        new InformeOpcionDto { Valor = "ingreso", Etiqueta = "Solo ingresos" },
                                        new InformeOpcionDto { Valor = "egreso", Etiqueta = "Solo egresos" }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new()
                {
                    Codigo = "operaciones", Nombre = "Operaciones",
                    Descripcion = "Listados y detalle de compras, ventas y sus cuotas.",
                    Informes =
                    [
                        new()
                        {
                            Id = "operaciones-compras", Nombre = "Detalle de operaciones de compra",
                            Descripcion = "Compras del período con pagos, divisas recibidas y saldos.",
                            OrigenVfp = "detacomp.FRX / detarecibido.FRX / opercomp.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new InformeParametroDto { Clave = "clienteId", Etiqueta = "Cliente (opcional)", Tipo = "cliente" }
                            ]
                        },
                        new()
                        {
                            Id = "operaciones-ventas", Nombre = "Detalle de operaciones de venta",
                            Descripcion = "Ventas del período con cobros, divisas entregadas y saldos.",
                            OrigenVfp = "detavent.FRX / detaentrgad.FRX / opervent.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new InformeParametroDto { Clave = "clienteId", Etiqueta = "Cliente (opcional)", Tipo = "cliente" }
                            ]
                        },
                        new()
                        {
                            Id = "cobros-ventas", Nombre = "Informe de cobros (ventas)",
                            Descripcion = "Cobros en Bs de cada venta del período, con totales por venta.",
                            OrigenVfp = "cobros.FRX / pagos.FRX",
                            Parametros = [fFecha("desde", "Desde"), fFecha("hasta", "Hasta")]
                        },
                        new()
                        {
                            Id = "recepciones-compras", Nombre = "Informe de recepciones (compras)",
                            Descripcion = "Cuotas de divisa recibida por cada compra del período.",
                            OrigenVfp = "recepcion.FRX",
                            Parametros = [fFecha("desde", "Desde"), fFecha("hasta", "Hasta")]
                        },
                        new()
                        {
                            Id = "entregas-ventas", Nombre = "Informe de entregas (ventas)",
                            Descripcion = "Cuotas de divisa entregada por cada venta del período.",
                            OrigenVfp = "entregas.FRX",
                            Parametros = [fFecha("desde", "Desde"), fFecha("hasta", "Hasta")]
                        },
                        new()
                        {
                            Id = "transferencias-rango", Nombre = "Transferencias del período",
                            Descripcion = "Libro de transferencias bancarias con totales.",
                            OrigenVfp = "transferencia.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new InformeParametroDto { Clave = "bancoId", Etiqueta = "Banco (opcional)", Tipo = "banco" }
                            ]
                        }
                    ]
                },
                new()
                {
                    Codigo = "clientes", Nombre = "Clientes",
                    Descripcion = "Saldos por divisa, fichas y directorio.",
                    Informes =
                    [
                        new()
                        {
                            Id = "saldos-divisas-compras", Nombre = "Saldo por recibir en divisas",
                            Descripcion = "Por cliente: divisas comprometidas, recibidas y por recibir.",
                            OrigenVfp = "detallecompras_clie.FRX"
                        },
                        new()
                        {
                            Id = "saldos-divisas-ventas", Nombre = "Saldo por entregar en divisas",
                            Descripcion = "Por cliente: divisas comprometidas, entregadas y por entregar.",
                            OrigenVfp = "DETALLEVENTAS_CLIE.FRX"
                        },
                        new()
                        {
                            Id = "cuenta-cliente", Nombre = "Cuenta del cliente",
                            Descripcion = "Estado de cuenta detallado (línea de tiempo) de un cliente.",
                            OrigenVfp = "detallecompras.FRX / detalleventas.FRX",
                            RutaPantalla = "/reportes/estado-cuenta",
                            Parametros =
                            [
                                new InformeParametroDto { Clave = "clienteId", Etiqueta = "Cliente", Tipo = "cliente", Requerido = true },
                                fFecha("desde", "Desde (opcional)"), fFecha("hasta", "Hasta (opcional)")
                            ]
                        },
                        new()
                        {
                            Id = "ficha-cliente", Nombre = "Ficha del cliente",
                            Descripcion = "Datos de identificación, contacto y cuentas bancarias.",
                            OrigenVfp = "fichaclie.FRX",
                            Parametros =
                            [
                                new InformeParametroDto { Clave = "clienteId", Etiqueta = "Cliente", Tipo = "cliente", Requerido = true }
                            ]
                        },
                        new()
                        {
                            Id = "lista-clientes", Nombre = "Lista de clientes",
                            Descripcion = "Directorio imprimible: nombre, RIF, cédula, teléfonos y correo.",
                            OrigenVfp = "listaclie.FRX",
                            Parametros =
                            [
                                new InformeParametroDto { Clave = "texto", Etiqueta = "Buscar (opcional)", Tipo = "texto" }
                            ]
                        }
                    ]
                },
                new()
                {
                    Codigo = "bancos", Nombre = "Bancos",
                    Descripcion = "Extractos y movimientos de cuentas de la casa.",
                    Informes =
                    [
                        new()
                        {
                            Id = "extracto-banco", Nombre = "Extracto bancario",
                            Descripcion = "Saldo anterior, movimientos y saldo acumulado por cuenta.",
                            OrigenVfp = "genelal_1.FRX / saldog1.FRX",
                            Parametros =
                            [
                                new InformeParametroDto { Clave = "bancoId", Etiqueta = "Banco", Tipo = "banco", Requerido = true },
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new InformeParametroDto { Clave = "cuenta", Etiqueta = "Nº de cuenta (opcional)", Tipo = "texto" }
                            ]
                        },
                        new()
                        {
                            Id = "movimientos-banco", Nombre = "Movimientos bancarios",
                            Descripcion = "Créditos y débitos de transferencias por banco.",
                            OrigenVfp = "movimientoscr.FRX / movimientosdb.FRX",
                            Parametros =
                            [
                                new InformeParametroDto { Clave = "bancoId", Etiqueta = "Banco (opcional)", Tipo = "banco" },
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new()
                                {
                                    Clave = "tipo", Etiqueta = "Tipo", Tipo = "select", Defecto = "todos",
                                    Opciones =
                                    [
                                        new InformeOpcionDto { Valor = "todos", Etiqueta = "Créditos y débitos" },
                                        new InformeOpcionDto { Valor = "credito", Etiqueta = "Solo créditos" },
                                        new InformeOpcionDto { Valor = "debito", Etiqueta = "Solo débitos" }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new()
                {
                    Codigo = "custodios", Nombre = "Custodios",
                    Descripcion = "Movimientos y disponibilidad de custodia de clientes.",
                    Informes =
                    [
                        new()
                        {
                            Id = "custodios-cliente", Nombre = "Informe de custodios",
                            Descripcion = "Depósitos, retiros y saldo de custodia en un período (por cliente).",
                            OrigenVfp = "ver_custodios.FRX / ver_custodiosclie.FRX",
                            Parametros =
                            [
                                fFecha("desde", "Desde"), fFecha("hasta", "Hasta"),
                                new InformeParametroDto { Clave = "clienteId", Etiqueta = "Cliente (opcional)", Tipo = "cliente" }
                            ]
                        },
                        new()
                        {
                            Id = "custodios-todos", Nombre = "Custodios — resumen por cliente",
                            Descripcion = "Depósitos, retiros, saldo y disponibilidad actual por cliente y moneda.",
                            OrigenVfp = "ver_custodiostodos.FRX"
                        },
                        new()
                        {
                            Id = "custodios-banco", Nombre = "Custodios vía banco",
                            Descripcion = "Movimientos de custodia registrados con recibo/cuenta bancaria.",
                            OrigenVfp = "ver_custodiosb.FRX",
                            Parametros = [fFecha("desde", "Desde"), fFecha("hasta", "Hasta")]
                        }
                    ]
                },
                new()
                {
                    Codigo = "comprobantes", Nombre = "Comprobantes",
                    Descripcion = "Los comprobantes imprimibles (compra, venta, recibo de caja, custodio y transferencia) se emiten desde las listas de Compras, Ventas, Caja y Transferencias con el botón «Comprobante».",
                    Informes = []
                }
            ]
        };
        return Task.FromResult(catalogo);
    }

    // =====================================================================
    // EJECUCIÓN
    // =====================================================================
    public async Task<InformeDto> EjecutarAsync(string id, InformeParametros p)
    {
        var informe = new InformeDto { Id = id };
        var (config, monedas) = await CargarBaseAsync();
        informe.Empresa = new InformeEmpresaDto
        {
            Nombre = config.NombreEmpresa,
            Rif = config.Rif,
            Direccion = config.Direccion,
            Telefono = config.Telefono
        };

        var (desde, hasta) = Rango(p);
        informe.Periodo = p.Desde != null || p.Hasta != null
            ? $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}"
            : null;

        switch (id)
        {
            case "consolidado-operaciones":
                await ConsolidadoOperacionesAsync(informe, config, monedas, desde, hasta);
                break;
            case "estado-general":
                await EstadoGeneralAsync(informe, hasta);
                break;
            case "cuadre-rango":
                await CuadreRangoAsync(informe, config, monedas, desde, hasta, p.Moneda);
                break;
            case "ingresos-caja":
                await IngresosCajaAsync(informe, desde, hasta, p.Tipo);
                break;
            case "operaciones-compras":
                await OperacionesAsync(informe, "C", desde, hasta, p.ClienteId, monedas);
                break;
            case "operaciones-ventas":
                await OperacionesAsync(informe, "V", desde, hasta, p.ClienteId, monedas);
                break;
            case "cobros-ventas":
                await CuotasOperacionesAsync(informe, "V", "pagos", desde, hasta);
                break;
            case "recepciones-compras":
                await CuotasOperacionesAsync(informe, "C", "recepciones", desde, hasta);
                break;
            case "entregas-ventas":
                await CuotasOperacionesAsync(informe, "V", "recepciones", desde, hasta);
                break;
            case "transferencias-rango":
                await TransferenciasRangoAsync(informe, desde, hasta, p.BancoId);
                break;
            case "saldos-divisas-compras":
                await SaldosDivisasAsync(informe, "C", monedas);
                break;
            case "saldos-divisas-ventas":
                await SaldosDivisasAsync(informe, "V", monedas);
                break;
            case "cuenta-cliente":
                await CuentaClienteAsync(informe, p, desde, hasta);
                break;
            case "ficha-cliente":
                await FichaClienteAsync(informe, p.ClienteId);
                break;
            case "lista-clientes":
                await ListaClientesAsync(informe, p.Texto);
                break;
            case "extracto-banco":
                await ExtractoBancoAsync(informe, p, desde, hasta);
                break;
            case "movimientos-banco":
                await MovimientosBancoAsync(informe, p, desde, hasta);
                break;
            case "custodios-cliente":
                await CustodiosClienteAsync(informe, desde, hasta, p.ClienteId, monedas);
                break;
            case "custodios-todos":
                await CustodiosTodosAsync(informe, monedas);
                break;
            case "custodios-banco":
                await CustodiosBancoAsync(informe, desde, hasta, monedas);
                break;
            default:
                throw new RecursoNoEncontradoException($"El informe '{id}' no existe.");
        }

        return informe;
    }

    // =====================================================================
    // RESUMEN GERENCIAL
    // =====================================================================
    private async Task ConsolidadoOperacionesAsync(InformeDto informe, Configuracion config,
        List<TipoMoneda> monedas, DateTime desde, DateTime hasta)
    {
        informe.Titulo = "CONSOLIDADO DE OPERACIONES";
        informe.Subtitulo = "Compras, ventas, circulante y compensaciones";

        var compras = await _db.Compras.AsNoTracking()
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta && c.Estado == "ACTIVO")
            .Select(c => new { c.MontoBs, c.PagadoBs, c.SaldoBs, c.Cantidad, c.RecibidoDivisa, c.PorRecibir })
            .ToListAsync();
        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Fecha >= desde && v.Fecha <= hasta && v.Estado == "ACTIVO")
            .Select(v => new { v.MontoBs, v.PagadoBs, v.SaldoBs, v.Cantidad, v.RecibidoDivisa, v.PorRecibir })
            .ToListAsync();

        var nacionalId = config.MonedaNacionalId;
        var movCaja = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha <= hasta && x.Estado == "ACTIVO")
            .Select(x => new { x.Origen, x.MonedaId, x.Monto })
            .ToListAsync();
        var compensaciones = await _db.Compensaciones.AsNoTracking()
            .Where(c => c.Fecha <= hasta)
            .Select(c => new { c.MonedaId, c.Abonos, c.Retiros })
            .ToListAsync();
        var bancos = await _db.Bancos.AsNoTracking().Where(b => b.Activo).ToListAsync();

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "concepto", Etiqueta = "Concepto" },
            new() { Clave = "bs", Etiqueta = "Monto Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "divisa", Etiqueta = "Monto divisa", Tipo = "moneda", Totalizable = true }
        };

        // --- COMPRAS ---
        var fCompras = new List<Dictionary<string, object?>>
        {
            Fila(("concepto", "Total de compras"), ("bs", compras.Sum(c => c.MontoBs)), ("divisa", compras.Sum(c => c.Cantidad))),
            Fila(("concepto", "Pagado en Bs"), ("bs", compras.Sum(c => c.PagadoBs)), ("divisa", null)),
            Fila(("concepto", "CxP por pagar (Bs)"), ("bs", compras.Sum(c => c.SaldoBs)), ("divisa", null)),
            Fila(("concepto", "Divisas recibidas"), ("bs", null), ("divisa", compras.Sum(c => c.RecibidoDivisa))),
            Fila(("concepto", "Divisas por recibir"), ("bs", null), ("divisa", compras.Sum(c => c.PorRecibir)))
        };
        informe.Secciones.Add(new InformeSeccionDto
        {
            Nombre = "COMPRAS", Columnas = columnas, Filas = fCompras, Totales = Totales(columnas, fCompras)
        });

        // --- VENTAS ---
        var fVentas = new List<Dictionary<string, object?>>
        {
            Fila(("concepto", "Total de ventas"), ("bs", ventas.Sum(v => v.MontoBs)), ("divisa", ventas.Sum(v => v.Cantidad))),
            Fila(("concepto", "Cobrado en Bs"), ("bs", ventas.Sum(v => v.PagadoBs)), ("divisa", null)),
            Fila(("concepto", "CcP por cobrar (Bs)"), ("bs", ventas.Sum(v => v.SaldoBs)), ("divisa", null)),
            Fila(("concepto", "Divisas entregadas"), ("bs", null), ("divisa", ventas.Sum(v => v.RecibidoDivisa))),
            Fila(("concepto", "Divisas por entregar"), ("bs", null), ("divisa", ventas.Sum(v => v.PorRecibir)))
        };
        informe.Secciones.Add(new InformeSeccionDto
        {
            Nombre = "VENTAS", Columnas = columnas, Filas = fVentas, Totales = Totales(columnas, fVentas)
        });

        // --- CIRCULANTE (caja y bancos) ---
        decimal cajaNac = 0, cajaDivisaBs = 0, cajaDivisa = 0;
        foreach (var moneda in monedas)
        {
            var ids = moneda.Nacional ? new[] { moneda.Id, nacionalId } : new[] { moneda.Id };
            var saldo = movCaja.Where(x => x.MonedaId != null && ids.Contains(x.MonedaId))
                .Sum(x => OrigenesCaja.Signo(x.Origen) * x.Monto);
            if (saldo == 0) continue;
            if (moneda.Nacional)
            {
                cajaNac += saldo;
            }
            else
            {
                cajaDivisa += saldo;
                cajaDivisaBs += Math.Round(saldo * moneda.MontoDolar, 2, MidpointRounding.AwayFromZero);
            }
        }
        var fCirculante = new List<Dictionary<string, object?>>
        {
            Fila(("concepto", "Caja (divisa nacional)"), ("bs", cajaNac), ("divisa", null)),
            Fila(("concepto", "Caja (divisas)"), ("bs", cajaDivisaBs), ("divisa", cajaDivisa)),
            Fila(("concepto", "Bancos (saldo en Bs)"), ("bs", bancos.Sum(b => b.Saldo)), ("divisa", null))
        };
        informe.Secciones.Add(new InformeSeccionDto
        {
            Nombre = "CIRCULANTE", Columnas = columnas, Filas = fCirculante, Totales = Totales(columnas, fCirculante)
        });

        // --- COMPENSACIONES (custodio) ---
        var fComp = new List<Dictionary<string, object?>>();
        foreach (var moneda in monedas)
        {
            var saldo = compensaciones.Where(c => c.MonedaId == moneda.Id).Sum(c => c.Abonos - c.Retiros);
            if (saldo == 0) continue;
            var bs = moneda.Nacional
                ? saldo
                : Math.Round(saldo * moneda.MontoDolar, 2, MidpointRounding.AwayFromZero);
            fComp.Add(Fila(("concepto", $"En custodio — {moneda.Descripcion}"), ("bs", bs),
                ("divisa", moneda.Nacional ? null : saldo)));
        }
        if (fComp.Count == 0)
            fComp.Add(Fila(("concepto", "Sin saldos en custodia"), ("bs", 0m), ("divisa", 0m)));
        informe.Secciones.Add(new InformeSeccionDto
        {
            Nombre = "COMPENSACIONES (CUSTODIO)", Columnas = columnas, Filas = fComp, Totales = Totales(columnas, fComp)
        });

        var tasaRef = monedas.FirstOrDefault(m => !m.Nacional)?.MontoDolar ?? 0m;
        if (tasaRef > 0)
            informe.Notas.Add(new InformeNotaDto { Texto = $"Equivalencia de divisas a Bs con tasa de referencia {tasaRef:0.####}." });
    }

    /// <summary>Estado general por cliente (misma base del reporte VFP estadogeneral.FRX).</summary>
    private async Task EstadoGeneralAsync(InformeDto informe, DateTime hasta)
    {
        var dto = await _reportes.EstadoGeneralAsync(hasta, null);
        informe.Titulo = "ESTADO GENERAL POR CLIENTE";
        informe.Subtitulo = $"Corte al {dto.FechaCorte:dd/MM/yyyy}";

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "rif", Etiqueta = "RIF / Cédula" },
            new() { Clave = "compras", Etiqueta = "Compras Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "pagadoC", Etiqueta = "Pagado Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldoC", Etiqueta = "CxP Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "ventas", Etiqueta = "Ventas Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "cobradoV", Etiqueta = "Cobrado Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldoV", Etiqueta = "CcP Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "custodioDivisa", Etiqueta = "Custodio divisa", Tipo = "moneda", Totalizable = true },
            new() { Clave = "custodioBs", Etiqueta = "Custodio Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "consolidado", Etiqueta = "Consolidado Bs", Tipo = "moneda", Totalizable = true }
        };
        var filas = dto.Clientes
            .OrderBy(c => c.Cliente)
            .Select(c => Fila(
                ("cliente", c.Cliente),
                ("rif", c.Rif ?? c.Cedula),
                ("compras", c.ComprasBs),
                ("pagadoC", c.PagadoComprasBs),
                ("saldoC", c.SaldoComprasBs),
                ("ventas", c.VentasBs),
                ("cobradoV", c.CobradoVentasBs),
                ("saldoV", c.SaldoVentasBs),
                ("custodioDivisa", c.CustodioDivisa),
                ("custodioBs", c.CustodioBs),
                ("consolidado", c.ConsolidadoBs)))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        informe.Notas.Add(new InformeNotaDto { Texto = $"Tasa de referencia usada: {dto.TasaActiva:0.####} Bs. Consolidado = CcP − CxP + custodio en Bs." });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin clientes con movimientos al corte." });
    }

    // =====================================================================
    // CAJA
    // =====================================================================
    private async Task CuadreRangoAsync(InformeDto informe, Configuracion config, List<TipoMoneda> monedas,
        DateTime desde, DateTime hasta, string? monedaFiltro)
    {
        var modo = (monedaFiltro ?? "nacional").Trim().ToLowerInvariant();
        informe.Titulo = modo == "divisas" ? "CUADRE DE CAJA DIVISAS" : "CUADRE DE CAJA DIVISA NACIONAL";

        List<TipoMoneda> seleccion;
        if (modo == "divisas") seleccion = monedas.Where(m => !m.Nacional).ToList();
        else if (modo == "todas") seleccion = monedas;
        else seleccion = monedas.Where(m => m.Nacional).ToList();

        if (seleccion.Count == 0)
            throw new ValidacionException("No hay monedas activas para el filtro seleccionado.");

        var nacionalId = config.MonedaNacionalId;
        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha <= hasta && x.Estado == "ACTIVO")
            .Select(x => new { x.Fecha, x.Origen, x.Recibo, x.Descripcion, x.MonedaId, x.Monto })
            .ToListAsync();

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "descripcion", Etiqueta = "Descripción" },
            new() { Clave = "entrada", Etiqueta = "Entrada", Tipo = "moneda", Totalizable = true },
            new() { Clave = "salida", Etiqueta = "Salida", Tipo = "moneda", Totalizable = true }
        };

        foreach (var moneda in seleccion)
        {
            // La divisa nacional agrupa también los movimientos sin moneda (VFP: blancos = Bs).
            var filasMoneda = movimientos
                .Where(x => moneda.Nacional
                    ? (x.MonedaId == null || x.MonedaId == moneda.Id || x.MonedaId == nacionalId)
                    : x.MonedaId == moneda.Id)
                .ToList();

            var saldoAnterior = filasMoneda.Where(x => x.Fecha < desde)
                .Sum(x => OrigenesCaja.Signo(x.Origen) * x.Monto);
            var enRango = filasMoneda.Where(x => x.Fecha >= desde)
                .OrderBy(x => x.Fecha).ThenBy(x => x.Recibo).ToList();

            if (saldoAnterior == 0 && enRango.Count == 0) continue;

            var filas = new List<Dictionary<string, object?>>
            {
                Fila(("fecha", null), ("recibo", "SALDO ANTERIOR"), ("descripcion", $"Saldo al {desde:dd/MM/yyyy}"),
                     ("entrada", null), ("salida", null))
            };
            foreach (var m in enRango)
            {
                var esEntrada = OrigenesCaja.EsEntrada(m.Origen);
                filas.Add(Fila(
                    ("fecha", m.Fecha),
                    ("recibo", m.Recibo),
                    ("descripcion", m.Descripcion ?? OrigenesCaja.Descripcion(m.Origen)),
                    ("entrada", esEntrada ? m.Monto : null),
                    ("salida", esEntrada ? null : m.Monto)));
            }
            var entradas = enRango.Where(x => OrigenesCaja.EsEntrada(x.Origen)).Sum(x => x.Monto);
            var salidas = enRango.Where(x => OrigenesCaja.EsSalida(x.Origen)).Sum(x => x.Monto);
            filas.Add(Fila(("fecha", null), ("recibo", "TOTAL"), ("descripcion", $"Saldo final: {(saldoAnterior + entradas - salidas):0.00}"),
                ("entrada", entradas), ("salida", salidas)));

            informe.Secciones.Add(new InformeSeccionDto
            {
                Nombre = seleccion.Count > 1 || !moneda.Nacional ? $"Moneda {moneda.Descripcion}" : null,
                Columnas = columnas,
                Filas = filas,
                Totales = Fila(("entrada", entradas), ("salida", salidas))
            });
        }

        if (informe.Secciones.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos de caja en el rango seleccionado." });
        else
            informe.Notas.Add(new InformeNotaDto { Texto = "Las filas SALDO ANTERIOR y TOTAL muestran el acumulado de la caja." });
    }

    private async Task IngresosCajaAsync(InformeDto informe, DateTime desde, DateTime hasta, string? tipo)
    {
        informe.Titulo = "LIBRO DE CAJA POR ORIGEN";
        var filtro = (tipo ?? "todos").Trim().ToLowerInvariant();
        if (filtro is not ("ingreso" or "egreso" or "todos"))
            throw new ValidacionException("El parámetro tipo debe ser ingreso, egreso o todos.");

        var movimientos = await _db.MovimientosCaja.AsNoTracking()
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta && x.Estado == "ACTIVO")
            .Select(x => new { x.Fecha, x.Origen, x.Recibo, x.Descripcion, x.Monto, x.ClienteId, x.CentroCosto })
            .ToListAsync();
        var nombres = await ClientesDiccionarioAsync(
            movimientos.Where(m => m.ClienteId != null).Select(m => m.ClienteId!).Distinct());

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "descripcion", Etiqueta = "Descripción" },
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "monto", Etiqueta = "Monto", Tipo = "moneda", Totalizable = true }
        };

        var grupos = movimientos
            .Where(m => filtro == "todos" || (filtro == "ingreso") == OrigenesCaja.EsEntrada(m.Origen))
            .GroupBy(m => m.Origen)
            .OrderBy(g => OrigenesCaja.EsEntrada(g.Key) ? 0 : 1).ThenBy(g => g.Key)
            .ToList();

        foreach (var grupo in grupos)
        {
            var filas = grupo.OrderBy(m => m.Fecha).ThenBy(m => m.Recibo)
                .Select(m => Fila(
                    ("fecha", m.Fecha),
                    ("recibo", m.Recibo),
                    ("descripcion", m.Descripcion),
                    ("cliente", m.ClienteId != null && nombres.TryGetValue(m.ClienteId, out var n) ? n : null),
                    ("monto", OrigenesCaja.Signo(grupo.Key) * m.Monto)))
                .ToList();
            informe.Secciones.Add(new InformeSeccionDto
            {
                Nombre = $"{grupo.Key} — {OrigenesCaja.Descripcion(grupo.Key)} ({(OrigenesCaja.EsEntrada(grupo.Key) ? "ENTRADA" : "SALIDA")})",
                Columnas = columnas,
                Filas = filas,
                Totales = Totales(columnas, filas)
            });
        }

        if (informe.Secciones.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos de caja en el rango seleccionado." });
        else
            informe.Notas.Add(new InformeNotaDto { Texto = "Los montos de egresos se muestran en negativo." });
    }

    // =====================================================================
    // OPERACIONES
    // =====================================================================
    private async Task OperacionesAsync(InformeDto informe, string tipoOp, DateTime desde, DateTime hasta,
        string? clienteId, List<TipoMoneda> monedas)
    {
        var esCompra = tipoOp == "C";
        informe.Titulo = esCompra ? "DETALLE DE OPERACIONES DE COMPRA" : "DETALLE DE OPERACIONES DE VENTA";

        var ops = esCompra
            ? await _db.Compras.AsNoTracking()
                .Where(c => c.Fecha >= desde && c.Fecha <= hasta && c.Estado == "ACTIVO"
                            && (clienteId == null || c.ClienteId == clienteId))
                .Select(c => new { c.Id, c.ClienteId, c.Fecha, c.MonedaDivisaId, c.Tasa, c.Cantidad, c.MontoBs, c.PagadoBs, c.SaldoBs, c.RecibidoDivisa, c.PorRecibir })
                .OrderBy(c => c.ClienteId).ThenBy(c => c.Fecha)
                .ToListAsync()
            : await _db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= desde && v.Fecha <= hasta && v.Estado == "ACTIVO"
                            && (clienteId == null || v.ClienteId == clienteId))
                .Select(v => new { v.Id, v.ClienteId, v.Fecha, v.MonedaDivisaId, v.Tasa, v.Cantidad, v.MontoBs, v.PagadoBs, v.SaldoBs, v.RecibidoDivisa, v.PorRecibir })
                .OrderBy(v => v.ClienteId).ThenBy(v => v.Fecha)
                .ToListAsync();

        var nombres = await ClientesDiccionarioAsync(ops.Select(o => o.ClienteId).Distinct());
        var monedasDic = monedas.ToDictionary(m => m.Id, m => m.Descripcion);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "nro", Etiqueta = "Nro. operación" },
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "divisa", Etiqueta = "Divisa" },
            new() { Clave = "cantidad", Etiqueta = "Cantidad divisa", Tipo = "moneda", Totalizable = true },
            new() { Clave = "tasa", Etiqueta = "Tasa", Tipo = "numero" },
            new() { Clave = "monto", Etiqueta = "Monto Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "pagado", Etiqueta = esCompra ? "Pagado Bs" : "Cobrado Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldo", Etiqueta = "Saldo Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "recibido", Etiqueta = esCompra ? "Divisa recibida" : "Divisa entregada", Tipo = "moneda", Totalizable = true },
            new() { Clave = "pendiente", Etiqueta = esCompra ? "Por recibir" : "Por entregar", Tipo = "moneda", Totalizable = true }
        };

        var filas = ops.Select(o => Fila(
                ("fecha", o.Fecha),
                ("nro", o.Id),
                ("cliente", nombres.GetValueOrDefault(o.ClienteId, o.ClienteId)),
                ("divisa", monedasDic.GetValueOrDefault(o.MonedaDivisaId, o.MonedaDivisaId)),
                ("cantidad", o.Cantidad),
                ("tasa", o.Tasa),
                ("monto", o.MontoBs),
                ("pagado", o.PagadoBs),
                ("saldo", o.SaldoBs),
                ("recibido", o.RecibidoDivisa),
                ("pendiente", o.PorRecibir)))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin operaciones en el rango seleccionado." });
    }

    /// <summary>Informe por operación con el detalle de sus cuotas (cobros, recepciones o entregas).</summary>
    private async Task CuotasOperacionesAsync(InformeDto informe, string tipoOp, string modo,
        DateTime desde, DateTime hasta)
    {
        var esCompra = tipoOp == "C";
        informe.Titulo = modo == "pagos"
            ? (esCompra ? "PAGOS DE COMPRAS" : "INFORME DE COBROS (VENTAS)")
            : (esCompra ? "INFORME DE RECEPCIONES (COMPRAS)" : "INFORME DE ENTREGAS (VENTAS)");

        var ops = esCompra
            ? await _db.Compras.AsNoTracking()
                .Where(c => c.Fecha >= desde && c.Fecha <= hasta && c.Estado == "ACTIVO")
                .Select(c => new { c.Id, c.ClienteId, c.Fecha, c.PagadoBs, c.SaldoBs, c.RecibidoDivisa, c.PorRecibir })
                .OrderBy(c => c.Fecha)
                .ToListAsync()
            : await _db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= desde && v.Fecha <= hasta && v.Estado == "ACTIVO")
                .Select(v => new { v.Id, v.ClienteId, v.Fecha, v.PagadoBs, v.SaldoBs, v.RecibidoDivisa, v.PorRecibir })
                .OrderBy(v => v.Fecha)
                .ToListAsync();

        if (ops.Count == 0)
        {
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin operaciones en el rango seleccionado." });
            return;
        }
        var nombres = await ClientesDiccionarioAsync(ops.Select(o => o.ClienteId).Distinct());

        var columnasPagos = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "cuota", Etiqueta = "Cuota" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "descripcion", Etiqueta = "Concepto" },
            new() { Clave = "monto", Etiqueta = "Monto Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldo", Etiqueta = "Saldo Bs", Tipo = "moneda" }
        };
        var columnasCuotas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "cuota", Etiqueta = "Cuota" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "descripcion", Etiqueta = "Concepto" },
            new() { Clave = "cantidad", Etiqueta = "Cantidad divisa", Tipo = "moneda", Totalizable = true },
            new() { Clave = "porrecibir", Etiqueta = "Por liquidar", Tipo = "moneda" }
        };

        foreach (var op in ops)
        {
            var seccion = new InformeSeccionDto
            {
                Nombre = $"{(esCompra ? "COMPRA" : "VENTA")} {op.Id} — {nombres.GetValueOrDefault(op.ClienteId, op.ClienteId)} — {op.Fecha:dd/MM/yyyy}",
                Columnas = modo == "pagos" ? columnasPagos : columnasCuotas
            };

            if (modo == "pagos")
            {
                var pagos = await _db.OperacionesPago.AsNoTracking()
                    .Where(x => x.TipoOp == tipoOp && x.OperacionId == op.Id && x.AnuladoEn == null)
                    .OrderBy(x => x.NroPer)
                    .ToListAsync();
                foreach (var pago in pagos)
                {
                    seccion.Filas.Add(Fila(
                        ("fecha", pago.Fecha),
                        ("cuota", pago.NroPer),
                        ("recibo", pago.Recibo),
                        ("descripcion", pago.TipoPago ?? (esCompra ? "Pago en Bs" : "Cobro en Bs")),
                        ("monto", pago.Monto),
                        ("saldo", pago.Saldo)));
                }
                seccion.Totales = Fila(("monto", pagos.Sum(x => x.Monto)));
                seccion.Filas.Add(Fila(("fecha", null), ("cuota", ""), ("recibo", "TOTAL"),
                    ("descripcion", esCompra ? "Total pagado en la operación" : "Total cobrado en la operación"),
                    ("monto", op.PagadoBs), ("saldo", op.SaldoBs)));
            }
            else
            {
                var cuotas = await _db.OperacionesRecepcion.AsNoTracking()
                    .Where(x => x.TipoOp == tipoOp && x.OperacionId == op.Id && x.AnuladoEn == null)
                    .OrderBy(x => x.NroPer)
                    .ToListAsync();
                foreach (var cuota in cuotas)
                {
                    seccion.Filas.Add(Fila(
                        ("fecha", cuota.Fecha),
                        ("cuota", cuota.NroPer),
                        ("recibo", cuota.Recibo2 ?? cuota.Recibo),
                        ("descripcion", cuota.DeBanco ? "Divisa recibida en banco" : cuota.DeCaja ? "Divisa recibida en caja" : "Cuota de divisa"),
                        ("cantidad", cuota.Cantidad),
                        ("porrecibir", cuota.PorRecibir)));
                }
                seccion.Totales = Fila(("cantidad", cuotas.Sum(x => x.Cantidad)));
                seccion.Filas.Add(Fila(("fecha", null), ("cuota", ""), ("recibo", "TOTAL"),
                    ("descripcion", esCompra ? "Total recibido en la operación" : "Total entregado en la operación"),
                    ("cantidad", op.RecibidoDivisa), ("porrecibir", op.PorRecibir)));
            }

            informe.Secciones.Add(seccion);
        }
        informe.Notas.Add(new InformeNotaDto { Texto = $"Operaciones listadas: {ops.Count}." });
    }

    private async Task TransferenciasRangoAsync(InformeDto informe, DateTime desde, DateTime hasta, string? bancoId)
    {
        informe.Titulo = "TRANSFERENCIAS DEL PERÍODO";
        var q = _db.Transferencias.AsNoTracking()
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta && t.Estado == "ACTIVO");
        if (!string.IsNullOrWhiteSpace(bancoId)) q = q.Where(t => t.BancoId == bancoId);
        var tran = await q.OrderBy(t => t.BancoId).ThenBy(t => t.Fecha).ToListAsync();

        var bancos = await _db.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre);
        var nombres = await ClientesDiccionarioAsync(
            tran.Where(t => t.ClienteId != null).Select(t => t.ClienteId!).Distinct());

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "banco", Etiqueta = "Banco" },
            new() { Clave = "cuenta", Etiqueta = "Cuenta" },
            new() { Clave = "numero", Etiqueta = "Nro. documento" },
            new() { Clave = "beneficiario", Etiqueta = "Beneficiario" },
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "motivo", Etiqueta = "Motivo" },
            new() { Clave = "monto", Etiqueta = "Monto", Tipo = "moneda", Totalizable = true }
        };

        var filas = tran.Select(t => Fila(
                ("fecha", t.Fecha),
                ("banco", t.BancoId != null && bancos.TryGetValue(t.BancoId, out var b) ? b : null),
                ("cuenta", t.CuentaTran),
                ("numero", t.NumeroTran),
                ("beneficiario", t.Beneficiario),
                ("cliente", t.ClienteId != null && nombres.TryGetValue(t.ClienteId, out var n) ? n : null),
                ("motivo", t.Motivo),
                ("monto", t.MontoBs is > 0 ? t.MontoBs : t.Monto)))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin transferencias en el rango seleccionado." });
    }

    // =====================================================================
    // CLIENTES
    // =====================================================================
    private async Task SaldosDivisasAsync(InformeDto informe, string tipoOp, List<TipoMoneda> monedas)
    {
        var esCompra = tipoOp == "C";
        informe.Titulo = esCompra ? "SALDO POR RECIBIR EN DIVISAS" : "SALDO POR ENTREGAR EN DIVISAS";

        var ops = esCompra
            ? await _db.Compras.AsNoTracking()
                .Where(c => c.Estado == "ACTIVO" && c.PorRecibir > 0)
                .Select(c => new { c.ClienteId, c.MonedaDivisaId, c.Cantidad, c.MontoBs, c.SaldoBs, c.RecibidoDivisa, c.PorRecibir })
                .ToListAsync()
            : await _db.Ventas.AsNoTracking()
                .Where(v => v.Estado == "ACTIVO" && v.PorRecibir > 0)
                .Select(v => new { v.ClienteId, v.MonedaDivisaId, v.Cantidad, v.MontoBs, v.SaldoBs, v.RecibidoDivisa, v.PorRecibir })
                .ToListAsync();

        var nombres = await ClientesDiccionarioAsync(ops.Select(o => o.ClienteId).Distinct());
        var monedasDic = monedas.ToDictionary(m => m.Id, m => m.Descripcion);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "divisa", Etiqueta = "Divisa" },
            new() { Clave = "cantidad", Etiqueta = "Cantidad negociada", Tipo = "moneda", Totalizable = true },
            new() { Clave = "recibido", Etiqueta = esCompra ? "Recibida" : "Entregada", Tipo = "moneda", Totalizable = true },
            new() { Clave = "pendiente", Etiqueta = esCompra ? "Por recibir" : "Por entregar", Tipo = "moneda", Totalizable = true },
            new() { Clave = "monto", Etiqueta = "Monto Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldo", Etiqueta = "Saldo Bs", Tipo = "moneda", Totalizable = true }
        };

        var filas = ops
            .GroupBy(o => new { o.ClienteId, o.MonedaDivisaId })
            .OrderBy(g => nombres.GetValueOrDefault(g.Key.ClienteId, g.Key.ClienteId))
            .ThenBy(g => g.Key.MonedaDivisaId)
            .Select(g => Fila(
                ("cliente", nombres.GetValueOrDefault(g.Key.ClienteId, g.Key.ClienteId)),
                ("divisa", monedasDic.GetValueOrDefault(g.Key.MonedaDivisaId, g.Key.MonedaDivisaId)),
                ("cantidad", g.Sum(x => x.Cantidad)),
                ("recibido", g.Sum(x => x.RecibidoDivisa)),
                ("pendiente", g.Sum(x => x.PorRecibir)),
                ("monto", g.Sum(x => x.MontoBs)),
                ("saldo", g.Sum(x => x.SaldoBs))))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        informe.Notas.Add(new InformeNotaDto { Texto = "Solo se listan clientes con saldo de divisa pendiente." });
    }

    private async Task CuentaClienteAsync(InformeDto informe, InformeParametros p, DateTime desde, DateTime hasta)
    {
        if (string.IsNullOrWhiteSpace(p.ClienteId))
            throw new ValidacionException("Debe indicar el cliente.");
        var dto = await _reportes.EstadoCuentaAsync(p.ClienteId,
            p.Desde != null ? desde : null, p.Hasta != null ? hasta : null);
        informe.Titulo = "CUENTA DEL CLIENTE";
        informe.Subtitulo = dto.Cliente;

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "tipo", Etiqueta = "Tipo" },
            new() { Clave = "origen", Etiqueta = "Origen" },
            new() { Clave = "descripcion", Etiqueta = "Descripción" },
            new() { Clave = "referencia", Etiqueta = "Referencia" },
            new() { Clave = "sentido", Etiqueta = "Sentido" },
            new() { Clave = "monto", Etiqueta = "Monto Bs", Tipo = "moneda", Totalizable = true },
            new() { Clave = "acumulado", Etiqueta = "Acumulado Bs", Tipo = "moneda" }
        };

        var filas = dto.Lineas.Select(l => Fila(
                ("fecha", l.Fecha),
                ("tipo", l.Tipo),
                ("origen", l.Origen),
                ("descripcion", l.Descripcion),
                ("referencia", l.Referencia),
                ("sentido", l.Sentido),
                ("monto", l.MontoBs),
                ("acumulado", l.AcumuladoBs)))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        informe.Notas.Add(new InformeNotaDto { Texto = $"Tasa de referencia usada: {dto.Tasa:0.####} Bs." });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos en el rango seleccionado." });
    }

    private async Task FichaClienteAsync(InformeDto informe, string? clienteId)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
            throw new ValidacionException("Debe indicar el cliente.");
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId)
            ?? throw new RecursoNoEncontradoException($"No existe el cliente {clienteId}.");
        informe.Titulo = "FICHA DEL CLIENTE";
        informe.Subtitulo = cliente.NombreCompleto;

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "campo", Etiqueta = "Campo" },
            new() { Clave = "valor", Etiqueta = "Valor" }
        };
        var datos = new List<Dictionary<string, object?>>
        {
            Fila(("campo", "Código"), ("valor", cliente.Id)),
            Fila(("campo", "Tipo de persona"), ("valor", cliente.Persona == 2 ? "Jurídica" : "Natural")),
            Fila(("campo", "Razón social"), ("valor", cliente.RazonSocial)),
            Fila(("campo", "Nombres"), ("valor", cliente.Nombres)),
            Fila(("campo", "Apellidos"), ("valor", cliente.Apellidos)),
            Fila(("campo", "RIF"), ("valor", cliente.Rif)),
            Fila(("campo", "Cédula"), ("valor", cliente.Cedula)),
            Fila(("campo", "Dirección"), ("valor", cliente.Direccion)),
            Fila(("campo", "Teléfonos"), ("valor", cliente.Telefonos)),
            Fila(("campo", "Correo"), ("valor", cliente.Email)),
            Fila(("campo", "Observaciones"), ("valor", cliente.Observaciones)),
            Fila(("campo", "Estado"), ("valor", cliente.Activo ? "ACTIVO" : "INACTIVO"))
        };
        informe.Secciones.Add(new InformeSeccionDto { Nombre = "DATOS DEL CLIENTE", Columnas = columnas, Filas = datos });

        var cuentas = await _db.CuentasCliente.AsNoTracking()
            .Where(c => c.ClienteId == clienteId && c.Activa)
            .ToListAsync();
        var bancos = await _db.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre);
        var colCtas = new List<InformeColumnaDto>
        {
            new() { Clave = "banco", Etiqueta = "Banco" },
            new() { Clave = "cuenta", Etiqueta = "Nº de cuenta" }
        };
        var filasCtas = cuentas
            .Select(c => Fila(("banco", bancos.GetValueOrDefault(c.BancoId, c.BancoId)), ("cuenta", c.NumeroCuenta)))
            .ToList();
        informe.Secciones.Add(new InformeSeccionDto { Nombre = "CUENTAS BANCARIAS", Columnas = colCtas, Filas = filasCtas });

        var centros = await _db.CentroCostos.AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .ToListAsync();
        if (centros.Count > 0)
        {
            var colCc = new List<InformeColumnaDto> { new() { Clave = "centro", Etiqueta = "Centro de costo" } };
            informe.Secciones.Add(new InformeSeccionDto
            {
                Nombre = "CENTROS DE COSTO",
                Columnas = colCc,
                Filas = centros.Select(c => Fila(("centro", c.Principal ? $"{c.Descripcion} (principal)" : c.Descripcion))).ToList()
            });
        }
    }

    private async Task ListaClientesAsync(InformeDto informe, string? texto)
    {
        informe.Titulo = "LISTA DE CLIENTES";
        var q = _db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim().ToLower();
            q = q.Where(c => (c.RazonSocial ?? "").ToLower().Contains(t)
                          || (c.Nombres ?? "").ToLower().Contains(t)
                          || (c.Apellidos ?? "").ToLower().Contains(t)
                          || (c.Rif ?? "").ToLower().Contains(t)
                          || (c.Cedula ?? "").ToLower().Contains(t));
        }
        var clientes = await q.OrderBy(c => c.RazonSocial).ThenBy(c => c.Apellidos).ToListAsync();

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "codigo", Etiqueta = "Cód." },
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "rif", Etiqueta = "RIF" },
            new() { Clave = "cedula", Etiqueta = "Cédula" },
            new() { Clave = "telefonos", Etiqueta = "Teléfonos" },
            new() { Clave = "email", Etiqueta = "Correo" },
            new() { Clave = "direccion", Etiqueta = "Dirección" }
        };
        var filas = clientes
            .Select(c => Fila(
                ("codigo", c.Id),
                ("cliente", c.NombreCompleto),
                ("rif", c.Rif),
                ("cedula", c.Cedula),
                ("telefonos", c.Telefonos),
                ("email", c.Email),
                ("direccion", c.Direccion)))
            .ToList();
        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas });
        informe.Notas.Add(new InformeNotaDto { Texto = $"Total de clientes listados: {filas.Count}." });
    }

    // =====================================================================
    // BANCOS
    // =====================================================================
    private async Task ExtractoBancoAsync(InformeDto informe, InformeParametros p, DateTime desde, DateTime hasta)
    {
        if (string.IsNullOrWhiteSpace(p.BancoId))
            throw new ValidacionException("Debe indicar el banco.");
        var banco = await _db.Bancos.AsNoTracking().FirstOrDefaultAsync(b => b.Id == p.BancoId)
            ?? throw new RecursoNoEncontradoException($"No existe el banco {p.BancoId}.");
        informe.Titulo = "EXTRACTO BANCARIO";
        informe.Subtitulo = banco.Nombre;

        // Libro de transferencias del banco (VFP: CTASTRAN / detasaldo).
        var q = _db.Transferencias.AsNoTracking().Where(t => t.BancoId == banco.Id && t.Estado == "ACTIVO");
        if (!string.IsNullOrWhiteSpace(p.Cuenta)) q = q.Where(t => t.CuentaTran == p.Cuenta);
        var movs = await q.OrderBy(t => t.Fecha).ThenBy(t => t.Id).ToListAsync();

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "cuenta", Etiqueta = "Cuenta" },
            new() { Clave = "numero", Etiqueta = "Nro. documento" },
            new() { Clave = "beneficiario", Etiqueta = "A nombre de" },
            new() { Clave = "motivo", Etiqueta = "Descripción" },
            new() { Clave = "credito", Etiqueta = "Crédito", Tipo = "moneda", Totalizable = true },
            new() { Clave = "debito", Etiqueta = "Débito", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldo", Etiqueta = "Saldo", Tipo = "moneda" }
        };

        var grupos = movs.GroupBy(t => t.CuentaTran ?? "(sin cuenta)").OrderBy(g => g.Key).ToList();
        foreach (var grupo in grupos)
        {
            var saldoAnterior = grupo.Where(t => t.Fecha < desde).Sum(MontoNeto);
            var enRango = grupo.Where(t => t.Fecha >= desde && t.Fecha <= hasta).ToList();

            var filas = new List<Dictionary<string, object?>>
            {
                Fila(("fecha", null), ("cuenta", null), ("numero", "SALDO ANTERIOR"),
                    ("beneficiario", null), ("motivo", $"Saldo al {desde:dd/MM/yyyy}"),
                    ("credito", null), ("debito", null), ("saldo", saldoAnterior))
            };
            var acumulado = saldoAnterior;
            foreach (var m in enRango)
            {
                var monto = MontoBs(m);
                var credito = EsDebito(m) ? (decimal?)null : monto;
                var debito = EsDebito(m) ? monto : (decimal?)null;
                acumulado += (credito ?? 0) - (debito ?? 0);
                filas.Add(Fila(
                    ("fecha", m.Fecha),
                    ("cuenta", m.CuentaTran),
                    ("numero", m.NumeroTran),
                    ("beneficiario", m.Beneficiario),
                    ("motivo", m.Motivo),
                    ("credito", credito),
                    ("debito", debito),
                    ("saldo", acumulado)));
            }
            var totalCredito = enRango.Where(t => !EsDebito(t)).Sum(MontoBs);
            var totalDebito = enRango.Where(EsDebito).Sum(MontoBs);
            filas.Add(Fila(("fecha", null), ("cuenta", null), ("numero", "SALDO FINAL"),
                ("beneficiario", null), ("motivo", $"Saldo al {hasta:dd/MM/yyyy}"),
                ("credito", totalCredito), ("debito", totalDebito), ("saldo", acumulado)));

            informe.Secciones.Add(new InformeSeccionDto
            {
                Nombre = grupos.Count > 1 ? $"Cuenta {grupo.Key}" : null,
                Columnas = columnas,
                Filas = filas,
                Totales = Fila(("credito", totalCredito), ("debito", totalDebito))
            });
        }

        informe.Notas.Add(new InformeNotaDto { Texto = $"Saldo registrado del banco: {banco.Saldo:0.00} Bs." });
        if (informe.Secciones.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos bancarios registrados." });
        else
            informe.Notas.Add(new InformeNotaDto { Texto = "Saldo anterior calculado sobre el histórico completo de movimientos del banco." });
    }

    private async Task MovimientosBancoAsync(InformeDto informe, InformeParametros p, DateTime desde, DateTime hasta)
    {
        informe.Titulo = "MOVIMIENTOS BANCARIOS";
        var filtro = (p.Tipo ?? "todos").Trim().ToLowerInvariant();

        var q = _db.Transferencias.AsNoTracking()
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta && t.Estado == "ACTIVO");
        if (!string.IsNullOrWhiteSpace(p.BancoId)) q = q.Where(t => t.BancoId == p.BancoId);
        var movs = await q.OrderBy(t => t.BancoId).ThenBy(t => t.Fecha).ToListAsync();

        var bancos = await _db.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "banco", Etiqueta = "Banco" },
            new() { Clave = "cuenta", Etiqueta = "Cuenta" },
            new() { Clave = "numero", Etiqueta = "Nro. documento" },
            new() { Clave = "beneficiario", Etiqueta = "Beneficiario" },
            new() { Clave = "motivo", Etiqueta = "Motivo" },
            new() { Clave = "credito", Etiqueta = "Crédito", Tipo = "moneda", Totalizable = true },
            new() { Clave = "debito", Etiqueta = "Débito", Tipo = "moneda", Totalizable = true }
        };

        var filas = movs
            .Where(t => filtro == "todos" || (filtro == "debito") == EsDebito(t))
            .Select(t =>
            {
                var monto = MontoBs(t);
                var esDebito = EsDebito(t);
                return Fila(
                    ("fecha", t.Fecha),
                    ("banco", t.BancoId != null && bancos.TryGetValue(t.BancoId, out var b) ? b : null),
                    ("cuenta", t.CuentaTran),
                    ("numero", t.NumeroTran),
                    ("beneficiario", t.Beneficiario),
                    ("motivo", t.Motivo),
                    ("credito", esDebito ? null : monto),
                    ("debito", esDebito ? monto : null));
            })
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos en el rango seleccionado." });
    }

    // =====================================================================
    // CUSTODIOS
    // =====================================================================
    private async Task CustodiosClienteAsync(InformeDto informe, DateTime desde, DateTime hasta,
        string? clienteId, List<TipoMoneda> monedas)
    {
        informe.Titulo = "INFORME DE CUSTODIOS";
        var q = _db.Compensaciones.AsNoTracking().Where(c => c.Fecha >= desde && c.Fecha <= hasta);
        if (!string.IsNullOrWhiteSpace(clienteId)) q = q.Where(c => c.ClienteId == clienteId);
        var comps = await q.OrderBy(c => c.ClienteId).ThenBy(c => c.Fecha).ToListAsync();

        var nombres = await ClientesDiccionarioAsync(comps.Select(c => c.ClienteId).Distinct());
        var monedasDic = monedas.ToDictionary(m => m.Id, m => m.Descripcion);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "descripcion", Etiqueta = "Descripción" },
            new() { Clave = "moneda", Etiqueta = "Moneda" },
            new() { Clave = "deposito", Etiqueta = "Depósito", Tipo = "moneda", Totalizable = true },
            new() { Clave = "retiro", Etiqueta = "Retiro", Tipo = "moneda", Totalizable = true }
        };

        foreach (var grupo in comps.GroupBy(c => c.ClienteId).OrderBy(g => nombres.GetValueOrDefault(g.Key, g.Key)))
        {
            var filas = grupo
                .Select(c => Fila(
                    ("fecha", c.Fecha),
                    ("recibo", c.ReciboTran),
                    ("descripcion", c.Descripcion),
                    ("moneda", monedasDic.GetValueOrDefault(c.MonedaId, c.MonedaId)),
                    ("deposito", c.Abonos > 0 ? c.Abonos : null),
                    ("retiro", c.Retiros > 0 ? c.Retiros : null)))
                .ToList();
            informe.Secciones.Add(new InformeSeccionDto
            {
                Nombre = nombres.GetValueOrDefault(grupo.Key, grupo.Key),
                Columnas = columnas,
                Filas = filas,
                Totales = Totales(columnas, filas)
            });
        }
        if (informe.Secciones.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos de custodia en el rango seleccionado." });
        else
            informe.Notas.Add(new InformeNotaDto { Texto = "La disponibilidad actual por cliente se muestra en «Custodios — resumen por cliente»." });
    }

    private async Task CustodiosTodosAsync(InformeDto informe, List<TipoMoneda> monedas)
    {
        informe.Titulo = "CUSTODIOS — RESUMEN POR CLIENTE";
        var comps = await _db.Compensaciones.AsNoTracking()
            .Select(c => new { c.ClienteId, c.MonedaId, c.Abonos, c.Retiros })
            .ToListAsync();
        var disponibles = await _db.Custodios.AsNoTracking()
            .Select(c => new { c.ClienteId, c.MonedaId, c.Disponible, c.DisponibleBs })
            .ToListAsync();

        var nombres = await ClientesDiccionarioAsync(
            comps.Select(c => c.ClienteId).Union(disponibles.Select(d => d.ClienteId)));
        var monedasDic = monedas.ToDictionary(m => m.Id, m => m.Descripcion);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "moneda", Etiqueta = "Moneda" },
            new() { Clave = "depositos", Etiqueta = "Depósitos", Tipo = "moneda", Totalizable = true },
            new() { Clave = "retiros", Etiqueta = "Retiros", Tipo = "moneda", Totalizable = true },
            new() { Clave = "saldo", Etiqueta = "Saldo custodia", Tipo = "moneda", Totalizable = true },
            new() { Clave = "disponible", Etiqueta = "Disponible actual", Tipo = "moneda", Totalizable = true },
            new() { Clave = "disponibleBs", Etiqueta = "Disponible Bs", Tipo = "moneda", Totalizable = true }
        };

        var filas = comps.GroupBy(c => new { c.ClienteId, c.MonedaId })
            .Select(g =>
            {
                var disp = disponibles.FirstOrDefault(d => d.ClienteId == g.Key.ClienteId && d.MonedaId == g.Key.MonedaId);
                return Fila(
                    ("cliente", nombres.GetValueOrDefault(g.Key.ClienteId, g.Key.ClienteId)),
                    ("moneda", monedasDic.GetValueOrDefault(g.Key.MonedaId, g.Key.MonedaId)),
                    ("depositos", g.Sum(x => x.Abonos)),
                    ("retiros", g.Sum(x => x.Retiros)),
                    ("saldo", g.Sum(x => x.Abonos - x.Retiros)),
                    ("disponible", disp?.Disponible ?? 0m),
                    ("disponibleBs", disp?.DisponibleBs ?? 0m));
            })
            .Where(f => Saldo(f) != 0m || Disponible(f) != 0m)
            .OrderBy(f => (string?)f["cliente"])
            .ThenBy(f => (string?)f["moneda"])
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin saldos de custodia registrados." });

        static decimal Saldo(Dictionary<string, object?> f) => f["saldo"] is decimal d ? d : 0m;
        static decimal Disponible(Dictionary<string, object?> f) => f["disponible"] is decimal d ? d : 0m;
    }

    private async Task CustodiosBancoAsync(InformeDto informe, DateTime desde, DateTime hasta, List<TipoMoneda> monedas)
    {
        informe.Titulo = "CUSTODIOS VÍA BANCO";
        var comps = await _db.Compensaciones.AsNoTracking()
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta && (c.ReciboTran != null || c.CtaTran != null))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        var nombres = await ClientesDiccionarioAsync(comps.Select(c => c.ClienteId).Distinct());
        var monedasDic = monedas.ToDictionary(m => m.Id, m => m.Descripcion);

        var columnas = new List<InformeColumnaDto>
        {
            new() { Clave = "fecha", Etiqueta = "Fecha", Tipo = "fecha" },
            new() { Clave = "cliente", Etiqueta = "Cliente" },
            new() { Clave = "recibo", Etiqueta = "Recibo" },
            new() { Clave = "cuenta", Etiqueta = "Cuenta" },
            new() { Clave = "descripcion", Etiqueta = "Descripción" },
            new() { Clave = "moneda", Etiqueta = "Moneda" },
            new() { Clave = "deposito", Etiqueta = "Depósito", Tipo = "moneda", Totalizable = true },
            new() { Clave = "retiro", Etiqueta = "Retiro", Tipo = "moneda", Totalizable = true }
        };
        var filas = comps
            .Select(c => Fila(
                ("fecha", c.Fecha),
                ("cliente", nombres.GetValueOrDefault(c.ClienteId, c.ClienteId)),
                ("recibo", c.ReciboTran),
                ("cuenta", c.CtaTran),
                ("descripcion", c.Descripcion),
                ("moneda", monedasDic.GetValueOrDefault(c.MonedaId, c.MonedaId)),
                ("deposito", c.Abonos > 0 ? c.Abonos : null),
                ("retiro", c.Retiros > 0 ? c.Retiros : null)))
            .ToList();

        informe.Secciones.Add(new InformeSeccionDto { Columnas = columnas, Filas = filas, Totales = Totales(columnas, filas) });
        if (filas.Count == 0)
            informe.Notas.Add(new InformeNotaDto { Texto = "Sin movimientos de custodia vía banco en el rango." });
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
    private static (DateTime desde, DateTime hasta) Rango(InformeParametros p)
    {
        var hasta = (p.Hasta ?? DateTime.Today).Date;
        var desde = (p.Desde ?? hasta.AddDays(-30)).Date;
        if (desde > hasta)
            throw new ValidacionException("La fecha «Desde» no puede ser posterior a «Hasta».");
        return (desde, hasta);
    }

    private async Task<(Configuracion config, List<TipoMoneda> monedas)> CargarBaseAsync()
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync() ?? new Configuracion();
        var monedas = await _db.TipoMonedas.AsNoTracking()
            .Where(m => m.Activa)
            .OrderBy(m => m.Id)
            .ToListAsync();
        if (monedas.Count == 0)
            monedas = [new TipoMoneda { Id = config.MonedaNacionalId, Descripcion = "Bs", Nacional = true }];
        return (config, monedas);
    }

    private async Task<Dictionary<string, string>> ClientesDiccionarioAsync(IEnumerable<string> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<string, string>();
        var clientes = await _db.Clientes.AsNoTracking()
            .Where(c => lista.Contains(c.Id))
            .Select(c => new { c.Id, c.RazonSocial, c.Nombres, c.Apellidos })
            .ToListAsync();
        return clientes.ToDictionary(
            c => c.Id,
            c => !string.IsNullOrWhiteSpace(c.RazonSocial)
                ? c.RazonSocial
                : $"{c.Nombres} {c.Apellidos}".Trim());
    }

    private static Dictionary<string, object?> Fila(params (string clave, object? valor)[] campos)
    {
        var fila = new Dictionary<string, object?>();
        foreach (var (clave, valor) in campos) fila[clave] = valor;
        return fila;
    }

    private static Dictionary<string, object?>? Totales(List<InformeColumnaDto> columnas,
        List<Dictionary<string, object?>> filas)
    {
        var resultado = new Dictionary<string, object?>();
        foreach (var col in columnas.Where(c => c.Totalizable))
        {
            decimal suma = 0;
            foreach (var fila in filas)
                if (fila.TryGetValue(col.Clave, out var v) && v is decimal d)
                    suma += d;
            resultado[col.Clave] = suma;
        }
        return resultado.Count > 0 ? resultado : null;
    }

    private static decimal MontoBs(Transferencia t) => t.MontoBs is > 0 ? t.MontoBs.Value : t.Monto;

    /// <summary>Neto de un movimiento bancario: entra (+) o sale (−).</summary>
    private static decimal MontoNeto(Transferencia t) => EsDebito(t) ? -MontoBs(t) : MontoBs(t);

    private static bool EsDebito(Transferencia t) => t.TipoTran == "DB";
}
