# 02 – Punto de entrada, menú principal y programas generales (VFP6 → Angular/.NET/PostgreSQL)

> Análisis de reingeniería inversa del sistema **Monrroy / Mundodolar** ("Sistema Administrativo MONRYS CAPITAL LLC").
> Fuentes analizadas: `C:\Mundodolar\Monrroy.pjx`, `setting.prg`, `inifiles.prg`, `funcion1.prg`, `control_de_error.prg`, `config.fpw`, `MENUPRINC.MPR` (menú real), `MENUPRIC.MPR` (menú obsoleto), `a.PRG`, `actualiza.bat`, `login.scx`, `pantallaini.scx`, `conosmon.scx`, `datos\*.PRG` y `datos\data1.dbc`.
> Encoding original: cp1252. Todas las rutas en el código son absolutas y hardcoded (`C:\MUNDODOLAR\...`).

---

## 1. Punto de entrada real

### 1.1 Programa principal (según `Monrroy.pjx`)
El `.pjx` (archivo de proyecto VFP, 112 registros) marca como **MAINPROG=T** al programa **`SETTING.PRG`** (KEY=`SETTING`, tipo `P`). El ejecutable compilado es `monrroy.exe` (~2 MB, hay backup `monrroy_old_01042025.exe`).

Contenido del proyecto (tipos):
| Tipo | Significado | Miembros |
|---|---|---|
| `P` | Programas | `SETTING` (**principal**), `INIFILES`, `FUNCION1`, `CONTROL_DE_ERROR` |
| `M` | Menú | `MENUPRINC` (el menú real) |
| `T` | Otro/archivo de config | `CONFIG` (= `programas\config.fpw`) |
| `K` | Formularios (.SCX) | ~60 formularios: `LOGIN`, `PANTALLAINI`, `PLANTILLA`, `PRESENTACION`, `COMPRAS_NEW`, `VENTAS_NEW`, `CUSTODIOS`, `RECIBOS`, `TRANSFERENCIAS`, `CONSOLIDADO`, `SALDOS`, `MOVIMIENTOS`, `MODIFICA_TASAS`, `TASACALENDARIO`, `CONFIGURACION`, `RESPALDOS`, `INGRESOEGRESOSINFORME`, etc. |
| `x` | Archivos de texto/utilitarios | `HARROW`, `BUZON`, `PRINT`, `CLOSE`, `DELETE`, `AGENDA`, `CALENDARIO1`, `BACKUP DISK`, wizards… |
| `d` | Datos | `DATA1` (contenedor `.dbc`) — marcado `EXCLUDE=T`, no se compila en el EXE |

### 1.2 `a.PRG` — no es el arranque
Script corto de utilería (inicialización manual de entorno): `CLOSE ALL`, `SET DATE DMY`, `SET EXCLUSIVE OFF`, `SET DELETED ON`, `SET EXACT ON`, `CD c:\mundodolar`. **No es invocado por el sistema** (no hay `DO a.prg` en el proyecto ni en el menú).

### 1.3 `MENUPRINC.MPR` vs `MENUPRIC.MPR`
- **`MENUPRINC.MPR` (61 KB, generado por GENMENU el 01/05/2026)** → **es el menú usado**: `setting.prg` ejecuta `DO menuprinc.mpr`.
- `MENUPRIC.MPR` (4.6 KB, 08/03/2019) → menú **obsoleto/no referenciado** (única coincidencia de texto: él mismo). Estructura vieja: *Archivos / Transacciones / Consultas / Reportes / Salir*. Se puede descartar en la migración.
- `menuprinc.MNT` (memo del menú) no aporta código adicional al `.MPR` (todo el código de los PROCs de menú está generado en el `.MPR`).

---

## 2. Secuencia de arranque (boot)

Ejecutable `monrroy.exe` → VFP carga `CONFIG.FPW` → `SETTING.PRG`:

