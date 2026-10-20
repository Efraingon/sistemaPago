# 01 — Análisis de Estructura de Base de Datos (VFP → PostgreSQL)

> Sistema: **Monrroy / Mundodolar** — "SISTEMA ADMINISTRATIVO MONRYS CAPITAL LLC"
> Fuente: `C:\Mundodolar\datos\` (28 tablas DBF + contenedor `data1.dbc`)
> Volumen total: ~140.000 registros. Codificación origen: cp1252.

## 1. Resumen de tablas por dominio

### 1.1 Maestros / Configuración
| Tabla | Registros | Descripción | Campos clave |
|---|---|---|---|
| `DOLARMAIN` | 1 | **Configuración global del sistema**: empresa (MONRYS CAPITAL LLC, RIF, dirección, logo), IVA 16%, monedas por defecto (`ID_MONY`=0001 USD, `ID_MONY2`), **3 tasas**: `TASACTIVA` (referencia/conversión), `TASACOMP` (default compra), `TASAVENTA` (default venta), versión | Una fila; funciona como tabla de settings |
| `TIPOMONY` | 2 | Catálogo de monedas: `0001` = USD/BS (`PRINCIPAL=.T.`), `0002` = BS (`NACIONAL=.T.`) | `ID_MONY` PK |
| `CALENDTASA` | 37 | Calendario de tasas por fecha (`TASA`, `TASACOMP`, `TASAVENTA`) | `FECHA` PK lógico |
| `CLIENTES` | 393 | Maestro de clientes: persona natural (1) o jurídica (2) con `RAZONSOCIA`/`NOMBRES`+`APELLIDOS`, RIF, cédula, dirección, teléfonos, email, cuenta contable | `ID_CLIE` PK (CHAR 10) |
| `CENTROSCLIE` | 460 | Centros de costo por cliente (`PPAL='X'` = principal, se replica a `caja/compras/ventas.centrocost`) | `ID_CLIE` + `DESCRIP` |
| `BANCOS` | 37 | Entidades bancarias: nacionales (`NACIONAL=1`: BANESCO, MERCANTIL, BANCO DE VENEZUELA...) e internacionales (`NACIONAL=2`: WELLS FARGO, CITI BANK); **`SALDO` es campo cacheado** (recalculado por formularios saldos/movimientos) | `ID_BANCO` PK (CHAR 5) |
| `CTAS` / `CTASBANCO` | 16 / 0 | Cuentas bancarias (número 20 dígitos, `TIPO`, débitos/créditos/saldo cacheado) | (`ID_BANCO`,`NUMECTAS`) |
| `CTASCLIE` | 498 | Cuentas bancarias de clientes (banco + cliente + nº cuenta) | (`ID_BANCO`,`ID_CLIE`,`NUMECTAS`) |
| `CUSTODIO` | 195 | **Saldos de custodia por (cliente, moneda)**: `DISPONIBLE` (divisa) y `DISPONIBB` (Bs) — campos cacheados = Σ `compensac.abonos` − Σ `compensac.retiros` | (`ID_CLIE`,`ID_MONY`) |
| `MAESTRO` | 14 | Plan contable (cuentas 4.1.x ingresos, 6.1.x gastos) | `CTACONTAB` PK |
| `CODIINGRESO` / `CODIEGRESO` | 8 / 6 | Codificación de ingresos/egresos → cuenta contable | `ID_CODI` / `ID_CODE` |
| `USUARIOS` | 3* | Usuarios: `CLAVEUSU` **en texto plano**, `ACCESO` = string de 150 chars (permisos posicionales), `TIPO` (1=admin), `FINANZAS`, `ACTIVO`, `BLOQUEO` | `ID_USUARIO` PK |
| `PERMISOS` | 3* | Catálogo de permisos: la posición física del registro (=RECNO) define el índice en `USUARIOS.ACCESO`; `FORMULARIO`/`OBJETO` | Auto incremental físico |
| `CONFIGREPOR` | 1 | Configuración de envío de reportes (PDF/Excel) | Una fila |
| `VER_ERROR` | 93 | Log de errores (`ON ERROR` global): fecha, usuario, mensaje, programa, línea, equipo |

*BD de prueba incluida; producción vive en `En_Usuarios`/respaldos.

### 1.2 Transacciones de Compra/Venta de Divisas
| Tabla | Registros | Descripción |
|---|---|---|
| `COMPRAS` | 8.860 | **Cabecera de compra de divisa**: cliente, fecha, monedas (`ID_MONY1` divisa, `ID_MONY2` nacional), `CANTIDAD` (USD), tasa `VALONACIO`, `MONTO_BS` = ROUND(CANTIDAD × tasa, 2), `PAGADO_BS`, `SALDO_BS`, `RECIBIDODV`, `PORECIB` (por recibir), centro de costo |
| `VENTAS` | 2.234 | **Cabecera de venta de divisa** — misma estructura espejo (la casa vende USD al cliente) |
| `COMPRECIB` | 8.349 | **Cuotas de recepción de una compra** (el cliente paga en partes): `NROPER` (correlativo por operación), cantidad, recibos, bancos (débito/crédito), banderas `COMPENSAC`/`DECAJA`/`DEBANCO` |
| `COMPAGAR` | 11.522 | **Pagos de una compra**: monto, banco débito/crédito, `TIPOPAGO`, `SALDO` |
| `VENRECIB` | 2.566 | **Cobros de una venta** (espejo de COMPRECIB) |
| `VENPAGAR` | 3.049 | **Pagos de una venta** (espejo de COMPAGAR) |

### 1.3 Caja y Custodia
| Tabla | Registros | Descripción |
|---|---|---|
| `CAJA` | 9.129 | **Libro de caja** (movimientos de efectivo): `ORIGEN` (código de tipo, ver §2), `EJERCC` (ejercicio/año), `ID_CAJA` = correlativo 10 dígitos **por año**, `RECIBO` = correlativo **global** 20 dígitos, `MONTO`, moneda, `TASA`/`USD`/`MTOBS` (monto en Bs según tasa), vínculo a `ID_COMPRA`/`ID_VENTA`/`ID_CLIE`/banco/transferencia, cierre (`FECHACIER`,`ID_CIERR`), anulación (`ANULADO`,`FECHANUL`), codificación contable (`ID_CODI`/`ID_CODE`), centro de costo |
| `COMPENSAC` | 8.265 | **Movimientos de custodia de clientes**: `ID_TRANS` = correlativo 10 dígitos por año, `ABONOS` (entra a custodia) / `RETIROS` (sale), moneda, referencia bancaria (`RECITRAN`,`CTASTRAN`), centro de costo. **Invariante**: custodia = Σ abonos − Σ retiros |
| `CTASTRAN` | 23.904 | **Libro de transferencias bancarias** (nacionales e internacionales): `ORIGTRAN` (origen), `TIPOTRAN`, cuenta y nº de transferencia, beneficiario, monto, moneda, banco, tasa/USD/MTOBS, cliente, vínculo compra/venta, `DECAJA` (viene de caja), anulación |
| `CONODOL` / `CONONAC` | 0 / 0 | **Denominaciones de billetes** (USD y Bs): valor × cantidad — para arqueo/conteo de caja (catálogo vacío en esta BD) |

### 1.4 Auditoría
| Tabla | Registros | Descripción |
|---|---|---|
| `BITACORA` | 54.845 | Auditoría: fecha (datetime), usuario, tipo, acción, observación, equipo (SYS(0)), bloqueo |

## 2. Claves codificadas (dominio)

**`CAJA.ORIGEN`** (2 caracteres):
`IN` ingreso | `EG` egreso | `CO` pago en compras | `VE` cobro en ventas | `RE` recepción de divisas (compra) | `EN` entrega de divisas (venta) | `C1`/`C2` recepción/entrega custodio caja | `DE`/`DS` depósito/... custodio | `IB`/`EB`/`EG` ingreso/egreso banco | `T5`/`T6` transferencias.

**`CTASTRAN.ORIGTRAN`**: `CO` compra | `VE` venta | `CU` custodio | `DP` depósito | `RT` retiro | `T1`–`T6` transferencias (nac./internac., propio/tercero).

**`TIPOMONY`**: `PRINCIPAL` marca la divisa dura (USD), `NACIONAL` la moneda local (Bs).

## 3. Modelo de relación inferido (ER)

```
CLIENTES ─┬─< COMPRAS >── COMPRECIB / COMPAGAR       (cuotas de recepción/pago)
          ├─< VENTAS  >── VENRECIB  / VENPAGAR
          ├─< CAJA          (ID_CLIE + ID_COMPRA/ID_VENTA)
          ├─< COMPENSAC ──> CUSTODIO (cache: Σabonos−Σretiros por cliente+moneda)
          ├─< CTASTRAN      (transferencias del cliente)
          ├─< CENTROSCLIE   (centros de costo, PPAL='X')
          └─< CTASCLIE      (cuentas bancarias del cliente)

