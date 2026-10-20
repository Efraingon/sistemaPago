# 03 — Reglas de negocio extraídas de los formularios (.SCX/.SCT)

**Sistema:** Monrroy / Mundodolar — casa de cambio y pagos (Visual FoxPro 6, cp1252)
**Origen:** `C:\Mundodolar\formas\*.scx` (+ memos `.SCT`)
**Destino migración:** Angular + .NET + PostgreSQL

> Cada regla indica: **FORMULARIO** origen → **EVENTO** → regla → **TABLAS** afectadas.
> Al final: RESUMEN de flujos de datos clave.

---

## 0. Modelo de datos relevante (confirmado contra `C:\Mundodolar\datos\`)

| Tabla | Rol | Campos clave |
|---|---|---|
| `CAJA` | Movimientos de caja (efectivo, ambas monedas) | `ORIGEN(2)`, `EJERCC(4)`, `ID_CAJA(10)`, `FECHA`, `RECIBO(20)`, `MONTO(14,2)`, `TASA(14,7)`, `USD(14,2)`, `MTOBS(14,2)`, `ID_MONY(4)`, `ID_CLIE`, `ID_COMPRA`, `ID_VENTA`, `ID_BANCO`, `CTASTRAN`, `NUMETRAN`, `ID_CODI`, `ID_CODE`, `FECHACIER`, `ID_CIERR`, `ANULADO`, `FECHANUL` |
| `CTASTRAN` | Movimientos de cuentas bancarias | `ORIGTRAN(2)`, `TIPOTRAN(2)` (`CR`/`DB`), `FECHTRAN`, `CTASTRAN(20)`, `NUMETRAN(20)`, `BENETRAN`, `MONTTRAN`, `ID_MONY`, `ID_BANCO`, `ID_CLIE`, `ID_COMPRA`, `ID_VENTA`, `DECAJA`, `TASA`, `USD`, `MTOBS`, `ANULADO`, `FECHANUL` |
| `COMPRAS` / `VENTAS` | Cabecera de operación | `EJERCC`, `ID_COMPRA`/`ID_VENTA(10)`, `ID_CLIE`, `FECHA`, `ID_MONY1`, `ID_MONY2`, `MTOXD1`, `MTOXD2`, `VALONACIO(14,7)`, `CANTIDAD(14,2)`, `MONTO_BS`, `PAGADO_BS`, `SALDO_BS`, `RECIBIDODV`, `PORECIB`, `CENTROCOST` |
| `COMPAGAR` / `VENPAGAR` | Cuotas de **pago** (compra) / **cobro** (venta) en Bs | `NROPER(5)`, `EJERCC`, `ID_*`, `FECHA`, `MONTO`, `BANCODEBE`, `CTADEBE`, `BANCOCREDI`, `CTACREDI`, `RECIBO`, `TIPOPAGO(15)`, `SALDO` |
| `COMPRECIB` / `VENRECIB` | Entregas de **divisa recibida** (compra) / **entregada** (venta) | `NROPER(5)`, `EJERCC`, `ID_*`, `FECHA`, `CANTIDAD`, `RECIBO`, `RECIBO2`, `PORECIB`, `BANCODEBE/CTADEBE`, `BANCOCREDI/CTACREDI`, `TIPOPAGO(15)`, `COMPENSAC(L)`, `DECAJA(L)`, `DEBANCO(L)` |
| `COMPENSAC` | Libro de **custodio** (abonos/retiros de divisa) | `EJERCC`, `ID_TRANS(10)`, `FECHA`, `DESCRIP`, `ID_CLIE`, `ID_MONY`, `ABONOS`, `RETIROS`, `RECITRAN(20)`, `CTASTRAN`, `CENTROCOST` |
| `CUSTODIO` | Saldo disponible de divisa en custodia | `ID_CLIE`, `ID_MONY`, `DISPONIBLE`, `DISPONIBB` |
| `CLIENTES` | Maestro | `ID_CLIE(10)`, `PERSONA(1)` (1=natural, 2=jurídica), `RAZONSOCIA`, `NOMBRES`, `APELLIDOS`, `RIF(15)`, `CEDULA(15)`, `CTACONTAB` |
| `BANCOS` | Entidades | `ID_BANCO(5)`, `BANCO`, `NACIONAL(1)` (**1 = nacional, 2 = internacional**), `SALDO`, `CTACONTAB` |
| `CTAS` | Cuentas **propias** | `ID_BANCO`, `NUMECTAS(20)`, `TIPO`, `DEBITOS`, `CREDITOS`, `SALDO`, `CTACONTAB` |
| `CTASCLIE` | Cuentas de **clientes** | `ID_BANCO`, `ID_CLIE`, `NUMECTAS(20)` |
| `CTASCLIE` (cliente) vs `CTAS` (casa) | — | Verificado en `nuevocobro`: `ctasclie_temp` = cuentas del **cliente**; `ctas_temp` = cuentas **propias** de la casa |
| `TIPOMONY` | Monedas | `ID_MONY(4)`, `DESCRIP`, `PRINCIPAL(L)` (la "divisa", USD), `NACIONAL(L)` (Bs), `VALONACIO`, `MONTODOLAR` |
| `DOLARMAIN` | Parámetros globales (1 registro) | `TASACTIVA`, `TASACOMP`, `TASAVENTA` (14,7), `IVA`, `ID_MONY` (divisa principal), `ID_MONY2` (nacional), `RUTA_DATA1`, `BLANQUEA`, `PRINNAC` |
| `CALENDTASA` | Calendario de tasas por día | `FECHA`, `NOMBDIA`, `TASA`, `TASACOMP`, `TASAVENTA` |
| `USUARIOS` | Usuarios | `ID_USUARIO(10)`, `NOMBREUSU`, `CLAVEUSU(10)`, `ACCESO(150)`, `ACTIVO`, `TIPO(1)` (**1 = administrador**), `BLOQUEO`, `FINANZAS`, `RUTAPANT` |
| `PERMISOS` | Catálogo de permisos | `DESCRIP(50)`, `VALOR(1)`, `FIJO(L)`, `FORMULARIO(50)`, `OBJETO(50)` |
| `BITACORA` | Auditoría | `FECHA` (datetime), `ID_USUARIO`, `TIPO`, `ACCION(200)`, `OBSERVA(200)`, `EQUIPO(50)` |
| `CODIINGRESO` / `CODIEGRESO` | Codificadores de ingreso/egreso | `ID_CODI`/`ID_CODE(5)`, `DESCRIP`, `CTACONTAB` |
| `CENTROSCLIE` | Centros de costo por cliente | `ID_CLIE`, `DESCRIP`, `PPAL` (`"X"` = principal) |

**Configuración global VFP** (`funcion1.prg` → `LoadSETUP`): `SET DELETE ON`, `SET DATE TO BRITISH` (dd/mm/aaaa), `SET CENTURY ON`, `SET EXACT OFF`, `SET SAFETY OFF`, `SET EXCLUSIVE OFF`, `SET MULTILOCKS ON`. En migración: `SET DELETE ON` ⇒ los `DELETE` lógicos sobre `VENRECIB`/`COMPRECIB` **sí** son borrados visibles; conviene reemplazarlos por borrado suave (soft-delete).

---

## 1. MÓDULO OPERACIONES (Compras / Ventas)

### 1.1 Alta de una venta
**`ventas_new.scx` → `Command5.Click` ("Registrar")**

1. Validaciones previas (todas con `RETURN` si fallan): cliente seleccionado (`p_id_clie`), centro de costo (`combo1`), tasa de venta (`text11`), cantidad a vender (`text4`), fecha (`text12`).
2. Confirmación explícita: `MESSAGEBOX("¿Está Ud. seguro de registrar la venta?", 292)`.
3. **Correlativo de venta:** `EJERCC = SUBSTR(DTOS(fecha),1,4)` (año de la operación). `ID_VENTA = MAX(ventas.id_venta) WHERE ejercc = <año>` + 1, **normalizado a 10 dígitos con ceros a la izquierda** (`STRTRAN(STR(VAL(max)+1,10,0)," ","0")`). Si no hay registros → `"0000000001"`.
4. **INSERT en `VENTAS`** con la semántica de saldos:
   - `ID_MONY1 = p_id_venta` (moneda `PRINCIPAL` = divisa USD), `ID_MONY2 = p_id_paga` (moneda `NACIONAL` = Bs).
   - `MTOXD1 = MTOXD2 = VALONACIO = p_valonacio` (la tasa capturada).
   - `CANTIDAD = text4` (cantidad de divisa).
   - **`MONTO_BS = ROUND(CANTIDAD * TASA, 2)`** (`calculo1`: `ROUND(thisform.text4.Value * p_valonacio,2)`).
   - `PAGADO_BS = 0.00`, `SALDO_BS = MONTO_BS`.
   - `RECIBIDODV = 0.00`, `PORECIB = CANTIDAD`.
   - `CENTROCOST` = descripción del centro de costo.
