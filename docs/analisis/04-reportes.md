# 04 — Inventario y análisis de reportes (.FRX/.FRT)

> Reingeniería inversa del sistema **Monrroy / Mundodolar** (casa de cambio / pagos), Visual FoxPro 6, cp1252.
> Carpeta analizada: `C:\Mundodolar\Reportes\` (~52 archivos `.FRX`, todos con su `.FRT`).
> Destino de la migración: **Angular + .NET + PostgreSQL**.

## Cómo están construidos estos reportes (hallazgo metodológico clave)

Antes del inventario, la mecánica interna condiciona toda la migración:

- Los `.FRX` son **tablas DBF de diseño** (bandas, expresiones, picture de formato). **No contienen SQL embebido.** El entorno de datos no guarda `SELECT`; los reportes leen de **cursores/alias** pre-armados por el formulario llamador.
- El flujo real es: **formulario generador → CREATE TABLE temporal en `C:\Mundodolar\temp\` → bucle `DO WHILE` que recorre tablas fuente (`caja`, `compras`, `ventas`, `clientes`, `compensac`, `ctastran`…) y hace `APPEND/REPLACE` sobre el cursor → `REPORT FORM`**.
- El SQL equivalente a la migración vive en esa lógica de formularios (`operaciones.SCT`, `cajareport.SCT`, `custodios.SCT`, `movimientos.SCT`, `funcion1.prg`), no en el reporte.
- Los reportes se invocan casi siempre en **doble paso**: `REPORT FORM &x_ruta PREVIEW NOCONSOLE` (vista previa) y `REPORT FORM &x_ruta TO PRINTER PROMPT NOCONSOLE` (impresión), con exportación opcional a PDF (`a_pdf`, `expopdf`).
- Parámetros públicos frecuentes: `p_fecha1`/`p_fecha2` (rango DESDE/HASTA), `p_fecha3` (fecha de corte), `pp_id_clie`/`p_id_clie` (cliente puntual), `pp_titulo` (título dinámico), `pp_clie` (nombre del cliente en encabezado).
- Las cifras monetarias usan `PICTURE "999,999,999.99"` (miles con coma, 2 decimales) — formato a conservar en la UI.
- Los montos suelen venir **ya convertidos a Bolívares (Bs)** en el cursor; el desglose USD/Bs se deriva dividiendo por la tasa (`saldo_bs / (monto_bs / cantidad)`), patrón repetido en `estadogeneral`.

> Traducción conceptual: casi todos los "reportes" son en realidad **vistas de consulta (informes gerenciales/estado de cuenta)** generadas por agregación procedural. Solo unos cuantos son **documentos transaccionales** que se imprimen al registrar un movimiento (recibos, comprobantes de compra/venta, entregas/recepciones de custodio).

### Cursores (tablas temporales) identificados y su origen

| Cursor temporal | Tablas fuente recorridas | Alimenta a |
|---|---|---|
| `cajareport` | `caja`, `rptcaja`, `clientes`, `centrosclie` | ingresos_caja, ingresos_caja_cs |
| `cuadrebs` / `cuadredv` | movimientos de `caja` (entradas/salidas por fecha) | cuadrebs, cuadredv |
| `estadogen` | `compras`, `ventas`, `compensac`, `clientes` | estadogeneral, estadogeneral_new |
| `opercomp` / `opervent` | `compras`/`ventas`, `compagar`/`cobrar`, `clientes` | opercomp, opervent |
| `detacomp` / `detavent` | `compras`/`ventas`, `compagar`/`cobrar`, `bancos`, `tipomony` | detacomp, detavent, detarecibido, detaentrgad, detallecompras, detalleventas |
| `detaclie` / `detaclie2` | `compras`/`ventas`, pagos, `clientes` | detallecompras_clie(_2), DETALLEVENTAS_CLIE(_2) |
| `detasaldo` | `ctastran` (movimientos bancarios), `bancos` | movimientoscr, movimientosdb, saldog1, genelal_1 |
| `tcustodio` | `compensac` (abonos/retiros custodio), `tipomony` | ver_custodios*, entregacustodio*, recibecustodio* |
| `listacle` | `clientes` | listaclie |
| `compras_temp`/`ventas_temp`, `*recipagos`, `compagar_temp`/`venpagar_temp`, `comprecib_temp`/`venrecib_temp` | `compras`, `ventas`, pagos | COMPRA, VENTA, PAGOS, cobros, recepcion, entregas |

### Códigos `caja.origen` (clasificador de movimientos de caja)

`IN`=ingreso, `VE`=venta, `CO`=compra, `IB`=ingreso a banco, `EB`=egreso a banco, `EG`=egreso, `DE`/`RE`=depósito/reposición de custodio, `EN`=entrega, `C1`,`T6`=otros. El reporte `ingresos_caja` se reutiliza para **Ingresos** (`pp_titulo="INGRESOS CAJA DIVISA NACIONAL"`) y **Egresos** (`"EGRESOS CAJA"`) filtrando por estos códigos.

---

## Tabla maestra de reportes

| Reporte | Propósito (lenguaje natural) | Cursores/tablas | Tipo |
|---|---|---|---|
| **recibo** | Recibo de caja (comprobante simple de un movimiento) | `caja` | **Transaccional** |
| **COMPRA** | Comprobante de registro de una compra de divisa | `compras_temp` | **Transaccional** |
| **VENTA** | Comprobante de registro de una venta de divisa | `ventas_temp` | **Transaccional** |
| **ingresocaja** | Comprobante de ingreso a caja (divisa recibida) | `caja`, `cajareposicion`, `tipomony` | **Transaccional** |
| **ingresobanco / ingresobanco2 / ingresobanco22 / ingresobanco_02** | Comprobante de ingreso/depósito a cuenta bancaria | `caja`/`ctastran`, `ctas_temp`, `bancoscredi(_2)` | **Transaccional** |
| **entregacustodio / entregacustodiob** | Comprobante de entrega de custodio (a caja / a banco) | `caja`/`ctastran`, `custodios(_b)`, `tipomony` | **Transaccional** |
| **recibecustodio / recibecustodiob** | Comprobante de recepción de custodio (a caja / a banco) | `caja`/`ctastran`, `custodios(_b)`, `tipomony` | **Transaccional** |
| **estadogeneral / estadogeneral_16122024 / estadogeneral_new** | Estado General por cliente (COMPRAS/VENTAS/CUSTODIO/CONSOLIDADO) | `estadogen` (← compras, ventas, compensac, clientes) | Informe gerencial |
| **consolidado** | Consolidado general de la empresa (totales en Bs y USD) | cursor propio + `dolarmain` (tasas) | Informe gerencial |
| **cuadrebs** | Cuadre de caja — divisa nacional (Bs) | `cuadrebs` (movimientos caja) | Informe de consulta |
| **cuadredv** | Cuadre de caja — divisas (extranjeras) | `cuadredv` | Informe de consulta |
| **ingresos_caja / ingresos_caja_cs** | Ingresos y egresos de caja por rango (con/sin centro de costo y saldo) | `cajareport` | Informe de consulta |
| **movimientoscr** | Movimientos bancarios — Créditos por banco/cuenta | `detasaldo` (← ctastran, bancos) | Informe de consulta |
| **movimientosdb** | Movimientos bancarios — Débitos por banco/cuenta | `detasaldo` | Informe de consulta |
| **saldog1** | Estado de cuenta bancario (créditos, débitos, saldo) por banco/cuenta | `detasaldo` | Informe de consulta |
| **genelal_1** | General de saldos bancarios (cierre por cuenta) | `detasaldo` | Informe de consulta |
| **opercomp** | Operaciones de compra: monto/pagado/saldo/pendiente por cliente | `opercomp` | Informe de consulta |
| **opervent** | Operaciones de venta: monto/cobrado/saldo/pendiente por cliente | `opervent` | Informe de consulta |
| **detallecompras / detallecompras_obs** | Detalle transaccional de compras (con observaciones) | `detacomp` | Informe de consulta |
| **detalleventas / detalleventas_obs** | Detalle transaccional de ventas (con observaciones) | `detavent` | Informe de consulta |
| **detallecompras_clie / detallecompras_clie_2** | Estado de cuenta detallado de compras por cliente | `detaclie` / `detaclie2` | Informe de consulta |
| **DETALLEVENTAS_CLIE / detalleventas_clie_2** | Estado de cuenta detallado de ventas por cliente | `detaclie` / `detaclie2` | Informe de consulta |
| **detacomp / detavent** | Detalle de operaciones de compra/venta (caja+banco) | `detacomp` / `detavent` | Informe de consulta |
| **detarecibido** | Detalle de divisas recibidas por compra (caja+custodio+banco) | `detarecibido` | Informe de consulta |
| **detaentrgad** | Detalle de divisas entregadas por venta (caja+custodio+banco) | `detaentrgad` | Informe de consulta |
| **recepcion** | Informe de recepción de divisas sobre compras (recibida/por recibir) | `comprecib_temp`, `compras_temp` | Informe de consulta |
| **entregas** | Informe de entregas de divisas sobre ventas (entregado/por entregar) | `venrecib_temp`, `ventas_temp` | Informe de consulta |
| **cobros** | Informe/estado de cuenta de cobros sobre ventas | `venpagar_temp`, `ventas` | Informe de consulta |
| **PAGOS** | Estado de cuenta de pagos sobre compras | `compagar_temp`, `compras` | Informe de consulta |
| **ver_custodios / ver_custodiosb** | Informe de custodios por divisa (caja / bancos): depósitos vs retiros | `tcustodio`, `custodios(_b)` | Informe de consulta |
| **ver_custodiosclie / _2 / _3** | Informe de custodio de un cliente (con saldo inicial, medio, centro costo) | `tcustodio` | Informe de consulta |
| **ver_custodiostodos / _2 / _3** | Informe de custodios de todos los clientes (consolidado por cliente) | `tcustodio` | Informe de consulta |
| **ver_custodios_02** | Detalle de custodios (depósitos/retiros) por cliente/divisa | `tcustodio` | Informe de consulta |
| **listaclie** | Listado maestro de clientes (natural/jurídica, con totales) | `listacle` (← clientes) | Maestro/consulta |
| **fichaclie** | Ficha individual de datos del cliente | variables de cliente | Maestro/consulta |
| **caso_01** | Reporte de movimientos generales ("caso 01": fecha, money, banco, ref) — auxiliar contable | cursor `caso_01` | Informe de consulta |

> Archivos extra fuera del listado pedido, inventariados igualmente: `estadogeneral_16122024.frx` (copia fechada de estadogeneral), `ver_custodiosb`, `ver_custodiostodos_2/3`, `ver_custodios_02`. No se encontró `consolidado` duplicado; `genelal_1` es el "general" de saldos (typo original conservado).

---

## Secciones detalladas de los reportes más importantes

### 1. estadogeneral / estadogeneral_new — Estado General (informe gerencial principal)
- **Propósito:** Una fila por cliente con la situación consolidada: COMPRAS (USD por recibir, Bs por pagar, USD por pagar), VENTAS (USD por entregar, Bs por cobrar, USD por cobrar), CUSTODIO (saldo) y CONSOLIDADO por cliente. Es el tablero de posiciones de la casa.
- **Cursores:** `estadogen` con columnas `nro_clie, id_clie, cliente, por_recib, porpag_bs, por_pagusd, por_entre, porcob_bs, por_cobusd, cantidadc, cantidadv, custoabono, custoreti, saldocust, consolidad`.
- **Fuentes/lógica:** recorre `compras` (por `id_clie`, descartando `fecha > p_fecha3`), `ventas` y `compensac` (custodio: abonos/retiros). Suma `porpag_bs += saldo_bs` (compras) y `porcob_bs += saldo_bs` (ventas); el equivalente USD se calcula `saldo_bs / (monto_bs / cantidad)`.
- **Filtros:** fecha de corte `p_fecha3` (pide `criterioreport_edogen`: "Indique la fecha de corte"); cliente opcional `pp_id_clie`.
- **Agrupaciones/totales:** totales por columnas (`TOTALTYPE`), totales generales, `FECHA DE CORTE`.
- **Tipo:** informe gerencial de consulta (no se imprime al transaccionar). `estadogeneral_new` agrega columnas de cantidades (`cantidadc/cantidadv`) y `custoabono/custoreti`; `_16122024` es copia fechada.

### 2. consolidado — Consolidado general de la empresa
- **Propósito:** Totales globales con conversión de moneda: Total Compras, Total Pagado, CxP Compras, Total Divisas Compradas/Recibidas, en columnas "BS. * USD" y "USD. / Bs." usando una tasa (`f_tasa`, `f_text2..f_text6`).
- **Fuentes:** cursor propio + `dolarmain` (tasas/paridad). Muchas expresiones calculadas `f_text* / f_tasa` y `f_text* * f_tasa`.
- **Agrupaciones:** por `dolarmain.nmbclie` (cliente) con subtotales; suma de columnas convertidas.
- **Tipo:** informe gerencial de cierre. Mapea a un **endpoint de dashboard/consolidado** con parámetro de fecha y tasa.

### 3. cuadrebs / cuadredv — Cuadre de caja (Bs y divisas)
- **Propósito:** Conciliación de caja: por cada fecha, **Ingresos** (entradas) y **Egresos** (salidas) con Nro. de recibo y descripción, y **Saldo** resultante. `cuadrebs` es divisa nacional; `cuadredv` agrega columna `divisa` y totaliza por divisa.
- **Cursores:** `cuadrebs (tipo, recibo, descrip, fecha, entradas, salidas)` y `cuadredv (+ divisa)`, indexados por `DTOS(fecha)`. Se construyen (CREATE TABLE en `\temp\`) en `operaciones`; el poblamiento recorre movimientos de `caja` clasificados en entradas/salidas.
- **Filtros:** rango `p_fecha1`(DESDE)/`p_fecha2`(HASTA).
- **Agrupaciones/totales:** subtotal por fecha (`TOTALTYPE=2`) y **Total general** de entradas, salidas y saldo.
- **Tipo:** informe de consulta/cierre de caja. Requiere un endpoint de **cuadre por rango**.

### 4. recibo — Recibo de caja (transaccional)
- **Propósito:** Comprobante mínimo que se imprime al registrar un ingreso/egreso de caja: "RECIBOS DE CAJA", Descripción, Nro. de recibo, Monto, Fecha, y marca "ANULADO" si aplica.
- **Tablas:** `caja (descrip, recibo, monto, fecha)` (grupo por `caja.descrip`; `int(val(caja.recibo))` para el número).
- **Tipo:** **transaccional** (plantilla tipo recibo/ticket). Debe migrar a un **documento imprimible** ligado a la operación, con vista de "anulado".

### 5. COMPRA / VENTA — Comprobantes de transacción
- **Propósito:** Comprobante impreso al registrar una compra/venta: Cliente, Divisa comprada/vendida, Divisa de pago, Cantidad, **Tasa de compra/venta**, Monto transacción, Monto pagado/cobrado, Saldo, Divisas recibidas/entregadas, Observaciones.
- **Cursores:** `compras_temp` / `ventas_temp` (mismas columnas espejo: `cantidad, cliente, descri1, descri2, monto_bs, pagado_bs, porecib, saldo_bs, valonacio, observa, id_compra/id_venta`).
- **Tipo:** **transaccional** (dos comprobantes espejo; conviene unificar en una plantilla parametrizada por tipo operación).

### 6. ingresocaja / ingresobanco* — Comprobantes de ingreso
- **Propósito:** Comprobante de ingreso de efectivo a **caja** (con divisa recibida, cantidad, disponibilidad) o a **banco** (con banco, cuenta, Nro. documento). `ingresobanco2/22` son variantes para el segundo banco/cuenta (`bancoscredi2`).
- **Tablas:** `caja`, `ctastran`/`ctas_temp` (cuentas y Nro. documento), `tipomony` (descrip de divisa), `bancoscredi(_2)`.
- **Tipo:** **transaccional**.

### 7. ingresos_caja / ingresos_caja_cs — Informe de ingresos y egresos de caja
- **Propósito:** Listado por rango de fechas de todos los movimientos de caja con Fecha, Recibo, Descripción, Monto, Origen (descriptivo), Cliente y Centro de Costo. `_cs` agrega **Saldo actual** y observaciones.
- **Cursor:** `cajareport (ordenar, origencaja, fecha, recibo, descrip, observa, monto, cliente, centrocost[, saldo_act])`, poblado desde `caja` (filtrando `origen`) + `rptcaja` (descripción del origen) + `clientes`/`centrosclie`.
- **Filtros:** rango `p_fecha1/p_fecha2`; códigos de `origen` (ingresos: IN/VE/IB; egresos: CO/EB/EG); `pp_titulo` alterna entre "INGRESOS CAJA DIVISA NACIONAL" y "EGRESOS CAJA".
- **Agrupaciones:** por Origen (`origencaja`) con subtotal y Total general de montos.
- **Tipo:** informe de consulta. **Un solo diseño** sirve para ingresos y egresos ⇒ en la migración, un endpoint con filtro `tipo=ingreso|egreso`.

### 8. movimientoscr / movimientosdb / saldog1 / genelal_1 — Movimientos y saldos bancarios
- **Propósito:** por banco y cuenta: movimientos con Fecha, Descripción (`benetran`, `motitran`), **Créditos** (cr), **Débitos** (db) y **Saldo**. `movimientoscr` lista solo créditos, `movimientosdb` solo débitos; `saldog1` y `genelal_1` muestran ambos con saldo acumulado y saldo final.
- **Cursor:** `detasaldo (id_banco, banco, fechtran, numectas, benetran, motitran, creditos, debitos, saldo, saldofin, inisaldo)` desde `ctastran` (movimientos) + `bancos`.
- **Filtros:** banco/cuenta (`versaldos1.id_banco`), rango de fechas (form `movimcriterios`).
- **Agrupaciones:** total de créditos/débitos por cuenta (`TOTALTYPE`) y total general.
- **Tipo:** informe de consulta (estado de cuenta bancario). `genelal_1` es el de cierre ("General de saldos").

### 9. opercomp / opervent — Operaciones de compra/venta por cliente
- **Propósito:** por operación (compra/venta Nro.), Cliente, Tasa, Fecha, **Monto**, **Pagado/Cobrado**, **Saldo**, **Pendiente**, Compromiso, Recibido.
- **Cursores:** `opercomp` / `opervent` (espejos) desde `compras`/`ventas` + `compagar`/cobrar + `clientes`.
- **Filtros:** rango `p_fecha1/p_fecha2`, cliente.
- **Agrupaciones:** totales de Monto, Pagado/Cobrado y Saldo (subtotal y total general).
- **Tipo:** informe de consulta (aging de cartera).

### 10. entregas / recepcion — Entrega y recepción de divisas
- **Propósito:** sobre una venta/compra Nro. muestra la **Cantidad comprometida**, lo **Entregado/Recibido**, lo **Por entregar/por recibir**, con detalle de Nro.Op, Fecha, Recibo y Tipo Op.
- **Cursores:** `venrecib_temp`/`comprecib_temp` (pagos/recepciones) + `ventas_temp`/`compras_temp`.
- **Agrupaciones:** total entregado/recibido; cantidad comprometida.
- **Tipo:** informe de seguimiento de cumplimiento de la operación.

### 11. ver_custodios* / entregacustodio* / recibecustodio* — Custodios
- **Propósito:** control de **custodios** (guarda de divisas por cliente/divisa): por fecha, **Depósitos** (`entrega`/abonos) y **Retiros** (`retira`), con recibo, descripción, **Disponibilidad general** y saldo. Variantes: por caja (`ver_custodios`), por banco (`ver_custodiosb`), de un cliente (`ver_custodiosclie*`, con saldo inicial y centro de costo), de todos los clientes (`ver_custodiostodos*`). Los comprobantes `entregacustodio/recibecustodio(_b)` imprimen el movimiento puntual.
- **Cursor:** `tcustodio (id_clie, cliente, fecha, descrip, divisa, entrega, retira, recibo, saldo, saldoini, medio, centrocost, observa)` desde `compensac` (abonos/retiros) + `tipomony`.
- **Filtros:** cliente, divisa (rechaza divisa nacional), rango `p_fecha1/p_fecha2`.
- **Agrupaciones:** total depósitos/retiros por divisa y por cliente; `entrega - retira` = saldo.
- **Tipo:** `ver_*` consulta; `entregacustodio/recibecustodio*` **transaccional**.

### 12. listaclie / fichaclie — Maestros de clientes
- **Propósito:** `listaclie` listado completo (Descripción/Razón Social, RIF, Teléfonos, Correo) con **Total persona natural / jurídica / total clientes**; `fichaclie` ficha de un cliente (Cédula, RIF, Dirección, Teléfonos, Email, Observaciones).
- **Fuentes:** `listacle` poblado desde `clientes` (`funcion1.prg:listadoclie`).
- **Tipo:** maestro/consulta. `fichaclie` usa variables públicas (`pp_clie`, `cedula_rpt`…).

### 13. detallecompras/detalleventas(+obs) y *_clie(+2) — Detalle y estado de cuenta
- **Propósito:** el **detalle transaccional** (`detalle*`): por operación Nro., Fecha, Ref, Mda., Cantidad, Tasa, Monto, Pagado, Saldo, con observaciones en la variante `_obs`. El **estado de cuenta por cliente** (`*_clie`): agrupado por Cliente y Tipo de transacción, con totales por divisa (`qdivisa`) y total general; la variante `_2` agrega referencias, centro de costos y saldos en USD.
- **Cursores:** `detacomp`/`detavent` (detalle) y `detaclie`/`detaclie2` (por cliente).
- **Filtros:** rango `p_fecha1/p_fecha2`, cliente/proveedor, `p_monto_bs`/`p_saldo_bs` (totales calculados).
- **Tipo:** consulta. `detacomp/detavent/detarecibido/detaentrgad` añaden el desglose **Caja + Banco + Custodio** de cada operación.

### 14. cobros / PAGOS — Estados de cuenta de cobro y pago
- **Propósito:** `cobros` = "INFORME DE VENTAS / Compromiso de cobro" (Venta Nro., Nro.Op, Fecha, Monto, Saldo, Tipo Op, Recibo, Total cobrado, Cuenta débito/crédito). `PAGOS` = "ESTADO DE CUENTA SOBRE COMPRAS / Compromiso de pago" (espejo).
- **Cursores:** `venpagar_temp` / `compagar_temp`.
- **Tipo:** consulta (conciliación de pagos/recibos contra operación).

### 15. caso_01 — Auxiliar de movimientos ("caso 01")
- **Propósito:** tabla de movimientos con Fecha, Mes, Money, **T(X)**, Ref, Banco, Centro de Negocio, Observaciones; encabezado "Sin Indicar". Parece un **auxiliar contable/exportable** (usa `CTRL+W` para cerrar, sugiere captura).
- **Tipo:** consulta/auxiliar. De baja prioridad en la migración; verificar uso real.

---

## Implicaciones para la migración

### Principios transversales
1. **No migrar el `.FRX`; migrar la lógica de los cursores.** Cada reporte = un endpoint `.NET` que reproduce el `SELECT`/agregación que el form hacía proceduralmente (ahora en PostgreSQL con `SUM`, `FILTER`, `JOIN`, CTEs).
2. **Formato monetario:** conservar `999,999,999.99` (miles, 2 decimales). Usar `decimal(14,2)` en PG y pipe de formato en Angular.
3. **Moneda:** los montos se guardan mayoritariamente en **Bs**; el desglose USD se divide por la tasa de la operación (`monto_bs/cantidad`). La migración debe exponer **monto + tasa + moneda** para poder reconvertir en UI/reportes sin pérdida.
4. **Impresión PDF:** el sistema imprime en doble paso (preview + impresión) y exporta PDF opcional. En Angular, usar un **servicio de impresión** (template PDF/HTML print, ej. pdfmake o `window.print()` con CSS `@media print`) sobre los mismos componentes de tabla.

### Familias de reportes → endpoints de API (.NET)

| Familia | Reportes | Endpoint propuesto | Parámetros |
|---|---|---|---|
| **Estado general / posiciones** | estadogeneral*, consolidado | `GET /api/reportes/estado-general`, `GET /api/reportes/consolidado` | `fechaCorte`, `idCliente?`, `tasa?` |
| **Cuadre de caja** | cuadrebs, cuadredv | `GET /api/reportes/cuadre-caja` | `desde`, `hasta`, `moneda=BS|DIVISA` |
| **Movimientos de caja** | ingresos_caja(_cs) | `GET /api/reportes/caja` | `desde`, `hasta`, `tipo=ingreso|egreso`, `idCliente?`, `centroCosto?` |
| **Estado de cuenta cliente** | opercomp, opervent, detalle*_clie*, cobros, PAGOS | `GET /api/reportes/estado-cuenta/{idCliente}` | `desde`, `hasta`, `tipo=compra|venta|ambos` |
| **Detalle de transacciones** | detalle*, detacomp, detavent, detarecibido, detaentrgad | `GET /api/reportes/detalle-transacciones` | `desde`, `hasta`, `tipo`, `idCliente?`, `incluirObs` |
| **Cumplimiento entrega/recepción** | entregas, recepcion | `GET /api/reportes/cumplimiento/{tipo}/{idOperacion}` | `tipo=entrega|recepcion` |
| **Movimientos y saldos bancarios** | movimientoscr, movimientosdb, saldog1, genelal_1 | `GET /api/reportes/movimientos-bancarios` | `idBanco`, `idCuenta`, `desde`, `hasta`, `sentido=credito|debito|ambos` |
| **Custodios** | ver_custodios*, entregacustodio*, recibecustodio* | `GET /api/reportes/custodios` | `idCliente?`, `divisa?`, `desde`, `hasta`, `alcance=caja|banco|todos` |
| **Maestros** | listaclie, fichaclie | `GET /api/clientes`, `GET /api/clientes/{id}` | paginación/filtros |

### Documentos transaccionales (plantilla tipo recibo)
Estos **no** son informes: se generan **al momento de registrar** la operación y deben tener **plantilla de impresión tipo recibo/ticket** en Angular:
- `recibo` (recibo de caja)
- `COMPRA` / `VENTA` (comprobantes de operación)
- `ingresocaja`, `ingresobanco*` (ingresos a caja/banco)
- `entregacustodio*`, `recibecustodio*` (movimientos de custodio)

Modelo recomendado: un componente Angular **reutilizable de "documento comprobante"** (encabezado con razón social, datos de operación, montos con PICTURE, pie con firma/observaciones) parametrizado por `tipoDocumento`, servido por `GET /api/documentos/{tipo}/{id}` y exportable a PDF. Se imprimen una vez y quedan **consultables** (por eso los reportes de consulta de detalle deben poder volver a imprimir el comprobante original).

### Vistas / pantallas Angular por familia
- **Tablero gerencial:** tarjetas con Consolidado + Estado General por cliente (fecha de corte), con drill-down a estado de cuenta del cliente.
- **Cierre de caja:** pantalla de Cuadre (Bs y divisas) por rango + Ingresos/Egresos de caja con filtro de tipo.
- **Clientes:** listado maestro + ficha; desde la ficha, botón a estado de cuenta y custodios.
- **Bancos:** selector banco/cuenta → movimientos (créditos/débitos/saldo) y estado de cuenta.
- **Operaciones:** módulo compras/ventas con pestañas de Detalle, Estado de cuenta, Cumplimiento (entregas/recepciones), y Custodios.
- **Punto de caja / TPV:** flujo transaccional que registra el movimiento y dispara la impresión del comprobante (recibo/ingreso/entrega).

### Riesgos / notas de migración
- Muchos reportes son **variantes casi idénticas** (espejos compra/venta, `_clie` vs `_clie_2`, `cuadrebs` vs `cuadredv`, `ingresobanco` vs `ingresobanco2`). **Unificar** en componentes/endpoint parametrizados para reducir superficie.
- La numeración de recibos (`int(val(caja.recibo))`) y las claves (`id_clie`, `id_compra`, `id_venta`) deben preservarse como **legados** para no romper trazabilidad histórica.
- `estadogeneral_16122024` y `estadogeneral_new` son versiones/coopias; migrar **una** versión canónica.
- `caso_01` y reportes con exportación XLS (`x_namexls`) requieren confirmación de uso antes de invertir en su migración.
- Todos los textos vienen en cp1252 con acentos; al volcar a UTF-8 (PostgreSQL) verificar conversión de "Descripción", "Pág.", "Consolidado", etc.
