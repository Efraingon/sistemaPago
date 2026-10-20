# Migración de datos — VFP6 (C:\Mundodolar) → PostgreSQL

Scripts SQL **generados automáticamente** por `tools\etl_vfp_to_postgres.py` desde los
28 DBF originales. Contienen los datos históricos reales (~199.000 filas).

## Regenerar

```powershell
python tools\etl_vfp_to_postgres.py --datos C:\Mundodolar\datos --out database\migracion
```

Solo usa la librería estándar de Python (sin dependencias). Maneja: cp1252→UTF-8,
fechas VFP (`YYYYMMDD`), datetime VFP (número juliano + ms), lógicos, memos, y omite
filas borradas lógicamente (igual que `SET DELETED ON` de VFP).

## Orden de ejecución (PostgreSQL)

```powershell
psql -U postgres -d monrys -f database\01_schema.sql      # esquema
psql -U postgres -d monrys -f database\03_functions.sql   # funciones/trigger
# …luego, en este orden exacto (por claves foráneas):
Get-ChildItem database\migracion\*.sql | ForEach-Object { psql -U postgres -d monrys -f $_.FullName }
psql -U postgres -d monrys -f database\02_seed.sql        # al FINAL: idempotente (ON CONFLICT DO NOTHING)
```

Los archivos ya están numerados en orden de dependencias de FK
(001 monedas → 022 bitácora → 998 datafix → 999 contadores). El seed va al final porque los datos
migrados tienen prioridad: solo añade roles, permisos y el usuario `ADMIN`.

## Notas de migración

| Origen (VFP) | Destino | Detalle |
|---|---|---|
| TIPOMONY | tipo_moneda | 2 monedas: 0001 USD, 0002 BS |
| DOLARMAIN | configuracion | 1 fila (datos de la empresa + tasas activas) |
| CLIENTES | cliente | `persona`: 1=natural, 2=jurídica (se conserva) |
| CENTROSCLIE | centro_costo | `principal` = (PPAL='X'); 67 clientes venían con doble 'X' y solo se conserva el primero |
| COMPRECIB + VENRECIB | operacion_recepcion | `tipo_op`='C'/'V'; `operacion_id` = `ejercicio` + ID_COMPRA/ID_VENTA (ID canónico de 16 dígitos) |
| COMPAGAR + VENPAGAR | operacion_pago | igual que arriba |
| CAJA | caja | `ANULADO`→estado='ANULADO' + `anulado_en`=FECHANUL; `monto_bs`/`tasa`/`usd` en 0 → `NULL` |
| CTASTRAN | transferencia | ídem (incluye la misma normalización de `compra_id`/`venta_id` y de numéricos) |
| CONODOL/CONONAC | denominacion | 0 filas en origen (tablas vacías) |
| USUARIOS | usuario | **Las claves VFP son texto plano: NO se migran.** Cuentas quedan `activo=FALSE` con hash placeholder; crear claves nuevas (el seed define `ADMIN/admin123`). |
| VER_ERROR | log_error | no migrado (log de errores del VFP, prescindible) |
| BITACORA | bitacora | FECHA datetime VFP = JDN + ms (convertido a timestamp) |

`999_final_counters.sql` genera **18 semillas** de `correlativo (tabla, ejercicio, ultimo)`
con las claves **exactas** que pide la API (`CorrelativoService`):

- `COMPRA`, `VENTA`, `CAJA_ID`, `COMPENSACION` → por año (2022-2025), al máximo migrado;
- `CAJA_RECIBO`, `CLIENTE` → ejercicio `GLOBAL` (recibo y id de cliente son globales).

Sin esas semillas el contador arrancaría en 1 y un alta chocaría por PK/UNIQUE contra
los datos migrados (p.ej. `cliente '0000000001'`, `caja 2022 id 1`, recibo `...0001`).

`998_final_datafix.sql` corrige un hueco del origen: en VFP el banco **no** se guardaba
en `CTASTRAN` (se deducía de la cuenta bancaria), así que `transferencia.banco_id`
quedaba vacío y cualquier filtro/extracto por banco devolvía 0 filas. El script deriva
el banco desde `cuenta_banco(numero_cuenta)` (~23.900 transferencias; también `caja`).
Es idempotente: solo toca filas sin banco.

## Validación post-migración

```sql
SELECT COUNT(*) FROM bitacora;      -- 54.845
SELECT COUNT(*) FROM caja;          -- 9.129
SELECT COUNT(*) FROM compra;        -- 8.859
SELECT COUNT(*) FROM venta;         -- 2.234
SELECT COUNT(*) FROM transferencia; -- 23.904
SELECT COUNT(*) FROM transferencia WHERE banco_id IS NOT NULL; -- 23.901 (998 datafix)
-- invariantes del negocio (03_functions.sql):
SELECT * FROM fn_invariantes_caja();   -- custodio: Σabonos − Σretiros = saldo
```