5. **Tasa por defecto** (`Load`): `p_valonacio = dolarmain.tasaventa`. En `compras_new.scx` es `dolarmain.tasacomp`.
6. Bitácora: `paccion = "REGISTRA LA VENTA NRO. " + EJERCC + ID_VENTA`.
7. Abre automáticamente `ventasrecipagos` (pago/cobro + recepción/entrega de divisas), imprime boleta `venta.frx` y opcionalmente exporta PDF.

**Reglas transversales:** el **monto en Bs se calcula siempre** como `ROUND(cantidad_divisa × tasa, 2)` — dos decimales, redondeo VFP (`ROUND`, half away from zero). La venta **no** toca caja ni banco por sí sola: sólo deja `PORECIB` (divisa por entregar) y `SALDO_BS` (Bs por cobrar).

### 1.2 Alta de una compra
**`compras_new.scx` → `Command5.Click`**

Simétrico a venta: `ID_COMPRAS` correlativo por `EJERCC`, `MONTO_BS = ROUND(CANTIDAD * TASA, 2)` con `p_valonacio = dolarmain.tasacomp`, `PAGADO_BS = 0`, `SALDO_BS = MONTO_BS`, `RECIBIDODV = 0`, `PORECIB = CANTIDAD`. Luego abre `comprasrecipagos`.

### 1.3 Cobro de una venta (Bs)
**`nuevocobro.scx` → `Command2.Click` ("Aceptar")** — invocado desde `ventasrecipagos`

- **Validaciones:**
  - Fecha, monto y tipo de cobro obligatorios.
  - Tipo de cobro ∈ {`DEPOSITO`, `TRANSFERENCIA`, `CAJA`}. Si `<> "CAJA"` exige además: cuenta del cliente (`combo2`, de `CTASCLIE`), cuenta propia a acreditar (`combo3`, de `CTAS`) y N° de documento (`text6`).
  - **Anti-duplicado de documento bancario:** `SEEK ALLTRIM(ctas_temp.numectas) + ALLTRIM(text6)` en `CTASTRAN` tag `buscatran`. Si existe y `!ctastran.anulado` → rechaza ("El número de documento ya existe" + fecha + beneficiario).
  - **Saldo insuficiente:** `IF thisform.text5.Value > thisform.text3.Value` (`text3 = p_saldo`) → "El monto a cobrar es mayor al saldo". **No se permite cobrar más de lo adeudado.**
- **`NROPER`** (número de cuota dentro de la operación): `MAX(nroper)+1` sobre la tabla temporal de la operación; arranca en 1.
- **Si el cobro es por BANCO** (`<> "CAJA"`): `INSERT` en `CTASTRAN` con `ORIGTRAN = "VE"`, `TIPOTRAN = "CR"`, `CTASTRAN = cuenta propia`, `NUMETRAN = documento`, `BENETRAN = p_cliente`, `MOTITRAN = "COBRANZA DE VENTA NRO. " + EJERCC + ID_VENTA`, `ID_VENTA`, `ID_CLIE`.
- **Si el cobro es por CAJA**: `INSERT` en `CAJA`:
  - `ORIGEN = "VE"` (Venta — cobranza).
  - `ID_CAJA` = correlativo **por ejercicio** (`MAX(caja.id_caja) WHERE ejercc = <año>` + 1, 10 dígitos, `0000000001` inicial).
  - `RECIBO` = correlativo **global** (`MAX(caja.recibo)` sobre **toda** la tabla, +1, 20 dígitos, `00000000000000000001` inicial). **El RECIBO es único global, no por año.**
  - `DESCRIP = "COBRANZA DE VENTA NRO. " + EJERCC + ID_VENTA`.
- `INSERT` en `VENPAGAR`: `NROPER`, `EJERCC`, `ID_VENTA`, `FECHA`, `MONTO`, `RECIBO`, `TIPOPAGO = combo1`, **`SALDO = p_saldo - monto`**.
- Bitácora: `"REGISTRA COBRANZA NRO. <nroper> DOCUMENTO: <doc> Compra Nro. <ejercc><id_venta>"`.

> **Nota de migración:** `ID_CAJA` (correlativo por año) y `RECIBO` (correlativo global) se generan con `SELECT MAX(...) INTO CURSOR`, **sin bloqueo transaccional** — carrera clásica. En .NET/PostgreSQL usar secuencia/serial o `INSERT ... RETURNING`. `MAX(caja.recibo)` global implica que un `RESTART` rompe la unicidad histórica.

### 1.4 Pago de una compra (Bs)
**`nuevopago.scx` → `Command2.Click`**

Espejo exacto del cobro: `ORIGEN = "CO"` en `CAJA`, `ORIGTRAN = "CO"` / `TIPOTRAN = "DB"` en `CTASTRAN`, `INSERT` en `COMPAGAR` con `SALDO = p_saldo - monto`. Descripción: `"PAGO EN COMPRAS: " + EJERCC + ID_COMPRA` (caja) / `"PAGO A COMPRAS. NRO. " + ...` (banco). Misma validación de monto ≤ saldo y de no duplicar documento.

### 1.5 Entrega de divisa en una venta
**`nuevoentrega.scx` → `Command2.Click`** — invocado desde `ventasrecipagos`

- **La cantidad total se distribuye entre (a lo sumo) dos destinos**, y la suma debe cuadrar exactamente:
  - `text3` = monto entregado **por CAJA**.
  - `text4` = monto entregado **por BANCO** (exige cuenta propia + cuenta cliente + N° documento, con la misma anti-duplicado en `CTASTRAN`).
  - `text10` = monto entregado **por COMPENSACIÓN a custodio**.
- **Validaciones de exclusión mutua:**
  - Si `text10 > 0` (custodio): **prohíbe** que `text3` o `text4` sean > 0 ("Transacción no válida — se está enviando un monto a custodio").
  - Si `text10 = 0`: exige `text3 + text4 = text1` ("La distribución de monto debe ser igual a la cantidad entregada").
  - Si `text10 > 0`: exige `text10 = text1`.
  - En `LostFocus` de `text3`/`text4`: `text3 + text4 > text1` ⇒ "Cantidad no válida" y anula el valor.
  - `text10 > text1` ⇒ "El envío al custodio es mayor a la cantidad a recibir".
- **Si hay parte por CAJA**: `INSERT` en `CAJA` con `ORIGEN = "EN"`, `ID_MONY = VENTAS.ID_MONY1`, correlativos `ID_CAJA`/`RECIBO` como arriba. Descripción `"ENTREGA DE DIVISAS VENTA NRO.: " + EJERCC + ID_VENTA`.
- **Si hay parte por BANCO**: `INSERT` en `CTASTRAN` con `ORIGTRAN = "VE"`, `TIPOTRAN = "DB"`, `ID_MONY`.
- **Si hay parte por CUSTODIO** (`text10 > 0`):
  - `CUSTODIO.ID_CLIE = VENTAS.ID_CLIE`, `ID_MONY = DOLARMAIN.ID_MONY`; si no existe el registro, lo crea.
  - **`CUSTODIO.DISPONIBLE = DISPONIBLE + text10`** (se **acumula** divisa bajo custodia).
  - `INSERT` en `COMPENSAC`: `ID_TRANS` correlativo **por ejercicio** (10 dígitos), `DESCRIP`, `ID_CLIE`, `ID_MONY`, **`ABONOS = text10`**, `RECITRAN = EJERCC + ID_TRANS` (formato del recibo de compensación).
  - `VENRECIB.COMPENSAC = .T.`, `TIPOPAGO = "COMPENSACION"`, `RECIBO = EJERCC + ID_TRANS`.
- `INSERT` en `VENRECIB`: `NROPER`, `EJERCC`, `ID_VENTA`, `FECHA`, `CANTIDAD`, **`PORECIB = p_cantid - cantidad`**; `TIPOPAGO` resultante ∈ {`"CAJA"`, `"BANCO"`, `"BANCO / CAJA"`, `"COMPENSACION"`}; banderas `DECAJA`/`DEBANCO` = `.T.` según corresponda; `RECIBO` (banco/compensación) y `RECIBO2` (caja) pueden ir ambos llenos en el caso mixto `"BANCO / CAJA"`.
- Tras el alta, `ventasrecipagos.Command4` recalcula: `VENTAS.RECIBIDODV = SUM(venrecib.cantidad)`, `VENTAS.PORECIB = CANTIDAD - RECIBIDODV`. **Si `PORECIB <= 0`** avisa "Ya se ha entregado la cantidad comprometida" pero **permite continuar** (puede entregar divisa de más, con confirmación).

### 1.6 Recepción de divisa en una compra
**`nuevorecibe.scx` → `Command2.Click`** — espejo de 1.5

`ORIGEN = "RE"` (caja) / `ORIGTRAN = "CO"`, `TIPOTRAN = "CR"` (banco). Para custodio: **`CUSTODIO.DISPONIBLE = DISPONIBLE - text10`** (se **retira** divisa de custodia) y `COMPENSAC.RETIROS = text10`. La validación de exclusión mutua es la misma pero invertida en el mensaje ("se está tomando un monto del custodio a compensación"). `TIPOPAGO = "CAJA - CUSTODIO"` cuando se usa `Check1` en `comprasclie_varios`.

