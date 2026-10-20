# 00 — Análisis Técnico Consolidado: Sistema Monrroy/Mundodolar → Web Stack

> **Objetivo**: reingeniería del sistema VFP6 "Sistema Administrativo MONRYS CAPITAL LLC" (casa de cambio / operador de divisas en Venezuela, USD ↔ Bs) hacia **Angular + .NET (C#) + PostgreSQL**.
> Documentos detallados: [01-estructura-datos](01-estructura-datos.md) · [02-menu-flujo-y-programas](02-menu-flujo-y-programas.md) · [03-reglas-negocio-formularios](03-reglas-negocio-formularios.md) · [04-reportes](04-reportes.md)

## 1. Punto de entrada y arranque

- **Programa principal**: `SETTING.PRG` (marcado MAINPROG en `Monrroy.pjx`), seguido de `INIFILES`, `FUNCION1`, `CONTROL_DE_ERROR` y el menú `MENUPRINC.MPR`.
- Secuencia: config (`CONFIG.FPW`) → gate de licencia (`temp\rutaservertemp.dbf`) → ~60 variables `PUBLIC` (`inifiles`) → validación de cliente amarrado (NROCLIENTE=23, "MONRYS CAPITAL LLC") → splash (`presentacion`) → **login** → pantalla inicial → menú (`READ EVENTS`).
- `MENUPRIC.MPR` está huérfano (2019) → se descarta.

## 2. Módulos del negocio (mapa de navegación del sistema nuevo)

| Barra VFP | Módulo | Pantallas VFP | Equivalente nuevo |
|---|---|---|---|
| Configuración | Seguridad/Config | nuevousuario, modiuario, cambiarclave, ver_bitacora, configuracion, modifica_tasas | `/usuarios`, `/configuracion`, `/tasas` |
| Registro de clientes | Maestro | clieingreso, cliemodifica, clieconsult, listaclie | `/clientes` (CRUD + ficha) |
| **Registro de operaciones** | **Compras/Ventas de divisa** | compras_new, ventas_new, comprasclie(2/_varios), ventasclie(2), comprasrecipagos, ventasrecipagos, operaciones, consolidado, fecha_de_corte | `/operaciones/compras`, `/operaciones/ventas`, `/consolidado` |
| Tesorería — Bancos | Bancos y transferencias | nuevobanco, selebancos, ctasbanco, eliminacuenta, bancoscredi(2), custodiosb, transferencias×4, saldos, movimientos | `/bancos`, `/transferencias`, `/saldos`, `/movimientos` |
| Tesorería — Caja | Punto de caja | recibos, cajareposicion, custodios, cajareport | `/caja` (movimientos, arqueo, cierre) |
| Tesorería — Ing/Egr | Codificación | codificaingresos, codificaegresos, ingresoegresosinforme | `/configuracion/codificacion`, `/caja/informes` |
| Respaldos | Respaldos | respaldos | (en web: exportación/backup programado) |

## 3. Reglas de negocio críticas (las 10 más importantes)

1. **Cálculo monetario**: `MONTO_BS = ROUND(CANTIDAD × TASA, 2)`. Tres tasas globales **no intercambiables**: `TASAVENTA` (default ventas), `TASACOMP` (default compras), `TASACTIVA` (referencia: consolidado/reposición de caja). Historial en `CALENDTASA`.
2. **Correlativos**: `CAJA.ID_CAJA` (10 díg/año), `CAJA.RECIBO` (20 díg global), `COMPENSAC.ID_TRANS` (10 díg/año), `NROPER` por operación — todos `MAX()+1` sin transacción en origen → **sequences en PostgreSQL**.
3. **Custodia de clientes**: `CUSTODIO.DISPONIBLE = Σ COMPENSAC.ABONOS − Σ COMPENSAC.RETIROS` por (cliente, moneda). Entrega/venta **suma** a custodia; recepción/compra **resta**. Anulación = fila reverso.
4. **Cuotas de operaciones**: una COMPRA/VENTA se liquida en partes (`COMPRECIB`/`VENRECIB` = por recibir/cobrar; `COMPAGAR`/`VENPAGAR` = pagos). Al saldar cambian `PAGADO_BS`/`SALDO_BS`/`PORECIB` de la cabecera.
5. **Libro de caja con códigos de ORIGEN** (IN/EG/CO/VE/RE/EN/C1/C2/DE/DS/IB/EB/T5/T6) y **cierre de caja** (`FECHACIER`/`ID_CIERR` — escritor en el EXE, reconstruir).
6. **Transferencias** nacional/internacional × propio/tercero (4 pantallas → 1 pantalla parametrizable), registradas en `CTASTRAN` y opcionalmente vinculadas a caja (`DECAJA`).
7. **Anulaciones**: por bandera en CAJA/CTASTRAN (no borrado); cuotas por DELETE físico + renumeración. Uniformizar a soft-delete con motivo.
8. **Permisos posicionales frágiles**: `USUARIOS.ACCESO` string 150 chars (posición = orden físico en `PERMISOS`), admin (`TIPO=1`) bypass, **fail-open** si falta la fila, bug "SLADOS" → **reemplazar por roles/permisos declarativos + guards**.
9. **Seguridad deficiente**: claves en texto plano, sin doble captura ni aprobación de dos personas → **hash bcrypt/argon2 + JWT**; considerar "aprobación de segundo usuario" para anulaciones (mejora sugerida).
10. **Auditoría**: `BITACORA` (54.8k) + `VER_ERROR` → tablas de auditoría + logging centralizado.

## 4. Inventarios

- **Datos**: 28 tablas (§1 doc 01). Volumen: ~140k registros; 7 tablas grandes (>2.000 filas).
- **Formularios**: ~65 SCX analizados; núcleo: `operaciones` (173 KB), `movimientos`, `cajareport`, `saldos`, `transferencias`.
- **Reportes**: 54 FRX (~45 únicos — muchos son espejos/variantes → unificar). ~10 **transaccionales** (recibo, COMPRA/VENTA, ingresocaja/banco, entreg/recibecustodio) + ~35 **gerenciales** (estadogeneral, consolidado, cuadrebs/cuadredv, detalle*, opercomp/opervent, ver_custodios*). Los FRX no contienen SQL: los cursores los arman los formularios (patrón: tabla temporal → bucle → `REPORT FORM`).

## 5. Huecos detectados (riesgos de migración)

1. **Cierre de caja** (`CAJA.FECHACIER`/`ID_CIERR`): el escritor está compilado en `monrroy.exe` → reconstruir desde `cajareposicion.scx` + comportamiento observable.
2. **Catálogo `RPTCAJA`** (traduce `CAJA.ORIGEN` a texto): no es DBF local → recrear como catálogo estático.
3. **Campos cacheados** (`CUSTODIO.DISPONIBLE`, `BANCOS.SALDO`, `CTAS.SALDO`, saldos de COMPRAS/VENTAS): recalcular al migrar y mantener coherencia transaccional.
4. BD en `datos\` es de **prueba** (3 usuarios, 3 permisos, catálogo de denominaciones vacío) — validar con producción (`En_Usuarios`/respaldos) antes del cutover.

## 6. Arquitectura objetivo propuesta

```
[Angular 20 SPA] ──HTTP/JSON──> [.NET 8 Web API (EF Core + Npgsql)] ──> [PostgreSQL 16]
       │                                   │
       ├─ JWT auth + guards                ├─ Services: Operaciones, Caja, Custodios,
       ├─ Reactive forms                   │  Tesorería, Reportes, Auditoría
       ├─ Módulos: Login, Dashboard,       ├─ Numeradores (sequences), TasaService,
       │  Clientes, Compras/Ventas,        │  CustodiaService (invariantes), CierreCaja
       │  Caja, Bancos, Transferencias,    └─ Auditoría + logging
       │  Custodios, Reportes, Config
       └─ Comprobante imprimible (recibo/ticket) + PDF
```

- **Dominios de la API**: `/api/auth`, `/api/clientes`, `/api/monedas`, `/api/tasas`, `/api/bancos`, `/api/cuentas`, `/api/compras`, `/api/ventas`, `/api/caja`, `/api/transferencias`, `/api/custodios`, `/api/reportes/*`, `/api/usuarios`, `/api/configuracion`, `/api/auditoria`.
- **Endpoints de reportes** (familias del doc 04): estado-general, consolidado, cuadre-caja, caja (ing/egr por rango), estado-cuenta/{cliente}, detalle-transacciones, movimientos-bancarios, custodios, cumplimiento, comprobante/{tipo}/{id}.
- **Diseño UI**: landing + shell con `@ui-ux-pro-max` (design system por industria "fintech/casa de cambio" — anti-patrones: nada de gradientes AI; confianza/institucional) + `@frontend-design` (estética distintiva, tipografía con carácter).

## 7. Plan de ejecución (Pasos)

| Paso | Entregable | Estado |
|---|---|---|
| Análisis | Docs 00–04 | ✅ |
| **A** | `database/`: scripts PostgreSQL (esquema + índices + seed) | ⏳ |
| **B** | `api/`: .NET 8 Web API (entidades, context, servicios, controladores CRUD + lógica de negocio) | ⏳ |
| **C** | `web/`: Angular (login, dashboard, módulos con formularios reactivos, servicios HTTP) | ⏳ |
| Diseño | Landing page + design system, abrir en navegador | ⏳ |