BANCOS ──< CTAS / CTASBANCO          (cuentas de la casa)
BANCOS ──< CTASTRAN / CAJA           (movimientos bancarios)
TIPOMONY ──< CAJA / COMPENSAC / CTASTRAN / COMPRAS / VENTAS   (moneda)
DOLARMAIN (1) — tasas activas del sistema
CALENDTASA — historial/calendario de tasas
CODIINGRESO / CODIEGRESO ──< CAJA.ID_CODI / ID_CODE ──> MAESTRO (plan contable)
USUARIOS ──< BITACORA ; PERMISOS (permisos posicionales en USUARIOS.ACCESO)
```

> **Nota**: VFP no tiene FKs; las relaciones se infieren por nombres de campo (`ID_*`). La migración a PostgreSQL debe formalizarlas con FK + índices.

## 4. Reglas de integridad y negocio detectadas a nivel de datos

1. **Numeración/ correlativos** por `MAX(...)+1` (sin transacción → carreras en origen):
   - `CAJA.ID_CAJA`: 10 dígitos por año (`EJERCC`), `CAJA.RECIBO`: 20 dígitos global.
   - `COMPENSAC.ID_TRANS`: 10 dígitos por año.
   - `NROPER` en cuotas: correlativo secuencial **dentro** de cada operación (re-numerado tras DELETE).
   - En PostgreSQL → **sequences** por tabla (+ unique constraint por año).
2. **Campos cacheados a recalcular** (denormalización VFP): `CUSTODIO.DISPONIBLE/DISPONIBB`, `BANCOS.SALDO`, `CTAS.SALDO/DEBITOS/CREDITOS`, `COMPRAS/VENTAS.PAGADO_BS/SALDO_BS/PORECIB`, `COMPAGAR/COMPAGAR.SALDO`. La migración puede mantenerlos como columnas materializadas con recálculo transaccional o derivarlos por vista.
3. **Anulaciones por bandera, no borrado** en `CAJA` y `CTASTRAN` (`ANULADO=.T.` + `FECHANUL`); las cuotas (`VENRECIB`/`COMPRECIB`) sí se borran físicamente. → En el sistema nuevo: `anulado_en` (soft delete) uniforme.
4. **Cierre de caja**: `CAJA.FECHACIER`/`ID_CIERR` marcan el corte; el escritor está en el EXE (hueco detectado — reconstruir lógica: cerrar día = marcar todos los movimientos del día con corte).
5. **Moneda dual**: todo movimiento puede registrar monto en moneda fuente + tasa + equivalente en Bs (`TASA`,`USD`,`MTOBS`). Tres tasas no intercambiables (activa/compra/venta).
6. **SET DELETE ON** global: los borrados lógicos VFP son invisibles; validar al migrar datos históricos.

## 5. Volumen y estrategia de migración de datos

| Tabla | Registros | Estrategia sugerida |
|---|---|---|
| BITACORA | 54.845 | Migrar reciente (12–24 meses); el resto a archivo |
| CTASTRAN | 23.904 | Migrar histórico completo (auditoría bancaria) |
| COMPAGAR | 11.522 | Migrar completo |
| CAJA | 9.129 | Migrar completo (libro mayor) |
| COMPRAS/COMPRECIB | 8.860/8.349 | Migrar completo |
| COMPENSAC | 8.265 | Migrar completo (base de custodios) |
| VENPAGAR/VENRECIB/VENTAS | 3.049/2.566/2.234 | Migrar completo |
| CLIENTES | 393 | Migrar completo |
| Tablas pequeñas | <500 | Migrar completo |

**Tipos VFP → PostgreSQL**: `CHAR(n)`→`CHAR(n)`/`VARCHAR(n)`, `NUMERIC(n,2)`→`NUMERIC(14,2)`, `NUMERIC(14,7)`→`NUMERIC(14,7)` (tasas), `DATE`→`DATE`, `DATETIME`→`TIMESTAMP`, `LOGICAL`→`BOOLEAN`, `MEMO`→`TEXT`, `GENERAL`→`BYTEA` (logo), `INTEGER`→`INTEGER`. IDs `CHAR` se conservan como texto (legacy) para trazabilidad con documentos impresos.