### 1.7 Operaciones "VARIAS" (múltiples documentos en un solo movimiento)
**`comprasclie2.scx` / `ventasclie2.scx` / `comprasclie_varios.scx`** — agrupan varias operaciones del mismo cliente en **un** movimiento de caja/banco/custodio.

- Se recorre el cursor temporal (`compras_temp`/`ventas_temp`) filtrado por `recibir > 0` y, por cada operación, se hace un `INSERT` en `COMPRECIB`/`VENRECIB` con `CANTIDAD` proporcional y **el mismo `RECIBO`/`RECIBO2`** que el movimiento consolidado.
- En `CAJA`/`CTASTRAN` el identificador de operación se marca como literal **`"VARIAS C"`** (`ID_COMPRA = "VARIAS C"` / `ID_VENTA = "VARIAS C"`), no como `EJERCC+ID`.
- Los acumuladores de cada cabecera se actualizan en el bucle: `COMPRAS.PORECIB -= recibir`, `COMPRAS.RECIBIDODV += recibir`, `COMPRECIB.PORECIB = CANTIDAD - recibir`.
- `comprasclie_varios` añade un `Check1` para tomar del custodio (origen `C2`, `TIPOPAGO = "CAJA - CUSTODIO"`).

### 1.8 Recalculo / anulación de cobros y entregas
**`ventasrecipagos.scx` → `Command6.Click` (eliminar cobro), `anula_indi`, `anula_multi`** (idéntico en `comprasrecipagos.scx`)

- **Anulación de cobro (`Command6`):** busca en `VENPAGAR` por `buscaoper` (`NROPER + EJERCC + ID_VENTA`). Si `TIPOPAGO = "CAJA"` ⇒ marca **`CAJA.ANULADO = .T.`, `CAJA.FECHANUL = DATE()`** (no borra). En otro caso ⇒ **`CTASTRAN.ANULADO = .T.`, `CTASTRAN.FECHANUL = DATE()`**. Luego `DELETE` del registro en `VENPAGAR`, **re-numera `NROPER` consecutivo** y recalcula `SALDO` de cada cuota restando de `VENTAS.MONTO_BS`. Finalmente `VENTAS.PAGADO_BS -= monto`, `VENTAS.SALDO_BS = MONTO_BS - PAGADO_BS`. Bitácora `"ELIMINA EL PAGO NRO. ... RECIBO: ..."`.
- **Anulación de entrega individual (`anula_indi`):** distingue tres casos según las banderas del `VENRECIB`:
  1. `COMPENSAC = .T.` ⇒ **reverso contable**: copia el registro de `COMPENSAC`, le pone `DESCRIP = "REVERSO EN " + ...`, **`RETIROS = ABONOS` y `ABONOS = 0`**, le asigna `ID_TRANS` nuevo correlativo por ejercicio y **`CUSTODIO.DISPONIBLE = DISPONIBLE - RETIROS`** (deshace el abono).
  2. `DEBANCO AND DECAJA` ⇒ anula **ambos**: `CTASTRAN.ANULADO/.FECHANUL` y `CAJA.ANULADO/.FECHANUL`.
  3. Sólo uno de los dos ⇒ anula sólo ese.
  - Si no encuentra los datos ⇒ `"No se han encontrado los datos para la anulación"`.
  - Acto seguido: `DELETE` de `VENRECIB`, re-numera `NROPER`, recalcula `PORECIB` y `VENTAS.RECIBIDODV`/`PORECIB`.
- **Anulación múltiple (`anula_multi`):** mismo algoritmo aplicado a **todos** los `VENRECIB` que compartan `EJERCC + RECIBO`; usa una tabla temporal `captura (clave)` para recolectar las operaciones afectadas y luego recalcula cada una desde cero (`RECIBIDODV = 0`, `PORECIB = CANTIDAD` y re-acumula).

### 1.9 Modificación de documentos ya emitidos
**`modidoc.scx`** — modifica **referencias** de un cobro/entrega ya registrado (no montos). Se abre desde `ventasrecipagos`/`comprasrecipagos` (DblClick sobre la columna del banco) cargando `p_tipooper`, `p_nrooper`, `p_bancooper`, `p_ctaoper`, `p_fechaoper`, `p_montooper`, `p_numetran`.

| Procedimiento | Qué cambia | Control de duplicado |
|---|---|---|
| `cobro_en_ventas` | `CTASTRAN.NUMETRAN`, `VENPAGAR.RECIBO` (y su copia temporal) | Rechaza si el **nuevo** N° ya existe en `CTASTRAN` (tag `buscatran`) y muestra a quién pertenece |
| `recepcion_de_divisas` | `COMPRECIB.RECIBO` (recorrido completo de la tabla) | Ídem |
| `modi_en_compras` / `modi_en_ventas` | Cambia la **cuenta** de débito: `CTASTRAN.CTASTRAN`, `COMPAGAR/VENPAGAR.BANCODEBE`, `.BANCODEBE`, `.CTADEBE` | Ídem |
| `modi_entrega` / `modi_recepcion` | Cuenta/recibo de `VENRECIB`/`COMPRECIB` | Ídem |

Todas registran en bitácora la sustitución (`"REEMPLAZA EL NÚMERO DE RECIBO <viejo> POR <nuevo> ..."`).

> **No existe anulación de la cabecera `COMPRAS`/`VENTAS`** en los formularios: sólo anulación de los movimientos de caja/banco asociados y de las cuotas. Tampoco hay **doble captura / aprobación de dos personas** en ninguna parte del código analizado — la única confirmación es un `MESSAGEBOX` Yes/No del mismo usuario.

---

## 2. MÓDULO CAJA

### 2.1 Semántica de `CAJA.ORIGEN` (tabla completa)

| ORIGEN | Significado | Efecto en saldo caja divisa | Efecto en saldo caja Bs. | Escrito por |
|---|---|---|---|---|
| `IN` | Ingreso de caja (moneda **nacional**) — reposición | — | **+** | `cajareposicion` |
| `EG` | Egreso de caja (moneda **nacional**) — reposición | — | **−** | `cajareposicion` |
| `DE` | Depósito de **divisa** a caja | **+** | — | `cajareposicion` |
| `DS` | Disposición (retiro) de **divisa** de caja | **−** | — | `cajareposicion` |
| `RE` | Recepción de divisa (compra) vía caja | **+** | — | `nuevorecibe`, `comprasclie2`, `comprasclie_varios` |
| `EN` | Entrega de divisa (venta) vía caja | **−** | — | `nuevoentrega`, `ventasclie2` |
| `C1` | Recepción de **custodio** a caja | **+** | — | `custodios` |
| `C2` | Entrega de **custodio** desde caja | **−** | — | `custodios`, `comprasclie_varios` |
| `VE` | Cobranza de venta vía caja | — | **+** | `nuevocobro` |
| `CO` | Pago de compra vía caja | — | **−** | `nuevopago` |
| `IB` | Ingreso a caja desde banco | — | **+** | `bancoscredi` (egreso a cuenta ⇒ entra a caja) |
| `EB` | Egreso de caja hacia banco | — | **−** | `bancoscredi` (abono a cuenta con fondos de caja) |
| `T5` | Transferencia internacional **salida** de caja | **−** | — | `bancoscredi2` |
| `T6` | Transferencia internacional **entrada** a caja | **+** | — | `bancoscredi2` |

*(Fuente: `conosmon.scx` y `consolidado.scx`, que suman/restan por `ORIGEN`; y los `replace caja.origen WITH ...` de cada formulario.)*

### 2.2 Generación de correlativos de caja (patrón repetido en todos los formularios)
```
EJERCC   = SUBSTR(DTOS(fecha),1,4)
ID_CAJA  = MAX(caja.id_caja) WHERE ejercc = EJERCC   + 1  → 10 dígitos, "0000000001" inicial
RECIBO   = MAX(caja.recibo)   (tabla completa)        + 1  → 20 dígitos, "00000000000000000001" inicial
```
Patrón idéntico para `COMPENSAC.ID_TRANS` (`MAX(compensac.id_trans) WHERE ejercc = <año>` + 1).

### 2.3 Ingresos y egresos de caja (reposición)
**`cajareposicion.scx` → `Command3.Click` (ingreso) / `Command4.Click` (egreso)**

- **Requiere** que exista una moneda marcada `TIPOMONY.NACIONAL` (si no, deshabilita ambos botones: "Debe indicar la moneda nacional de parámetros").
- **Conversión de moneda** (`Text2.LostFocus` / `Text7.LostFocus`) usando `p_valonacio = dolarmain.tasactiva`:
  - Si la moneda seleccionada **es la nacional** ⇒ `text8 (USD) = monto / tasa` (Bs → USD).
  - Si **no** es nacional (divisa) ⇒ `text8 (MTO_BS) = monto * tasa` (USD → Bs).
  - La tasa puede sobreescribirse en `text7`; si se pone ≤ 0 se restaura `dolarmain.tasactiva`.