| # | Acción | Detalle / efecto |
|---|---|---|
| 0 | `CONFIG.FPW` (`programas\config.fpw`) | `SCREEN=ON`, `TITLE=SISTEMA ADMINISTRATIVO MONRYS CAPITAL LLC`, `sysmenu=off`, `RESOURCE=OFF`, `delete=on`, `talk=off`, `SAFETY=off`, `optimize=ON`, `console=off` |
| 1 | Entorno | `SET EXCLUSIVE OFF`, `SET DELETE ON`, `SET DATE TO BRITISH` (dd/mm/aaaa), `SET CENTURY ON`, `SET MULTILOCKS ON`, `SET SAFETY OFF`, `SET REPROCESS TO -2`, `SET CLOCK STATUS`, status bar ON |
| 2 | **Gate de licencia 1** | Si NO existe `c:\Mundodolar\temp\rutaservertemp.dbf` → "El producto no esta disponible" y sale. (Este DBF de 1 registro funciona como *INI + licencia*.) |
| 3 | Directorios | `CD c:\Mundodolar\`, crea si faltan `txt`, `pdf`, `excel`, `Respaldos` |
| 4 | Librerías | `SET PROCEDURE TO programas\funcion1, inifiles, control_de_error ADDITIVE` |
| 5 | `DO inifiles` | Declara ~60 variables **PUBLIC**, inicializa valores por defecto y `DO temporales` (copia `tempsegu\*.dbf|*.cdx|*.fpt` → `temp\` como plantillas limpias) |
| 6 | Rutas desde INI | Lee `rutaservertemp`: `ruta_data`, `ruta_form`, `ruta_repo`, `ruta_temp`, `ult_usu` (= `pid_usuario`), `ruta_cont`. Si alguna está vacía → "No se han encontrado las rutas del servidor" y sale |
| 7 | Config de reportes | Abre `configrepor` → `a_pdf/a_pdfpreg/a_excel/a_excelpreg` (envío automático de reportes a PDF/Excel) |
| 8 | Parámetros de empresa | Abre `permisos` (order `formaobj`) y `dolarmain` → `p_agente_ret = dolarmain.agente_ret`, `p_nomempres = dolarmain.nombclie` |
| 9 | **Gate de licencia 2** | `dolarmain.nrocliente != 23` → "Usuario no autorizado" y sale |
| 10 | **Gate de licencia 3 (versión)** | `dolarmain.uversion != ultimaversion` (`"05/01/2026"` hardcodeado en setting.prg) → "Debe ejecutar la actualizacion del sistema" y sale |
| 11 | **Gate de licencia 4 (cliente)** | `ALLTRIM(dolarmain.nombclie) != "MONRYS CAPITAL LLC"` → "Empresa no autorizada" y sale. **Software amarrado a un único cliente** |
| 12 | Splash + Login | `DO FORM formas\presentacion` (splash) y `DO FORM formas\login` (bloqueante, modal) |
| 13 | Login OK | `IF !EMPTY(pclave)` → `DO FORM \Mundodolar\formas\pantallaini` (pantalla principal de fondo). Si no → `DO salida` |
| 14 | Bucle principal | `DO WHILE .T.` → `ON ERROR DO programas\control_de_error WITH ERROR(), MESSAGE(), MESSAGE(1), PROGRAM(), LINENO()` → `DO menuprinc.mpr` → `READ EVENTS` |

**Actualización del sistema**: `actualiza.bat` copia desde el servidor de red `\\192.168.0.110\Mundodolar\En_usuarios\` el `monrroy.exe`, `Reportes\*.*`, `temp\` y `tempsegu\` hacia `C:\mundodolar\`. La "versión" se controla con `dolarmain.uversion` vs `ultimaversion`.

### 2.1 Login (`formas\login.scx`) — reglas
- Combo de usuarios con `SET FILTER TO !usuarios.bloqueo` (usuarios bloqueados no aparecen).
- Validación de clave: `SEEK ALLTRIM(pid_usuario)+ALLTRIM(clave)` sobre tag `idclave` de `usuarios` → **claves en texto plano** (sin hash), campo `usuarios.claveusu` CHAR(10).
- Al aceptar setea las globales: `pclave`, `nombreusuario`, `pacceso = ALLTRIM(usuarios.acceso)`, `p_tipousu = usuarios.tipo` (**1 = Administrador**), `pid_usuario`, `p_rutapant`, `fecha_version`, `historico_finanzas`.
- Guarda el último usuario en `temp\rutaservertemp.ult_usu` **y** `tempsegu\rutaservertemp.ult_usu`.
- Registra en bitácora `paccion = "ENTRO AL SISTEMA"`; al salir `"SALIO DEL SISTEMA"` + `usuarios.activo = .F.` y `CANCEL`.
- `pantallaini.scx`: pantalla de fondo (usuario en sesión, ruta de datos, empresa, fecha/hora). `ON KEY LABEL F5 DO salida` (salida rápida). Un botón reabre el login.

---

## 3. Estructura completa del menú (mapa de módulos)

Barras superiores (`MENUPRINC.MPR`): **CONFIGURACIÓN** (Alt+C), **REGISTRO DE CLIENTES** (Alt+R), **REGISTRO DE OPERACIONES** (Alt+R), **TESORERÍA** (Alt+T), **RESPALDOS** (Alt+R), **SALIR** (Alt+S). Nota: tres pads comparten Alt+R (defecto heredado).

| Menú / Submenú | Opción | Archivo invocado | Permiso (`permisos`) | Propósito |
|---|---|---|---|---|
| Configuración ▸ Usuario | Nuevo | `DO FORM nuevousuario` | NUEVOUSUARIO | Alta de usuarios |
| Configuración ▸ Usuario | Control de usuarios | `DO FORM modiuario` | MODIUARIO | Edición/baja, genera matriz de permisos |
| Configuración ▸ Usuario | Cambio de clave | `DO FORM cambiarclave` | — | Cambio de clave propio |
| Configuración ▸ Usuario | Bitácora | `DO FORM ver_bitacora` | VER_BITACORA | Consulta de auditoría (→ `ver_filtbitac`, `ver_detabitac`) |
| Configuración | Configuración | `DO FORM configuracion` | **Solo p_tipousu=1 (Admin)** | Parámetros/`rutaservertemp`; si cambia rutas fuerza salida y reinicio |
| Configuración | Tasa activa | `DO FORM modifica_tasas` | — | Mantiene tasas del día (verifica con `tasacalendario`) |
| Configuración | Salir | Confirmación → `DO salida` | — | Cierra sesión |
| Registro de clientes | Nuevo | `DO FORM CLIEINGRESO` | CLIEINGRESO | Alta de clientes (persona natural 1 / jurídica 2) |
| Registro de clientes | Modificar / Eliminar | `DO FORM CLIEMODIFICA` | CLIEMODIFICA | Edición/baja de clientes |
| Registro de clientes | Consultar | `DO FORM CLIECONSULT` | CLIECONSULT | Ficha y cuenta del cliente (abre `comprasclie`, `ventasclie`, `nuevorecibe`, `nuevoentrega`, `nuevocobro`, `nuevopago`, `ctasclie`, `custodios`) |
| Registro de clientes | Listado | `DO listadoclie` (PRG de funcion1) | CLIECONSULT | Reporte `Reportes\listaclie.frx` (contadores natural/jurídico) |
| Registro de operaciones | Compras | `DO FORM COMPRAS_NEW` | COMPRAS_NEW | **Compra de divisas** (previa validación de divisa principal+nacional en `tipomony`) |
| Registro de operaciones | Ventas | `DO FORM ventas_new` | VENTAS_NEW | **Venta de divisas** (misma validación) |
| Registro de operaciones | Recepción / Cobros | `DO FORM CLIECONSULT` | CLIECONSULT | Entra por la ficha del cliente (cobros `nuevorecibe`/`nuevocobro`) |
| Registro de operaciones | Entrega / Pagos | `DO FORM CLIECONSULT` | CLIECONSULT | Entra por la ficha del cliente (entregas `nuevoentrega`/`nuevopago`) |
| Registro de operaciones | Reportes de operaciones | `DO FORM operaciones` | OPERACIONES | Reportes de compras/ventas (→ `movimcriterios`, FRX `opercomp`/`opervent`) |
| Registro de operaciones | Consolidado actual | `DO FORM consolidado` | CONSOLIDADO | Consolidado de operaciones a hoy (`fal1/fal2` vacíos) |
| Registro de operaciones | Consolidado fecha de corte | `DO FORM fecha_de_corte` + `DO FORM consolidado` | CONSOLIDADO | Consolidado a una fecha de corte |
| Tesorería ▸ Entidades bancarias | Nueva | `DO FORM nuevobanco` (`q_hace="ingresa"`) | NUEVOBANCO | Alta de banco/entidad |
| Tesorería ▸ Entidades bancarias | Modificar / Eliminar | `DO FORM selebancos` (`q_hace="ingresa"`) | SELEBANCOS | Selector de banco para edición |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Nueva | `DO FORM ctasbanco` | CTASBANCO | Nueva cuenta bancaria (nac./intl.) |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Eliminar | `DO FORM eliminacuenta` | ELIMINACUENTA | Baja de cuenta bancaria |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Abonos / Débitos cuenta nacional | `DO FORM bancoscredi` | BANCOSCREDI | Movimientos en cuentas nacionales (Bs.) |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Abonos / Débitos cuenta internacional | `DO FORM bancoscredi2` | BANCOSCREDI2 | Movimientos en cuentas internacionales (USD) |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Custodios | `DO FORM custodiosb` | CUSTODIOSB | Custodia/efectivo por cliente en banco |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Transferencias ▸ Nacionales mismo titular | `DO FORM transferencias` con `PUBLIC paginac=1` | TRANSFERENCIAS | Transferencia nacional, mismo titular |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Transferencias ▸ Nacionales de terceros | `paginac=2` | TRANSFERENCIAS | Transferencia nacional a terceros |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Transferencias ▸ Internacionales mismo titular | `paginac=3` | TRANSFERENCIAS | Transferencia internacional, mismo titular |
| Tesorería ▸ Entidades bancarias ▸ Cuentas | Transferencias ▸ Internacionales de terceros | `paginac=4` | TRANSFERENCIAS | Transferencia internacional a terceros |
| Tesorería ▸ Entidades bancarias | Saldos | `DO FORM fecha_de_corte` + `DO FORM saldos` | *ver nota bug* | Saldos por banco a fecha |
| Tesorería ▸ Entidades bancarias | Movimientos | `DO FORM movimientos` | MOVIMIENTOS | Movimientos bancarios (`ctastran`) |
| Tesorería ▸ Caja | Recibos | `DO FORM recibos` | RECIBOS | Recibos de caja |
| Tesorería ▸ Caja | Reposición | `DO FORM cajareposicion` | CAJAREPOSICION | Reposición de fondo de caja |
| Tesorería ▸ Caja | Custodios | `DO FORM custodios` | CUSTODIOS | Custodia de efectivo en caja (abonos/retiros) |
| Tesorería ▸ Caja | Cierres | *(sin PROC — barra sin acción)* | — | **Deshabilitado/pendiente** |
| Tesorería ▸ Caja | Reportes | `DO FORM cajareport` | CAJAREPORT | Arqueo/cierre de caja y reportes |
| Tesorería ▸ Ingresos | Codificador de ingresos | `DO FORM codificaingresos` | *(sin control)* | Catálogo de conceptos de ingreso → cuenta contable |
| Tesorería ▸ Ingresos | Reportes de ingresos | `DO FORM ingresoegresosinforme` | *(sin control)* | Informes de ingresos/egresos |
| Tesorería ▸ Egresos | Codificador de egresos | `DO FORM codificaegresos` | *(sin control)* | Catálogo de conceptos de egreso → cuenta contable |
| Tesorería ▸ Egresos | Reportes de egresos | `DO FORM ingresoegresosinforme` | *(sin control)* | Mismo formulario que ingresos |
| (pad) | RESPALDOS | `DO FORM respaldos` | *(sin control)* | Backup/restauración de datos |
| (pad) | SALIR | Confirmación (`MESSAGEBOX` 292) → `DO salida` | — | Sale del sistema |

> **Nota bug**: en *Saldos* el `SEEK` de permisos es `SEEK "SALDOS"+"SLADOS"` (typo) → el permiso real `SALDOS` nunca se encuentra y, por la regla *fail-open* (ver §4), **siempre se permite el acceso**.
> **Reportes** no se invocan con `REPORT FORM` desde el menú; todos los reportes (`.FRX/.FRT` en `C:\Mundodolar\Reportes\`, ~65 reportes: `recibo`, `cobros`, `pagos`, `COMPRA`, `VENTA`, `consolidado`, `estadogeneral`, `ver_custodios*`, `ingresocaja`, `ingresobanco`, `cuadrebs/cuadredv`, `fichaclie`, `listaclie`…) se disparan desde los formularios, con exportación opcional a PDF vía **XFRX** (`XFRX.APP` + `ZLIB.DLL`, HNDLIB.DLL) o Excel.

---

## 4. Control de permisos (patrón repetido en todo el menú)

Cada PROC de menú sigue exactamente este molde:

```foxpro
IF p_tipousu != 1                    && 1 = Administrador => acceso total
   SELECT permisos
   SEEK "<CLAVE_FORM>" + "<CLAVE_FORM>"
   IF !EOF()
      x_recno = RECNO()
      IF SUBSTR(pacceso, x_recno, 1) != "S"   && p.e. usuarios.acceso
         x_permitido = .F.
      ELSE
         x_permitido = .T.
      ENDIF
   ELSE
      x_permitido = .T.             && FAIL-OPEN: si la fila no existe, se permite
   ENDIF
