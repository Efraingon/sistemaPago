# Base de Datos — PostgreSQL (monrys)

Esquema migrado desde el sistema Visual FoxPro 6 `C:\Mundodolar\datos\` (28 tablas DBF).

## Instalación

La base **debe crearse en UTF-8** (el cluster de Windows 18 viene en `SPANISH_VENEZUELA.1252`):

```powershell
psql -U postgres -d postgres -c "CREATE DATABASE monrys ENCODING 'UTF8' TEMPLATE template0 LOCALE_PROVIDER icu LOCALE 'und';"

# orden correcto (los datos migrados tienen PRIORIDAD sobre el seed):
psql -U postgres -d monrys -f database\01_schema.sql      # esquema
psql -U postgres -d monrys -f database\03_functions.sql   # funciones/triggers
Get-ChildItem database\migracion\*.sql | ForEach-Object { psql -U postgres -d monrys -f $_.FullName }   # 199k filas reales
psql -U postgres -d monrys -f database\02_seed.sql        # idempotente: rellena lo que falta (ADMIN, roles, catálogos)
```

> `02_seed.sql` usa `ON CONFLICT DO NOTHING` en todas sus cargas: se ejecuta **después**
> de la migración y solo inserta lo que el origen VFP no aporta (roles, permisos,
> usuario `ADMIN`). Si ejecutas el seed antes, tendrás que volver a correrlo al final.

## Contenido

| Archivo | Descripción |
|---|---|
| `01_schema.sql` | Esquema completo: maestros, clientes, bancos, compras/ventas, caja, custodio, transferencias, seguridad (roles/permisos), auditoría. Tipos migrados VFP→PG con enums, FKs e índices. |
| `02_seed.sql` | Datos base: monedas (USD/BS), configuración (tasas activa/compra/venta), plan contable, codificación ing/egr, denominaciones de billetes (arqueo), bancos, roles/permisos, usuario `ADMIN` (clave `admin123`, PBKDF2 — cambiar en primer ingreso). |
| `03_functions.sql` | `fn_siguiente_correlativo` (numeración transaccional por año), `fn_recalcular_custodio` + trigger (invariante custodia = Σ abonos − Σ retiros), `fn_cierre_caja`, `fn_insertar_caja`, `fn_anular_movimiento` (soft-delete uniforme). |

## Mejoras sobre el origen VFP

- **Numeración con candado** (tabla `correlativo`) — elimina la condición de carrera del `MAX()+1` original.
- **Custodio** recalculado por trigger (en VFP dependía de formularios).
- **Anulaciones** uniformes con `estado` + `anulado_en`/`anulado_motivo` (VFP mezclaba flags y DELETE físico).
- **Permisos declarativos** (`rol`/`permiso`) reemplazan el string posicional de 150 caracteres con fail-open.
- **Claves** con PBKDF2 (el VFP guardaba texto plano).
- Se conservan los IDs `CHAR` legados (`0001`, `20220000000001`) para trazabilidad con documentos impresos.

## Reglas de normalización aplicadas por el ETL (`tools\etl_vfp_to_postgres.py`)

El origen VFP es sucio en varios puntos; el ETL lo normaliza sin perder filas:

| Regla | Detalle |
|---|---|
| **ID canónico de operación** | VFP guarda el ID de compra/venta en dos formas: corta (`0000000005`, en `COMPRAS`/`COMPRECIB`) y compuesta (`20220000000001`, en `CAJA`/`CTASTRAN`). El ETL compone siempre `ejercicio(4) + secuencia(12)` → `2022000000000005`, el mismo formato que genera la API. Así `caja.compra_id` enlaza con `compra.id`. |
| **Numéricos "sin calcular" → NULL** | `caja.monto_bs/tasa/usd` y `transferencia.monto_bs/tasa/usd` venían con `0` cuando no estaban calculados (9.031 de 9.129 y 23.836 de 23.904 filas). Se migran como `NULL` para que la API distinga "sin calcular" de un cero real y aplique la tasa correspondiente. |
| **Centros de costo** | El origen tiene 67 clientes con dos `PPAL='X'`; solo el primero queda como principal (respeta `ux_centro_ppal`). |
| **Moneda nacional colgante** | `DOLARMAIN.ID_MONY2` apunta a `'0003'`, que **no existe** en `TIPOMONY` (solo 0001=USD/BS y 0002=BS). El ETL lo resuelve a la moneda nacional real (`0002`); si no, el FK `compra.moneda_nac_id` rechazaría cualquier operación nueva. La API además valida este dato al guardar Configuración. |
| **Correlativos con las claves de la API** | `999_final_counters.sql` genera **18 semillas** idénticas a las que pide `CorrelativoService`: `COMPRA`/`VENTA`/`CAJA_ID`/`COMPENSACION` por año (al máximo migrado) y `CAJA_RECIBO`/`CLIENTE` con ejercicio `GLOBAL`. Sin ellas un alta chocaría por PK/UNIQUE (el contador arrancaría en 1). |
| **Banco de las transferencias** | VFP no guardaba el banco en `CTASTRAN` (se deducía de la cuenta bancaria) y el ETL dejaba `transferencia.banco_id` vacío: cualquier filtro o extracto por banco devolvía 0 filas. `998_final_datafix.sql` lo deriva de `cuenta_banco(numero_cuenta)` (~23.900 filas, idempotente); `caja.banco_id` se deriva igual. |
| **Fechas vacías** | La única compra sin fecha usa `01-01` de su ejercicio (columnas `NOT NULL`). |
| **Fila en blanco** | `COMPRAS.DBF` tiene 1 registro totalmente vacío que se descarta. |
| **Únicos inexistentes en el origen** | `(tipo_op, operacion_id, nroper)` **no** es único en VFP (~13.400 repeticiones legítimas en recepciones/pagos): el esquema lo indexa sin restricción. |

El ETL omite filas borradas lógicamente (`DELETED`), igual que `SET DELETED ON` de VFP.

## Decisiones de esquema

| Decisión | Motivo |
|---|---|
| **Dominios con `VARCHAR` + `CHECK`, no enums nativos** | `estado`, `caja.origen` y `transferencia.origen` eran `CREATE TYPE ... AS ENUM`. Npgsql envía los valores como parámetros `VARCHAR` y PostgreSQL **no** los convierte implícitamente a enum (error `42804` al insertar), mientras que los literales del ETF sí entraban. Con `VARCHAR` + `CHECK` se conserva la validez de dominio y el modelo C# (propiedades `string`) encaja sin fricción. |
| **Sin FK de `caja`/`transferencia` a `compra`/`venta`** | 490 referencias históricas del VFP quedan colgadas (operaciones borradas en origen): preservarlas sin romper la carga. |
| **`correlativo.ejercicio VARCHAR(6)`** | Admite el año (`2026`) y la marca global `GLOBAL` que usa la API. |