- **Ingreso** (`Command3`): si moneda es nacional ⇒ `ORIGEN = "IN"`, `TASA = p_valonacio`, `USD = text8`; si no ⇒ `ORIGEN = "DE"`, `TASA`, `MTOBS = text8`. Siempre: `EJERCC`, `ID_CAJA`, `FECHA`, `RECIBO`, `MONTO`, `DESCRIP`, `ID_MONY`, **`ID_CODI`** (codificador de ingreso), `OBSERVA`.
- **Egreso** (`Command4`): idéntico pero `ORIGEN = "EG"` (nacional) / `"DS"` (divisa) y **`ID_CODE`** (codificador de egreso).
- **Saldo displayed de caja** (`Combo1.Click`) — se recalcula recorriendo `CAJA` entera:
  - *Rama nacional:* excluye `ANULADO`, excluye los que tengan `FECHACIER` **no vacío** (ya cerrados) y excluye `C1`, `C2`, `RE`; luego suma/resta por origen (`CO` −, `IN` +, `EG` −, `VE` +, `DP` −, `RT` +).
  - *Rama divisa:* recorre por `ID_MONY`, excluye `ANULADO`, aplica `DE` +, `RE` +, `C1` +, `T6` +, `C2` −, `DS` −, `EN` −, `T5` −.

### 2.4 Abonos / débitos a cuentas nacionales
**`bancoscredi.scx` → `Command3.Click` (abono) / `Command4.Click` (débito)**

- Checkbox `Check1` = "usar fondos de caja". **Si está marcado**, además del movimiento bancario se genera el movimiento de caja correspondiente:
  - Abono con fondos de caja ⇒ `CAJA.ORIGEN = "EB"`, `CTASTRAN.DECAJA = .T.`
  - Débito a caja desde banco ⇒ `CAJA.ORIGEN = "IB"`, `CTASTRAN.DECAJA = .T.`
- `CTASTRAN.ORIGTRAN = "DP"` (depósito) con `TIPOTRAN = "CR"` para abono; `ORIGTRAN = "RT"` (retiro) con `TIPOTRAN = "DB"` para débito.
- `BENETRAN = dolarmain.nombclie` (la casa). `ID_CODI`/`ID_CODE`, `TASA = p_valonacio`, `USD = text8`.
- **No hay validación de saldo bancario insuficiente** en el débito: sólo valida campos no vacíos.

**`bancoscredi2.scx`** — mismos botones pero con `CAJA.ORIGEN = "T5"`/`"T6"` y `CTASTRAN.ORIGTRAN = "T5"`/`"T6"` (transferencias internacionales), guardando `TASA` y `MTOBS`.

### 2.5 Anulación de recibos de caja
**`recibos.scx` → `Command3.Click`**

- **Restricción de origen:** sólo permite anular recibos con `ORIGEN ∈ {"IN","DE","EG","DS"}` (ingresos/egresos de reposición). Cualquier otro origen ⇒ `"Origen no válido"`.
- Si `caja.anulado` ya ⇒ `"El recibo ya está anulado"`.
- Efecto: **`CAJA.ANULADO = .T.`, `CAJA.FECHANUL = DATE()`**. Bitácora `"ANULA EL RECIBO DE CAJA NRO: <recibo>"`.
- La grilla pinta en rojo (`RGB(255,147,147)`) los registros anulados vía `DynamicBackColor`.
- **Búsqueda por recibo** usa `INT(VAL(caja.recibo))` (el recibo se maneja numéricamente en la UI aunque se almacena como texto de 20).

### 2.6 Reportes y cierre de caja
**`cajareport.scx`** (menú *Tesorería → Caja → Reportes*) — genera reportes por rango `p_fecha1`/`p_fecha2`, saltándose anulados:

- `saldo_divisas_cs` (Cuadre de caja): primero **atrapa el saldo anterior** sumando todos los movimientos con `fecha < p_fecha1` (excluyendo anulados) con la misma regla de signos (`DE/RE/C1/T6` +, `EN/C2/DS/T5` −) y lo vuelca como fila `"SALDO ANTERIOR"` con `FECHA = p_fecha1 - 1`. Luego itera el rango y va acumulando `SALDO_ACT = SALDO_ANT + MONTO`. Traduce `ORIGEN` a descripción leyendo la tabla `RPTCAJA`, y adjunta cliente y centro de costo (`CENTROSCLIE` con `PPAL = "X"`).
- Reportes separados de **INGRESOS** (origenes `IN`, `VE`, `IB`) y **EGRESOS** (origenes `EG`/`CO`/`EB`, etc.), y de divisas nacional/internacional.

> **⚠ HUECO DE MIGRACIÓN — cierre de día:** los campos **`CAJA.FECHACIER`** (fecha de cierre) e **`ID_CIERR`** (ID de quien cierra) **no se escriben en ningún formulario `.SCX`**. Sólo se *leen* en `cajareposicion.scx` (`IF !EMPTY(caja.fechacier) → SKIP`, es decir, el saldo ya cerrado no se reincorpora). El escritor está compilado dentro de `monrroy.exe` (fuente no disponible) o en una herramienta externa. **Antes de migrar hay que recuperar esa lógica** (probablemente un "Cierre de caja/día" que marca los movimientos del día como cerrados y guarda el saldo resultante). El menú sí tiene *Tesorería → Caja → Cierres*, apuntando a `cajareport`, pero `cajareport` no escribe `FECHACIER`.

### 2.7 Conciliación ("cuadre") en `operaciones.scx`
Crea tablas temporales `cuadrebs (tipo, recibo, descrip, fecha, entradas, salidas)` y `cuadredv (…, divisa, entradas, salidas)` para cuadrar entradas/salidas de caja en Bs y en divisa en un rango de fechas. Es **sólo reporte**, no escribe en `CAJA`.

---

## 3. MÓDULO CUSTODIOS (compensaciones)

### 3.1 Qué es "compensación"
`COMPENSAC` es el **libro mayor de divisa retenida en custodia** por cliente. `CUSTODIO.DISPONIBLE` es el **saldo resumido** de ese libro por `(ID_CLIE, ID_MONY)`. Una compensación permite liquidar una operación **sin pasar por caja ni banco**: la divisa "se queda" en el sistema en poder del cliente.

- `ABONOS > 0` ⇒ el cliente **entrega** divisa al sistema (aumenta su custodio). Usado en `nuevoentrega` (venta) y en `custodios.Recepción`.
- `RETIROS > 0` ⇒ el cliente **retira** divisa de su custodio. Usado en `nuevorecibe` (compra) y en `custodios.Entrega`.
- Un registro nunca usa ambas columnas a la vez (el código pone `RETIROS = ABONOS` y `ABONOS = 0` en los reversos).
- **Recibo de una compensación** = `EJERCC + ID_TRANS` (p. ej. `202600000001`).

### 3.2 Recepción y entrega de custodio
**`custodios.scx` → `Command3.Click` (recepción) / `Command4.Click` (entrega)**

- **Validación de moneda:** si la moneda seleccionada está marcada `TIPOMONY.NACIONAL` ⇒ `"La divisa seleccionada es parámetro nacional" / "NO VÁLIDO PARA CUSTODIO"`. **El custodio sólo admite la divisa (no la nacional).**
- **Recepción** (`Command3`): `CUSTODIO.DISPONIBLE += monto` (crea el registro si no existe); `INSERT COMPENSAC` con `ABONOS = monto`, `RECITRAN = <recibo>`; `INSERT CAJA` con **`ORIGEN = "C1"`**, `ID_MONY`, `CENTROCOST`, `OBSERVA`; `DESCRIP = "RECEPCIÓN CUSTODIO DIVISAS: <moneda>(CAJA)"`. Correlativos de caja y de `COMPENSAC.ID_TRANS` por ejercicio.
- **Entrega** (`Command4`): **valida disponibilidad** — `IF custodio.disponible < monto` ⇒ *"La disponibilidad del cliente es menor a la cantidad a retirar… ¿Desea indicar saldo en negativo?"* — **permite continuar con saldo negativo si el usuario confirma** (no es bloqueante). Si el cliente no tiene registro de custodio ⇒ *"El cliente no tiene datos en custodio… ¿Desea registrarlo con saldo negativo?"*. Efecto: `CUSTODIO.DISPONIBLE -= monto`; `INSERT COMPENSAC` con `RETIROS = monto`; `INSERT CAJA` con **`ORIGEN = "C2"`**.
- Ambos piden centro de costo obligatorio.

### 3.3 Custodio en cuentas bancarias
**`custodiosb.scx`** — mismo concepto pero moviendo custodio **a cuentas** en vez de caja: `CTASTRAN.ORIGTRAN = "CU"`, `TIPOTRAN = "CR"` (recepción de custodio en cuenta) o `"DB"` (entrega), con `MOTITRAN = "RECEPCIÓN DE CUSTODIO EN CUENTA, CENTRO DE COSTO: ..."`, `ID_CLIE`, `ID_MONY`. Genera también el asiento en `COMPENSAC`.