ELSE
   x_permitido = .T.
ENDIF
IF x_permitido
   DO FORM ruta_formas + "<form>"
ELSE
   DO avisarpermiso                 && "Usuario no autorizado - Solicite permiso al Administrador"
ENDIF
```

**Modelo de permisos**: `usuarios.acceso` es un *string de bits* CHAR(150); el n-ésimo carácter corresponde al **RECNO() de la tabla `permisos`** (una fila por formulario/objeto, con `formulario` y `objeto`). El formulario `permisar.scx` administra esta matriz.
**Implicación para la migración**: esto debe convertirse en un modelo de **roles/permisos declarativo** (p. ej. tabla `permissions` + `role_permissions` y *guards* en Angular / policies en .NET), no en una cadena posicional frágil (el orden físico de filas define el permiso).

---

## 5. Programas compartidos

### 5.1 `programas\funcion1.prg` (biblioteca de utilidades)
| Procedimiento | Propósito | Regla/nota |
|---|---|---|
| `pak_tabla(nomtabla)` | Abre tabla de `ruta_data` **EXCLUSIVE** y hace `PACK` | Limpieza física de borrados |
| `abrearch(nomtabla, indice)` | Abre tabla de `ruta_data` y `SET ORDER TO TAG indice` | Rutina estándar de apertura (usada en todo el sistema) |
| `abrecont(nomtabla, indice)` | Igual pero sobre `prutacontable` (sistema contable externo) | Integración contable opcional |
| `abreclin(nomtabla, indice)` | Igual pero sobre `ruta_clin` (ruta "clínica"/otra base) | Ruta poco usada/de legado |
| `abrearch_excl(...)` | Igual que `abrearch` pero **EXCLUSIVE** | |
| `cierrarch(nomtabla)` | Cierra el alias si está abierto | |
| `AbrirTemp / CerrarTemp` | Abre tabla temporal de `ruta_temp` **EXCLUSIVE con ZAP** (limpia) | Trabajo con temporales por sesión |
| `AbrirTempsz / CerrarTempsz` | Igual sin ZAP | |
| `bitacora` | **Auditoría**: inserta en `bitacora` fecha(`DATETIME`), `id_usuario`, `tipo`(`ptiponum`), `accion`, `observa` (+ equipo `SYS(0)` y versión), limpia `paccion/pobserva` | Toda acción crítica debe llamarla |
| `expopdf` | Exporta reporte a PDF con **XFRX** (`pdf_ruta` → `C:\Mundodolar\Pdf\<p_nombrepdf>`) | Según flags de `configrepor` |
| `salida` | Cierra sesión: bitácora "SALIO DEL SISTEMA", `usuarios.activo=.F.`, `CLOSE DATABASES ALL`, `CLEAR ALL`, `RELEASE ALL`, `SET SYSMENU TO DEFAULT`, `CANCEL` | |
| `temporales` | Copia plantillas `tempsegu\*.dbf|*.cdx|*.fpt` → `temp\` | "Reinicio limpio" de temporales en cada arranque |
| `avisarpermiso` | MessageBox "Usuario no autorizado… Solicite permiso al Administrador" | |
| `listadoclie` | Crea tabla temporal `temp\listacle`, copia clientes (descripción = razón social o apellidos+nombres), cuenta `t_natural`/`t_juridico`/`t_clientes` y emite `REPORT FORM listaclie` (preview + impresora + PDF opcional) | Único lugar con `REPORT FORM` en PRG |
| `LoadSETUP` | Segundo set de `SET` (idéntico a setting, salvo `SET EXACT OFF`) | Legado |

### 5.2 `programas\inifiles.prg` — estado global
Declara ~60 variables **PUBLIC** e inicializa. Las principales (globo de estado de la sesión, candidatas a *session/context* en Angular o a DTO de sesión en .NET):

- **Sesión/usuario**: `pid_usuario`, `nombreusuario`, `pclave`, `pacceso`, `p_tipousu`, `historico_finanzas`, `plogin`, `p_rutapant`.
- **Rutas**: `ruta_data`, `ruta_formas`, `ruta_report`, `ruta_temp`, `ruta_clin`, `prutacontable`, `diraiz = "C:\MUNDODOLAR"`, `pdf_ruta`.
- **Empresa/parámetros**: `p_nomempres`, `p_agente_ret`, `pempresa`, `p_nombclie`, `p_rifclie`, `p_direclie`, `ultimaversion`, `p_valonacio`, `p_isnc`.
- **Facturación/IVA**: `p_factislr`, `p_nrocontrol`, `p_fechfact`, `p_montneto`, `p_monttot`, `p_montoiva`, `p_tasaiva`.
- **Tablas dinámicas**: `ptablacuentas`, `pmaestcomp`, `pmaestmov`, `ptablaparametros`, `ptablamov`, `ptablacomp`, `tablax`.
- **Interfaz/flujos**: `paccion`, `pobserva`, `ptiponum` (tipo de bitácora; 2 = eventos "anormales"), `msg`, `q_hace` (modo ingresa/modifica), `viene_de`, `p_nameform`, `p_titulo`, `paginac` (página de transferencias).
- **Contabilidad**: `piscontable=.T.`, `contabilidad_activa`, `p_ctacontab`.
- **Fechas de trabajo**: `m.fec1`, `m.fec2`, `fal1`, `fal2` (fechas de corte/consolidado), `ejer`/`ejercc` (ejercicio).
- **Reportes**: `a_pdf`, `a_pdfpreg`, `a_excel`, `a_excelpreg`, `p_nombrepdf`.

Termina con `DO temporales` (restaura las tablas temporales).

### 5.3 `programas\control_de_error.prg` — manejo de errores global
`ON ERROR` del bucle principal. Registra en **`ver_error`** (DBF + FPT): fecha, usuario, nombre y mensaje completo (número, tipo, instrucción, programa, línea, equipo `SYS(0)`, versión), muestra el mismo texto en un MessageBox y termina con `CLOSE DATABASES ALL` + `CANCEL`. **Cualquier error detiene la aplicación** (sin recuperación).

### 5.4 Programas utilitarios en `datos\`
| Programa | Propósito |
|---|---|
| `saldo_custodio.PRG` | **Recalcula el saldo de custodia**: para cada `custodio` (cliente+moneda), suma `compensac.abonos - compensac.retiros` y corrige `custodio.disponibb` si difiere de `custodio.disponible`. Regla de negocio clave: **saldo disponible = abonos − retiros del historial** |
| `carga_centros.PRG` | Crea en `centrosclie` un centro de costo principal (`ppal="X"`) por cada cliente (natural → apellidos; jurídico → razón social) |
| `carga_centros_tablas.PRG` | Rellena `caja.centrocost`, `compras.centrocost` y `ventas.centrocost` con el centro principal del cliente |
| `reindexa.PRG` | Mantenimiento: recorre todos los `.DBF` del directorio y hace `PACK` + `REINDEX` |
| `blanqueo.PRG` | **Peligroso**: recorre todos los `.DBF` y ofrece `ZAP` (borrar todos los registros) uno por uno |

---

## 6. Reglas de negocio detectadas en este alcance

1. **Tasa de cambio**: se guardan tres tasas en `dolarmain`: `TASACTIVA` (vigente), `TASACOMP` (tasa de compra) y `TASAVENTA` (tasa de venta), además de `CALENDTASA` (tasas por fecha: `fecha`, `tasa`, `tasacomp`, `tasaventa`, `nombdia`) mantenido por `tasacalendario.scx` y consultado por `compras_new`/`ventas_new`/`modifica_tasas`. Valores actuales de ejemplo: activa 220, compra 285, venta 300.
2. **Divisas (`tipomony`)**: debe existir **exactamente** una divisa con `principal=.T.` (USD/BS, `ID_MONY=0001`) y una con `nacional=.T.` (Bs., `0002`); el menú de Compras/Ventas valida ambos antes de abrir el formulario ("Debe indicar la divisa principal / nacional"). Cada divisa tiene `montodolar` y `valonacio` (valor en moneda nacional).
3. **Operación de cambio (compras/ventas)**: las tablas `compras` y `ventas` son espejos: `id_mony1`/`id_mony2`, `mtoxd1`/`mtoxd2` (montos en cada divisa), `valonacio`, `cantidad`, y saldos contables `monto_bs`, `pagado_bs`, `saldo_bs`, `recibidodv`, `porecib`. Documento por ejercicio (`ejercc`) + correlativo (`id_compra`/`id_venta`).
4. **Cuentas por cobrar/pagar propias de casa de cambio**: `comprecib`/`venrecib` (lo recibido por compras/ventas, con `porecib`) y `compagar`/`venpagar` (lo a pagar), con `nroper` (número de operación), bancos `bancodebe`/`bancocredi` y cuentas `ctadebe`/`ctacredi` (pago desde/hacia cuenta propia o del cliente), `recibo`, `tipopago`, `saldo`.
5. **Custodio (efectivo bajo custodia de clientes)**: `custodio` (cliente+moneda) con `disponible`/`disponibb`; los movimientos están en `compensac` (`abonos`/`retiros`, con `recitran`, `ctastran`, `centrocost`). Regla: *disponible = Σ abonos − Σ retiros* (ver `saldo_custodio.PRG`). Formularios `custodios` (caja) y `custodiosb` (banco); reportes `ver_custodios*`, `recibecustodio`, `entregacustodio`.
6. **Caja**: `caja` registra todos los movimientos de efectivo con `origen` (código de 2 letras: `IN` ingreso, `EB`/`IB` banco, `EG` egreso, `CO` cobro, `VE` venta, `EN` entrega, `RE` recepción, `C1/C2`, `T5/T6`, `DS`, `DE` — ver `conosmon.scx`), `recibo`, `monto`, `moneda`, `tasa`, `usd`, `fechacier`/`id_cierr` (cierre de caja) y `anulado`/`fechanul`.
7. **Arqueo de caja por denominaciones ("cono monetario")**: `conodol` (denominaciones en USD) y `cononac` (denominaciones en Bs.) con `tipo` (BILLETE/MONEDA), `valor`, `cantidad`; el total es `Σ(valor × cantidad)` y se compara con el teórico del período para el arqueo (`cajareport`, `ingresoegresosinforme`, `conosmon`).
8. **Transferencias**: `ctastran` es el libro mayor de movimientos bancarios: `origtran` (origen), `tipotran` (tipo), `ctastran` (cuenta), `numetran` (número/reference), `benetran` (beneficiario), `monttran`, `id_clie`, `id_compra`/`id_venta`, `tasa`, `usd`, `mtobs`, `decaja` (viene de caja), códigos de ingreso/egreso `id_codi`/`id_code`. Cuatro variantes según nacional/internacional y mismo titular/terceros (`paginac` 1..4).
9. **Codificación contable**: `codiingreso` (conceptos de ingreso → `ctacontab`) y `codiegreso` (conceptos de egreso → `ctacontab`), con catálogo `maestro` de cuentas (plan tipo 4.1.x / 6.1.x). Los movimientos de caja/transferencia se clasifican con `id_codi`/`id_code`.
10. **Centros de costo por cliente**: `centrosclie` (varios por cliente, uno principal `ppal="X"`), volcado a `caja.centrocost`, `compras.centrocost`, `ventas.centrocost`.
11. **Auditoría obligatoria**: `bitacora` (fecha/hora, usuario, acción, observación, equipo, bloqueo) escrita desde `funcion1!bitacora` en alta/entrada/salida; errores globales en `ver_error`.
12. **Licencia por cliente**: triple candado (`nrocliente=23`, `nombclie="MONRYS CAPITAL LLC"`, versión `uversion`) — a eliminar en la migración (reemplazar por tenant/serialización de la nueva app).

---

## 7. Configuración relevante y dependencias

- **INI/Licencia**: `temp\rutaservertemp.dbf` (1 registro): `RUTA_DATOS=C:\MUNDODOLAR\DATOS\`, `RUTA_REPO=C:\MUNDODOLAR\REPORTES\`, `RUTA_TEMPO=C:\MUNDODOLAR\TEMP\`, `RUTA_FORM=C:\MUNDODOLAR\FORMAS\`, `ULT_USU`, `RUTA_CONT=C:\MUNDODOLAR\DATOS\INFOCONT\`. Se replica en `tempsegu\` (plantillas).
- **Directorios**: `\datos` (DBFs), `\formas` (SCX), `\Reportes` (FRX), `\temp` (temporales volátiles), `\tempsegu` (plantillas de temporales), `\pdf`, `\excel`, `\txt`, `\Respaldos`, `\fondosys` (fondos/imágenes), `\graficos`, `\En_Usuarios` (paquete de distribución desde servidor `\\192.168.0.110`).
- **Contenedor de datos**: `datos\data1.dbc` (435 objetos: tablas, índices y campos; **sin stored procedures** — toda la lógica vive en PRG/SCX). Tablas principales fuera del DBC también en `\datos`.
- **Externos**: `XFRX.APP` + `ZLIB.DLL` (PDF), `HNDLIB.DLL` (librería de apoyo), `monrroy.exe` compilado.
- **Alias/tablas usadas por los programas generales**: `usuarios`, `permisos`, `bitacora`, `ver_error`, `dolarmain`, `configrepor`, `rutaservertemp`, `tipomony`, `clientes`, `listacle` (temp), `custodio`, `compensac`, `caja`, `compras`, `ventas`, `ctastran`, `bancos`, `ctas`/`ctasbanco`, `ctasclie`, `conodol`, `cononac`, `calentasa`, `maestro`, `codiingreso`, `codiegreso`, `centrosclie`, `compagar`, `comprecib`, `venpagar`, `venrecib`.

---

## 8. Conclusión del dominio del negocio

**Monrroy/Mundodolar es un sistema administrativo para una CASA DE CAMBIO / OPERADOR DE DIVISAS en Venezuela (MONRYS CAPITAL LLC), con caja, tesorería bancaria y libro contable básico.** Su núcleo es:

1. **Cliente único-multimoneda**: ficha de clientes (natural/jurídica, RIF/cédula) con varias cuentas bancarias (`ctasclie`) y centro de costo.
2. **Operaciones de cambio**: compra y venta de divisas (USD ↔ Bs.) con tasa explícita por operación, saldos en ambas monedas y estados por cobrar/pagar.
3. **Cobros y pagos** de esas operaciones, en efectivo (caja) o desde/hacia cuentas bancarias nacionales o internacionales, propias o de terceros, con transferencias.
4. **Custodia de efectivo de clientes** (abonos/retiros con saldo recalculable) — el cliente deja dinero en custodia y lo retira/entrega.
5. **Tesorería**: bancos, cuentas, movimientos (`ctastran`), saldos y consolidados a fecha.
6. **Caja física** con arqueo por denominaciones ("cono monetario" USD/Bs.) y reportes de arqueo.
7. **Codificación contable** de ingresos/egresos + catálogo de cuentas y puente opcional a un sistema contable externo (`ruta_cont\INFOCONT`).

"custodio" = saldo/movimientos de efectivo bajo custodia del cliente; "ctastran" = libro de transferencias/movimientos bancarios; "compensac" = movimientos (abonos/retiros) que compensan la custodia; "conodol/cononac" = catálogo de denominaciones (billete/moneda) en USD y en Bs. para el arqueo de caja.

### Implicaciones para la migración Angular + .NET + PostgreSQL
- Reemplazar el estado global `PUBLIC` por una **sesión/usuario autenticado** (JWT) + servicios inyectables; `pacceso` → modelo roles/permisos y *route guards*.
- Los "candados" de licencia y la ruta hardcoded → `appsettings.json`/configuración por entorno.
- `bitacora`/`ver_error` → tablas de **auditoría** y **logs** (seriadas, no DBF).
- Numeración de documentos (`id_compra`, `id_venta`, `id_caja`, recibos, `nroper`, `ejercc`) → **secuencias/series** con concurrencia (PostgreSQL sequences + control por ejercicio).
- Reglas monetarias (tasa, `monto_bs`, `usd`, custodia = abonos−retiros, arqueo por denominaciones) → **capa de servicios transaccional** con validación y transacciones ACID (hoy no hay transacciones formales: se escriben DBFs sueltos).