### 3.4 Reporte de custodio
`custodios.scx` (botón consulta) recorre `COMPENSAC` por cliente y moneda, acumulando `tcustodio.entrega = compensac.abonos` y `tcustodio.retira = compensac.retiros`; si el cliente no tiene movimientos ⇒ *"El cliente seleccionado no tiene divisas en custodio"*.

### 3.5 Regla consolidada de signos del custodio
```
CUSTODIO.DISPONIBLE (por ID_CLIE + ID_MONY)
    = Σ COMPENSAC.ABONOS  (entradas al custodio)
    − Σ COMPENSAC.RETIROS (salidas del custodio)
```
Mantener **ambas** tablas sincronizadas es un invariante crítico: en el código viejo se actualiza el resumen (`CUSTODIO`) en línea con cada movimiento; una migración a PostgreSQL debería derivar `CUSTODIO` de `COMPENSAC` (o usar `UPDATE ... SET disponible = disponible + x` con `SELECT ... FOR UPDATE`).

---

## 4. MÓDULO CLIENTES / CUENTAS

### 4.1 Alta de cliente
**`clieingreso.scx`**

- `PERSONA`: 1 = natural (`NOMBRES`+`APELLIDOS`), 2 = jurídica (`RAZONSOCIA`). El nombre visible se compone como `ALLTRIM(razonsocia)` o `ALLTRIM(nombres)+" "+ALLTRIM(apellidos)`.
- **Validación de unicidad por RIF** (`Text4.LostFocus`): `SET ORDER TO tag rif` → `SEEK ALLTRIM(valor)`. Si existe ⇒ *"El RIF indicado pertenece a: <cliente>"*, limpia el campo. Es **sólo advertencia** (no bloquea guardar, pero vacía el textbox).
- **Validación de unicidad por CÉDULA** (`Text5.LostFocus`): `SET ORDER TO tag cedula` → `SEEK`. Si existe ⇒ *"La cédula indicada pertenece a: <cliente>"* y limpia.
- No hay validación de **formato** (no valida longitud, dígitos verificadores ni prefijo V-/J-/E-). El RIF se copia a `text10` truncado a 50 caracteres.
- `ID_CLIE` se genera con el patrón correlativo de 10 dígitos.
- Alta de cliente también da de alta el centro de costo principal (`CENTROSCLIE.DESCRIP`).
- **Modificación:** `cliemodifica.scx` (mismas reglas de unicidad aplicadas al editar).

### 4.2 Cuentas bancarias de clientes
**`ctasclie.scx` → `Command2.Click`**

1. Exige banco y número de cuenta.
2. **Limpieza del número:** recorre el valor y **conserva sólo dígitos** (`ISDIGIT`), descartando cualquier separador o carácter.
3. **Validación de longitud:** `IF LEN(numectas) != 20` ⇒ *"Debe indicar el número de cuenta a 20 dígitos"*. **Formato estricto: 20 dígitos numéricos.**
4. **Anti-duplicado:** `SEEK ALLTRIM(id_banco)+ALLTRIM(id_clie)+ALLTRIM(numectas)` en tag `ordenado` ⇒ *"Ya existe"*.
5. `INSERT CTASCLIE (ID_BANCO, ID_CLIE, NUMECTAS)` + bitácora.

**`ctasbanco.scx` → `Command1.Click`** — idéntico para las cuentas **propias** (`CTAS`), con validación de unicidad global del número de cuenta y mensaje indicando el banco al que ya pertenece. `APERTURACTAS.scx` aplica la misma regla de 20 dígitos.

### 4.3 Eliminación de cuentas
**`eliminacuenta.scx`** — antes de borrar verifica, en **seis** tablas y con `COUNT`, que la cuenta no esté en uso (excluyendo anulados donde aplica):

```
CTASTRAN  (ctastran = cuenta y !anulado)              → "transacciones de banco"
CAJA      (caja.ctastran = cuenta y !anulado)         → "transacciones de caja"
VENRECIB  (ctacredi = cuenta OR ctadebe = cuenta)     → "transacciones de ventas"
COMPRECIB (ctacredi = cuenta OR ctadebe = cuenta)     → "transacciones de compras"
VENPAGAR  (ctacredi = cuenta OR ctadebe = cuenta)     → "transacciones de ventas"
COMPAGAR  (ctacredi = cuenta OR ctadebe = cuenta)     → "transacciones de compras"
```
Si alguna cuenta > 0 ⇒ bloquea con mensaje. Solo si todas están en 0 ⇒ `DELETE` (con confirmación).

### 4.4 Centros de costo
`CENTROSCLIE` se filtra por cliente; el principal se marca con `PPAL = "X"`. En alta de operación el centro de costo es **obligatorio**. `frmcentrosclie.scx` permite agregar centros en línea desde la operación.

---

## 5. MÓDULO TASAS Y MONEDAS

### 5.1 Parámetros de tasa globales
**`modifica_tasas.scx` → `Command2.Click`**

- Edita los tres campos de `DOLARMAIN`: **`TASACTIVA`**, **`TASACOMP`**, **`TASAVENTA`** (14,7 decimales).
- Validación: los tres deben ser `> 0`; si no se modificó ninguno ⇒ *"No existen cambios a realizar"*.
- **Propagación al calendario** (`ms = "¿Desea actualizar el calendario de tasas?"`): si se acepta, hace `SEEK DATE()` en `CALENDTASA`; si no existe lo crea, y **sobrescribe el registro del día de hoy** con `TASA = dolarmain.tasactiva`, `TASACOMP = dolarmain.tasacomp`, `TASAVENTA = dolarmain.tasaventa`, `NOMBDIA = UPPER(CDOW(DATE()))`.
- Bitácora con los tres valores anteriores y nuevos.

**Regla de negocio:** existen **tres tasas distintas** y se usan en contextos distintos:
- `TASAVENTA` → precio al que la casa **vende** divisa (default en `ventas_new`).
- `TASACOMP` → precio al que la casa **compra** divisa (default en `compras_new`).
- `TASACTIVA` → tasa de referencia para **convertir**Bs↔USD en caja/reposición (`cajareposicion`) y en el **consolidado**.

### 5.2 Calendario de tasas
**`tasacalendario.scx`**

- Vista por mes (control `Olecontrol1` con `.year`/`.month`); `SET FILTER TO YEAR(fecha)=<año> AND MONTH(fecha)=<mes>`.
- Permite editar `TASA`, `TASACOMP`, `TASAVENTA` por día; en `LostFocus` compara con el valor previo capturado en `GotFocus` (`f_tasa`, `f_tasacomp`, `f_tasaventa`) y **sólo registra bitácora si cambió** (`"MODIFICA CALENDARO DE TASA (ACTIVA/DE COMPRAS/DE VENTAS) DIA: <fecha> ANTES: <v> AHORA: <v>"`).
- Crea el registro del día si no existe (`FECHA`, `NOMBDIA`).
- **Restricción de edición de año:** `MESSAGEBOX("Indique el mismo año en ambas fechas")` para el rango.
- Existe `tasacalendario_old.scx` (versión anterior, sin cambios).

### 5.3 Monedas
**`tipomony_new.scx`** — catálogo `TIPOMONY`. Reglas:
- **No se puede renombrar a una descripción existente:** `LOCATE FOR ALLTRIM(c_descrip.descrip) = ALLTRIM(valor)` ⇒ *"La descripción indicada ya existe"* y restaura el valor anterior.
- Bitácora del cambio (`"MODIFICA DESCRIPCION DE DIVISA = <vieja> POR <nueva>"`).
- **`configuracion.scx`** sincroniza las monedas con la tasa activa:
  - Divisa principal: `tipomony.montodolar = dolarmain.tasactiva`, `tipomony.valonacio = 1`.
  - Moneda nacional: `tipomony.montodolar = 1`, `tipomony.valonacio = dolarmain.tasactiva`.

### 5.4 Conversión de moneda (reglas exactas encontradas)

| Contexto | Fórmula |
|---|---|
| Monto en Bs de una operación (`ventas_new`, `compras_new`) | `MONTO_BS = ROUND(CANTIDAD_divisa × TASA, 2)` |
| Bs → USD en reposición de caja (`cajareposicion`, moneda nacional) | `USD = MONTO / p_valonacio` |
| USD → Bs en reposición de caja (`cajareposicion`, divisa) | `MTOBS = MONTO × p_valonacio` |
| Transacciones internacionales (`bancoscredi2`) | guardan `TASA = p_valonacio` y `MTOBS = MONTO × tasa` |

`CAJA` y `CTASTRAN` **persisten la tasa usada** (`TASA`), el equivalente en USD (`USD`) y el equivalente en Bs (`MTOBS`) — preservar estos tres campos permite reconstruir el histórico sin re-convertir.

---

## 6. MÓDULO TRANSFERENCIAS BANCARIAS

**`transferencias.scx`** — 4 páginas (nacional mismo titular, nacional terceros, internacional mismo titular, internacional terceros). Todas usan `CTASTRAN.ORIGTRAN ∈ {T1,T2,T3,T4}` y `TIPOTRAN ∈ {CR,DB}`.

### 6.1 Transferencia nacional — mismo titular (`Page1`, `ORIGTRAN = "T1"`)
- **Validación:** `IF ALLTRIM(combo1) = ALLTRIM(combo2)` ⇒ *"Debe indicar cuentas diferentes"*. (Sólo se valida en esta página.)
- Escribe **dos** movimientos espejo con el **mismo `NUMETRAN`**:
  1. `TIPOTRAN = "DB"` sobre la cuenta de origen (`ctasp11`), `MOTITRAN = "TRANSFERENCIA DB MISMO TITULAR A LA CUENTA: <destino>"`.
  2. `TIPOTRAN = "CR"` sobre la cuenta destino (`ctasp12`), `MOTITRAN = "TRANSFERENCIA CR MISMO TITULAR DESDE LA CUENTA: <origen>"`.
- `BENETRAN = text2` (titular). Bitácora `"REGISTRA TRANSFERENCIA NAL. MISMO TITULAR NRO. <nro>"`.
- **Saldo neto de la casa = 0** (afecta el detalle por cuenta, no el consolidado).

### 6.2 Transferencia nacional — terceros (`Page2`, `ORIGTRAN = "T2"`)
- Escribe **un solo** movimiento: `TIPOTRAN = "CR"` sobre la cuenta del tercero (`ctasp22`), `BENETRAN = dolarmain.nombclie`, y guarda el enlace con el tercero: `ID_CLIE`, `ID_BANCO` (del tercero), `CTASCLIE` (cuenta del tercero). `MOTITRAN = "TRANSFERENCIA CR TERCEROS: <titular>"`.
- Requiere seleccionar **titular** y su **cuenta** (validación `combo2`: "Seleccione Titular").

### 6.3 Internacionales (`Page3`/`Page4`, `ORIGTRAN = "T3"`/`"T4"`)
Mismo esquema que las nacionales pero sobre cuentas internacionales; en `bancoscredi2` las variantes con impacto en caja usan `T5`/`T6`. El saldo bancario por cuenta las trata distinto (ver §7).

---

## 7. MÓDULO SALDOS Y MOVIMIENTOS BANCARIOS

### 7.1 Cálculo de saldo por cuenta
**`saldos.scx` (`individual2`) y `movimientos.scx` (`general1`/`general2`)**

Recorren `CTAS` → `CTASTRAN` por cuenta y acumulan:
```
SALDO = Σ (si TIPOTRAN = "CR" → MONTTRAN) − Σ (si TIPOTRAN = "DB" → MONTTRAN)
```
**Filtros aplicados (invariantes importantes):**
- Se excluyen los **anulados** (`ctastran.anulado`).
- Se excluyen `T1`, `T2`, `DP`, `RT` cuando se calcula el saldo de **cuentas internacionales** (`individual2`) — porque son movimientos internos/nacionales.
- Se excluyen `T3`, `T4`, `T5`, `T6` cuando se calcula el saldo de **cuentas nacionales**.
- Regla especial sobre `CO`/`VE` (operaciones compra/venta que tocan banco):
  - En cuentas **internacionales**: se incluyen **sólo** los `CO`/`VE` que tengan `ID_MONY` **vacío** (`IF EMPTY(ctastran.id_mony) → SKIP`).
  - En cuentas **nacionales**: se incluyen **sólo** los `CO`/`VE` que tengan `ID_MONY` **no vacío** (`IF !EMPTY(ctastran.id_mony) → SKIP`).
  > (Los comentarios `*!*` en el código muestran que esta condición se invirtió respecto de una versión anterior: hay que **confirmar el sentido vigente** con datos reales antes de migrar.)
- Opcionalmente se limita por fecha de corte (`fal1`, variable global que pone `fecha_de_corte.scx`).

### 7.2 Sincronización del saldo de la entidad
Tras calcular, **`movimientos.scx` y `saldos.scx` escriben de vuelta**:
```
BANCOS.SALDO = versaldos.creditos − versaldos.debitos
```
Es decir, `BANCOS.SALDO` es un **campo derivado cacheado** — en la migración debería ser una vista o un procedimiento, no un valor fuente.

### 7.3 Fecha de corte
**`fecha_de_corte.scx`** — sólo pone/limpia la variable global `fal1` (fecha de corte). `Command1` la limpia (`fal1 = CTOD("  /  /  ")`); la pone el llamador. `consolidado` y `saldos` la usan para limitar el alcance del cálculo.

### 7.4 Consolidado
**`consolidado.scx` → `Init`** — suma todo el histórico (con corte opcional `fal1`):
- **Compras:** acumula `MONTO_BS`, `PAGADO_BS`, `SALDO_BS`, `CANTIDAD`, `RECIBIDODV`, `PORECIB`.
- **Ventas:** ídem.
- **Caja nacional (`text14`):** excluye anulados; `CO` −, `EB` −, `EG` −, `IB` +, `VE` +, `IN` +.
- **Caja divisa (`text15`):** `DE` +, `RE` +, `C1` +, `T6` +, `C2` −, `DS` −, `EN` −, `T5` −.
- **Cuentas (`text16`/`text17`):** por cada `CTASTRAN` no anulado, mira el banco de la cuenta; si `BANCOS.NACIONAL = 1` acumula en `text16`, si no en `text17`; `CR` suma, `DB` resta.
- **Custodio (`text18`):** `Σ ABONOS − Σ RETIROS` de `COMPENSAC`.
- La **tasa activa** del consolidado puede cambiarse en pantalla, pero *"el cambio de la tasa activa afectará únicamente a los resultados de este reporte"* — no escribe en `DOLARMAIN`.

**`conosmon.scx`** — mismo barrido de caja separando Bs (`text2`) y divisa (`text3`) por `ORIGEN`, más el conteo de billetes/monedas (`CONONAC`/`CONODOL`: total = `Σ valor × cantidad`).

---

## 8. MÓDULO SEGURIDAD Y USUARIOS

### 8.1 Autenticación
**`login.scx`**

- `Combo1` (usuario) con `RowSource = "usuarios.nombreusu"`; el `LostFocus` fija `pid_usuario`, `historico_finanzas` y limpia `pclave`/`pacceso`. Excluye usuarios bloqueados: `SET FILTER TO !usuarios.bloqueo`.
- **Validación de clave** (`Text1.LostFocus` y `Command2.Click`): `SET ORDER TO tag idclave` → `SEEK ALLTRIM(pid_usuario) + ALLTRIM(clave)`. Si `EOF()` ⇒ *"Clave desconocida"*.
  > **La clave se compara con `SEEK` sobre un índice — no hay hashing.** `USUARIOS.CLAVEUSU` es texto plano de hasta 10 caracteres.
- Al autenticar exitosamente se cargan: `pclave`, `nombreusuario`, `pacceso` (cadena de permisos), `p_tipousu = usuarios.tipo`, `pid_usuario`, `p_rutapant`, `fecha_version = ultimaversion`.
- Registra en `RUTASERVERTEMP` (tabla temporal en `\temp\` y `\tempsegu\`) el `ULT_USU` — **marca de "quién está logueado"** compartida entre estaciones.
- Bitácora: `paccion = "ENTRO AL SISTEMA"`.
- **Salida** (`Command1.Click` y `funcion1.prg → salida`): `USUARIOS.ACTIVO = .F.`, bitácora `"SALIO DEL SISTEMA"`, `CLOSE DATABASES ALL`.

### 8.2 Modelo de permisos (matriz posicional)
**`permisar.scx`, `modiuario.scx`, `nuevousuario.scx`, `cambiarclave.scx`**

- `PERMISOS` es un **catálogo** de actividades (una fila por actividad), con `FORMULARIO` y `OBJETO` (ambos = nombre del formulario en mayúsculas, p. ej. `"COMPRAS_NEW"`), `DESCRIP` y un `FIJO` que marca permisos no borrables.
- **`USUARIOS.ACCESO` es un STRING de 150 caracteres**, donde la **posición N** corresponde al **`RECNO()` N** de la tabla `PERMISOS`. Cada carácter es `"S"` o `"N"`.
  - Carga (`permisar.Load`): si `usuarios.tipo = 1` (administrador) ⇒ `REPLACE permisos.valor WITH "S" ALL`. Si no, recorre `PERMISOS` con un contador `i` y pone `valor = "S"` si `SUBSTR(usuarios.acceso, i, 1) = "S"`, si no `"N"`.
  - Guardado (`Command2`): reconstruye la cadena concatenando `permisos.valor` de todos los registros y la guarda en `usuarios.acceso`. Si cambió `tipo`, lo escribe y registrа bitácora (`"COLOCA COMO ADMINISTRADOR"` / `"COLOCA COMO PERMISADO"`). Si el usuario editado es el actual (`pid_usuario`), **refresca `pacceso` en caliente**.
- **Evaluación en tiempo de ejecución** — patrón repetido en el menú y en los formularios:
  ```
  SELECT permisos ; SEEK <FORMULARIO>+<FORMULARIO>
  IF EOF()            → permitido por defecto (actividad aún no registrada)
  ELSE x_recno = RECNO()
       IF SUBSTR(pacceso, x_recno, 1) != "S" → denegado
  ```
  - **Administrador** (`tipo = 1` / `p_tipousu = 1`) salta toda la comprobación ⇒ acceso total.
  - Si la actividad **no existe** en `PERMISOS`, se **permite** por defecto (fail-open).
  - El menú (`MENUPRINC.MPR`) repite este bloque por cada barra (`x_permitido`, y si no ⇒ `DO avisarpermiso` → *"Usuario no autorizado / Solicite permiso al Administrador"*).
- **Registro de nuevas actividades** (RightClick en casi todos los formularios): `IF SUBSTR(pacceso, 2, 1) = "S"` ⇒ puede auto-agregar la actividad a `PERMISOS` (`GO BOTT; APPEND BLANK; DESCRIP/FORMULARIO/OBJETO`). **El carácter en posición 2 de `ACCESO` es el permiso "administrativo" para ampliar el catálogo.**
- **Reglas de protección en `permisar.scx`:**
  - `Text1.Valid`: sólo acepta `"S"` o `"N"`; si `usuarios.tipo = 1` **fuerza `"S"`** (no se le puede quitar permisos al admin); y **no permite poner `"N"` en la fila 1** (`IF RECNO() = 1` ⇒ *"No debe quitar el permiso de cambio de clave"*).
  - `Command3` (borrar actividad): **prohibido si `permisos.fijo`** (*"Actividad de parámetros, no debe borrarla"*). Si se borra, hace `PACK` y **recorre todos los usuarios re-indentando `ACCESO`** (`SUBSTR(acceso,1,x_recno-1) + SUBSTR(acceso,x_recno+1,…)`) — el borrado desplaza todas las posiciones posteriores. Requiere doble confirmación.
- **`nuevousuario.scx`:** `ID_USUARIO = VAL(ID_USUARIO del último) + 1` (10 dígitos). Inicializa `ACCESO = "SS"` (o `"S"`), `FINANZAS = año actual`, `TIPO = f_tipousu`, `RUTAPANT`.
- **`cambiarclave.scx`:** exige clave nueva ≠ actual y que la confirmación coincida. Bitácora `"MODIFICÓ SU CLAVE DE ACCESO"`.
- **`permisar.scx` `Command4`:** el administrador puede **resetear la clave de un usuario a `"1234"`** (bitácora `"REINICIA A 1234 LA CLAVE DEL USUARIO …"`).

> **⚠ HUECO DE MIGRACIÓN:** la matriz de permisos es **posicional y frágil** (depende del orden físico/físico de `PERMISOS`; un `PACK` o un `REINDEX` puede alterar `RECNO()`). En la migración conviene modelarla como tabla de relación `(usuario_id, permiso_id, permitido)` con clave única, eliminando la cadena de 150 caracteres.

### 8.3 Bitácora (auditoría)
**`funcion1.prg → PROCEDURE bitacora`** — invocado con `DO bitacora` tras casi toda operación de escritura.

```
APPEND BLANK en BITACORA
  FECHA      = DATETIME()
  ID_USUARIO = pid_usuario
  TIPO       = ptiponum      (por defecto 1)
  ACCION     = paccion        (texto de la operación, p.ej. "REGISTRA LA VENTA NRO. 202600000001")
  OBSERVA    = pobserva + " Desde: " + SYS(0) + " Actualizado al: " + ultimaversion
  EQUIPO     = SYS(0)        (nombre de estación + usuario OS)
Luego: STORE "" TO paccion, pobserva ; ptiponum = 1
```
**`ver_bitacora.scx`** la consulta por usuario y rango de fechas (`SET ORDER TO tag porfecha descend`), filtrando usuarios bloqueados. `ver_detabitac.scx` / `ver_filtbitac.scx` son el detalle y el filtro.

> **Regla clave:** el registro de auditoría es **append-only** y se dispara desde el código de cada formulario (no hay trigger en BD). Cada formulario arma el texto de `paccion` a mano. En la migración, centralizar en un servicio/domain-event para no perder cobertura.

### 8.4 Respaldo
**`respaldos.scx`** — avisa primero si hay otros usuarios conectados; comprueba si ya se hizo respaldo en la fecha; luego ejecuta `COPY FILE <ruta_data><ext> TO <rutabak><ext>` por cada extensión listada en una tabla de extensiones. Es una **copia de archivos VFP a nivel de SO**, no un backup de BD transaccional. La migración a PostgreSQL debe sustituirlo por `pg_dump`/WAL archiving.

---

## 9. MÓDULO CONFIGURACIÓN

**`configuracion.scx`**
- Cambia la **ruta de datos**: compara `dolarmain.ruta_data1` con `rutaservertemp.ruta_datos`; si difiere, lo propaga a ambas (`\temp\` y `\tempsegu\`) con bitácora (`"REEMPLAZA RUTA DATOS: <nueva> POR: <anterior>"`) y obliga a reiniciar ("Se ha modificado la ruta de los datos").
- Parámetros booleanos en `CONFIGREPOR`/`DOLARMAIN`: `enviar`/`preguntar` (PDF automático o preguntado) para reportes de caja y de banco, `blanquea`, `prinnac`.
- Enlaza a `tipomony`, `conosmon`, `modifica_tasas`, `maestro_ctbld`.

**`codificaingresos.scx` / `codificaegresos.scx`** — catálogos `CODIINGRESO`/`CODIEGRESO` (`ID_CODI`/`ID_CODE`, `DESCRIP`, `CTACONTAB`); el `ID_CODI` se guarda en `CAJA` para ingresos y `ID_CODE` para egresos.

**Maestro contable** (`MAESTRO`: `CTACONTAB`, `DESCRIPCIO`) — casi todas las entidades (`CLIENTES`, `BANCOS`, `CTAS`, `CTASCLIE`, `CODIINGRESO`, `CODIEGRESO`) arrastran un `CTACONTAB`, indicando que el sistema alimenta un plan contable.

---

## 10. RESUMEN DE FLUJOS DE DATOS CLAVE

### 10.1 Flujo completo de una **NUEVA VENTA**
```
ventas_new.Command5
 ├─ valida: cliente, centro de costo, tasa>0, cantidad>0, fecha
 ├─ confirmación de usuario
 ├─ ID_VENTA = MAX(id_venta WHERE ejercc=año)+1  (10 dígitos)
 ├─ INSERT VENTAS  {ejercc, id_venta, id_clie, fecha,
 │                  id_mony1=USD, id_mony2=BS,
 │                  valonacio=mtoxd1=mtoxd2=tasa,
 │                  cantidad, monto_bs=ROUND(cantidad*tasa,2),
 │                  pagado_bs=0, saldo_bs=monto_bs,
 │                  recibidodv=0, porecib=cantidad, centrocost}
 ├─ BITACORA "REGISTRA LA VENTA NRO. <ejercc><id_venta>"
 └─ DO FORM ventasrecipagos   ← pantalla de liquidación
      ├─ [Cobrar Bs]  → nuevocobro    (§10.2)
      └─ [Entregar divisa] → nuevoentrega (§10.3)
      + REPORT FORM venta.frx (boleta) + PDF opcional
```

### 10.2 Flujo completo de un **NUEVO COBRO** (Bs sobre una venta)
```
nuevocobro.Command2
 ├─ valida: fecha, monto, tipo (DEPOSITO|TRANSFERENCIA|CAJA)
 ├─ si tipo <> "CAJA": exige cuenta cliente + cuenta propia + N° documento
 │    └─ anti-duplicado: SEEK ctas.numectas+N° en CTASTRAN (buscatran) y !anulado → rechaza
 ├─ valida monto ≤ saldo de la operación        (saldo insuficiente → rechaza)
 ├─ NROPER = MAX(nroper de la operación)+1
 ├─ ¿es por CAJA?
 │    ├─ SÍ  → INSERT CAJA {origen="VE", ejercc=año,
 │    │                     id_caja=MAX(id_caja WHERE ejercc)+1 (10d),
 │    │                     recibo=MAX(recibo) GLOBAL +1 (20d),
 │    │                     fecha, monto,
 │    │                     descrip="COBRANZA DE VENTA NRO. "+ejercc+id_venta,
 │    │                     id_venta, id_clie}
 │    └─ NO  → INSERT CTASTRAN {origtran="VE", tipotran="CR", fechtran,
 │                              ctastran=cta propia, numetran=doc,
 │                              benetran=cliente, monttran,
 │                              motitran="COBRANZA DE VENTA NRO. "+…,
 │                              id_venta, id_clie}
 ├─ INSERT VENPAGAR {nroper, ejercc, id_venta, fecha, monto,
 │                   recibo, tipopago=tipo, saldo=p_saldo-monto}
 └─ BITACORA "REGISTRA COBRANZA NRO. <nroper> DOCUMENTO: <doc> ..."
   (luego ventasrecipagos recalcula: VENTAS.pagado_bs=SUM(venpagar.monto),
                                     VENTAS.saldo_bs=monto_bs-pagado_bs)
```

### 10.3 Flujo completo de una **ENTREGA DE DIVISA** (sobre una venta)
```
nuevoentrega.Command2
 ├─ distribución en 3 destinos excluyentes (caja / banco / custodio):
 │    si text10(custodio)>0 → prohíbe caja y banco
 │    si text10=0           → exige text3(caja)+text4(banco) = text1 (total)
 │    si text10>0           → exige text10 = text1
 ├─ banco (text4>0): anti-duplicado en CTASTRAN; INSERT CTASTRAN {origtran="VE", tipotran="DB", id_mony}
 ├─ caja  (text3>0): INSERT CAJA {origen="EN", id_caja, recibo, id_mony,
 │                                descrip="ENTREGA DE DIVISAS VENTA NRO.: "+…}
 ├─ custodio (text10>0):
 │    CUSTODIO[id_clie,id_mony].disponible += text10      (crea fila si no existe)
 │    ID_TRANS = MAX(compensac.id_trans WHERE ejercc)+1
 │    INSERT COMPENSAC {ejercc, id_trans, descrip, id_clie, id_mony,
 │                      abonos=text10, recitran=ejercc+id_trans}
 ├─ INSERT VENRECIB {nroper, ejercc, id_venta, fecha, cantidad,
 │                   porecib=p_cantid-cantidad,
 │                   recibo (banco/compensación), recibo2 (caja),
 │                   tipopago ∈ {CAJA, BANCO, "BANCO / CAJA", COMPENSACION},
 │                   compensac, debanco, decaja}
 └─ BITACORA "REGISTRA ENTREGA DE DIVISAS NRO. …"
   (ventasrecipagos recalcula: VENTAS.recibidodv=SUM(venrecib.cantidad),
                               VENTAS.porecib=cantidad-recibidodv)
```

### 10.4 Flujo de una **NUEVA COMPRA** y su **RECEPCIÓN DE DIVISA**
Idéntico a §10.1–10.3 con los espejos: `COMPRAS` / `COMPAGAR` / `COMPRECIB`, `CAJA.ORIGEN ∈ {CO (pago), RE (recepción)}`, `CTASTRAN.ORIGTRAN = "CO"`. La **recepción** con custodio hace `CUSTODIO.disponible -= monto` y `COMPENSAC.RETIROS = monto`.

### 10.5 Flujo de **COMPENSACIÓN** (custodio) — resumen
```
ENTREGA (venta, cliente pone divisa):  custodio.disponible += X ; COMPENSAC.abonos = X   (ORIGEN C1 o "COMPENSACION")
RECEPCIÓN (compra, cliente saca divisa): custodio.disponible -= X ; COMPENSAC.retiros = X (ORIGEN C2 o "COMPENSACION")
ANULACIÓN: registro REVERSO {retiros=abonos_previos, abonos=0, descrip="REVERSO EN …"}
           y custodio.disponible -= retiros
Invariante: CUSTODIO.DISPONIBLE = Σ ABONOS − Σ RETIROS (por cliente y moneda)
```

### 10.6 Diagrama de impacto por evento (referencia rápida de migración)

| Evento | CAJA | CTASTRAN | COMPENSAC | CUSTODIO | Cabecera (COMPRAS/VENTAS) | Cuotas (COMPAGAR/VENPAGAR/COMPRECIB/VENRECIB) |
|---|---|---|---|---|---|---|
| Alta compra/venta | — | — | — | — | `INSERT` | — |
| Cobro por banco | — | `INSERT` `VE`/`CR` | — | — | — | `INSERT VENPAGAR` |
| Cobro por caja | `INSERT` `VE` | — | — | — | — | `INSERT VENPAGAR` |
| Pago por banco | — | `INSERT` `CO`/`DB` | — | — | — | `INSERT COMPAGAR` |
| Pago por caja | `INSERT` `CO` | — | — | — | — | `INSERT COMPAGAR` |
| Entrega divisa (venta) | `INSERT` `EN` | `INSERT` `VE`/`DB` | `INSERT` `abonos` | `disponible +=` | `UPDATE` `recibidodv`/`porecib` | `INSERT VENRECIB` |
| Recepción divisa (compra) | `INSERT` `RE` | `INSERT` `CO`/`CR` | `INSERT` `retiros` | `disponible -=` | `UPDATE` `recibidodv`/`porecib` | `INSERT COMPRECIB` |
| Custodio vía caja | `INSERT` `C1`/`C2` | — | `INSERT` | `+`/`−` | — | — |
| Custodio vía cuenta | — | `INSERT` `CU` | `INSERT` | `+`/`−` | — | — |
| Ingreso/egreso reposición | `INSERT` `IN`/`EG`/`DE`/`DS` | — | — | — | — | — |
| Abono/débito a cuenta | `INSERT` `EB`/`IB` (si `Check1`) | `INSERT` `DP`/`RT` | — | — | — | — |
| Transferencia nacional | — | `INSERT` `T1` (×2) / `T2` | — | — | — | — |
| Transferencia internacional | `INSERT` `T5`/`T6` | `INSERT` `T3`/`T4`/`T5`/`T6` | — | — | — | — |
| Anular cobro | `ANULADO=.T.` (si caja) | `ANULADO=.T.` (si banco) | — | — | `UPDATE pagado_bs`/`saldo_bs` | `DELETE` + re-numerar `NROPER` |
| Anular entrega | `ANULADO=.T.` (si caja) | `ANULADO=.T.` (si banco) | `INSERT` reverso | `disponible −=` | `UPDATE recibidodv`/`porecib` | `DELETE` + re-numerar `NROPER` |
| Anular recibo de reposición | `ANULADO=.T.`, `FECHANUL` | — | — | — | — | — |
| Cierre de caja/día | `FECHACIER`, `ID_CIERR` | — | — | — | — | — |

---

## 11. HUECOS Y PUNTOS A CONFIRMAR ANTES DE MIGRAR

1. **Cierre de caja (`CAJA.FECHACIER` / `ID_CIERR`)** — se leen pero **no se escriben en ningún `.SCX`**; el escritor está en el `.EXE` compilado. **Recuperar esa lógica es requisito previo.**
2. **Carrera en correlativos** — todos los `MAX(...)+1` son no transaccionales. Reemplazar por secuencias en PostgreSQL.
3. **Sentido de la condición `ID_MONY` en `CO`/`VE`** para saldos de cuentas nacionales vs internacionales (§7.1): hay versiones comentadas que la invierten. Validar contra datos reales.
4. **Sin doble captura ni aprobación de dos personas** — la única barrera es un `MESSAGEBOX` del mismo usuario. Si el negocio la requiere, es funcionalidad **nueva**.
5. **Claves de usuario en texto plano** (`SEEK` sobre índice) — migrar a hash (p. ej. bcrypt/Argon2).
6. **Permisos posicionales** (cadena de 150 chars ligada a `RECNO()`) — rediseñar como relación explícita.
7. **`SET DELETE ON` + `DELETE` físico`** en `VENRECIB`/`COMPRECIB` — pasar a soft-delete para conservar historia.
8. **Catálogo `RPTCAJA`** — `cajareport.scx` lo abre para traducir `CAJA.ORIGEN` → descripción legible (`rptcaja.descrip`). No existe como `.DBF` suelto ni está en `data1.dbc`; se abre en tiempo de ejecución. **Recuperar su contenido** (o sustituirlo por un ENUM/tabla nueva en PostgreSQL) es requisito para los reportes de caja.

> Nota sobre los datos de ejemplo: la BD de referencia en `C:\Mundodolar\datos\` es de pruebas — `PERMISOS.DBF` y `USUARIOS.DBF` tienen solo 3 registros cada uno (2 de ellos `FIJO=.T.`). La cadena `ACCESO(150)` y la matriz de permisos están **dimensionadas para hasta 150 actividades**; el modelo completo se deduce del código (`permisar.scx`, `permisosvalor`), no de los datos cargados.
8. **`BANCOS.SALDO` y `CUSTODIO.DISPONIBLE` son campos derivados cacheados** — derivarlos de `CTASTRAN` y `COMPENSAC` respectivamente.
9. **Tablas temporales en disco** (`c:\mundodolar\temp\*_temp.dbf`) usadas como "estado de sesión" de los formularios — sustituir por estado de aplicación/EF Core en memoria.
10. **Marca de sesión en `RUTASERVERTEMP`** (`ULT_USU`) — replicada en dos carpetas (`\temp\`, `\tempsegu\`); sustituir por gestión de sesiones real.
