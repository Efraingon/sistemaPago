# 🏦 Sistema de Pagos Monrys Capital

> Reingeniería completa del sistema administrativo **MONRYS CAPITAL LLC** (casa de cambio / operador de divisas en Venezuela) — migración de **Visual FoxPro 6** a una stack web moderna (Angular 20 + .NET 9 + PostgreSQL/SQLite).

---

## 📋 Tabla de Contenidos

- [Descripción General](#descripción-general)
- [Módulos del Sistema](#módulos-del-sistema)
- [Objetivo para Clientes y Usuarios](#objetivo-para-clientes-y-usuarios)
- [Stack Tecnológico](#stack-tecnológico)
- [Estructura de Directorios](#estructura-de-directorios)
- [Requisitos de Instalación](#requisitos-de-instalación)
- [Cómo Ejecutar](#cómo-ejecutar)
- [Seguridad](#seguridad)
- [Migración de Datos](#migración-de-datos)
- [Credenciales de Demo](#credenciales-de-demo)

---

## 📖 Descripción General

**Sistema de Pagos Monrys Capital** es una plataforma web completa para la gestión de operaciones de una casa de cambio / operador de divisas. El sistema fue originalmente desarrollado en **Visual FoxPro 6** y ha sido migrado completamente a una arquitectura web moderna, conservando todas las reglas de negocio del legado y agregando mejoras de seguridad, usabilidad y trazabilidad.

El sistema permite gestionar:
- ✅ Compras y ventas de divisas con control de tasas
- ✅ Movimientos de caja (ingresos y egresos)
- ✅ Transferencias bancarias
- ✅ Custodia de divisas de clientes
- ✅ Gestión de clientes y bancos
- ✅ Reportes y comprobantes imprimibles
- ✅ Auditoría de todas las transacciones

---

## 🧩 Módulos del Sistema

| Módulo | Descripción | Características principales |
|--------|-------------|------------------------------|
| **Dashboard** | Panel principal de control | KPIs, tasas del día, resumen de operaciones |
| **Clientes** | Directorio de clientes | Alta, edición, búsqueda, ficha de cliente, estado de cuenta |
| **Compras** | Operaciones de compra de divisas | Registro, pagos, cuotas, control de saldo pendiente |
| **Ventas** | Operaciones de venta de divisas | Registro, cobros, cuotas, control de saldo pendiente |
| **Caja** | Libro de caja | Ingresos/egresos, codificación, arqueo, cierre diario, comprobantes imprimibles |
| **Cierre de Caja** | Cierre de caja por fecha | Consolidado, anulaciones con motivo |
| **Bancos** | Gestión de entidades bancarias | Cuentas, saldos, débitos, créditos |
| **Transferencias** | Movimientos bancarios | Depósitos, retiros, transferencias nacionales/internacionales |
| **Custodios** | Custodia de divisas de clientes | Depósitos, retiros, saldos por cliente y moneda |
| **Tasas** | Gestión de tasas de cambio | Tasa activa, compra, venta; calendario histórico |
| **Reportes** | Centro de reportes | Estado general, cuadre de caja, estado de cuenta, comprobantes, exportación CSV/PDF |
| **Usuarios** | Gestión de usuarios | CRUD de usuarios, roles y permisos |
| **Configuración** | Configuración del sistema | Datos de empresa, comprobantes, auditoría de cambios |

---

## 🎯 Objetivo para Clientes y Usuarios

### Para el Usuario Administrativo (casa de cambio)

- **Operar eficientemente**: Registrar compras y ventas de divisas con cálculo automático de montos en Bs según la tasa aplicada.
- **Control de caja**: Llevar un registro detallado de ingresos y egresos con códigos de codificación, arqueo y cierre diario.
- **Custodia de clientes**: Administrar depósitos y retiros de divisas en custodia para cada cliente, con saldo disponible actualizado en tiempo real.
- **Reportes gerenciales**: Generar reportes de estado general, cuadre de caja, estado de cuenta por cliente, con opción de impresión y exportación a CSV.
- **Trazabilidad**: Cada movimiento queda registrado con auditoría completa (usuario, fecha, motivo de anulación, etc.).

### Para el Cliente Final (usuario del negocio)

- **Transparencia**: Acceso a estados de cuenta detallados con movimientos, saldos y comprobantes.
- **Control de custodia**: Conocer en cualquier momento el saldo disponible de sus divisas en custodia.
- **Comprobantes imprimibles**: Recibir comprobantes de cada transacción (compra, venta, recibo de caja, ingreso, entrega, recepción).
- **Seguridad**: Acceso autenticado con usuario y contraseña, roles y permisos definidos.

---

## 🛠 Stack Tecnológico

| Capa | Tecnología | Versión | Propósito |
|------|------------|---------|-----------|
| **Frontend** | Angular (SPA) | 20.3 | Interfaz de usuario, landing pública, 13 módulos del sistema |
| **Backend** | ASP.NET Core Web API | .NET 9 | Servicios de negocio, autenticación JWT, CRUDs, reportes |
| **Base de datos** | PostgreSQL / SQLite | 18 / — | Almacenamiento de datos (PostgreSQL en producción, SQLite en demo) |
| **ORM** | Entity Framework Core | 9.0 | Mapeo objeto-relacional |
| **Autenticación** | JWT + PBKDF2 | — | Tokens de acceso, hash de contraseñas |
| **Documentación API** | Swagger/OpenAPI | 6.6 | Documentación y pruebas de endpoints |
| **Pruebas E2E** | Playwright | 1.64 | Pruebas de extremo a extremo en navegador |
| **Design System** | UI/UX Pro Max | — | Tokens de diseño, paleta navy/oro, tipografía IBM Plex |
| **ETL (migración)** | Python (stdlib) | — | Migración de datos desde DBF de Visual FoxPro |

### Patrones y prácticas aplicadas
- **Arquitectura en capas**: Controllers → Services → Domain → Data (EF Core)
- **DTOs**: Objetos de transferencia de datos para capa de presentación
- **JWT Bearer**: Autenticación stateless con expiración de tokens
- **Soft-delete**: Anulaciones con motivo, sin borrado físico
- **Auditoría automática**: Bitácora en toda mutación de datos
- **Formularios reactivos**: Validación en frontend con Angular Forms
- **CSS variables + Design System**: Tokens de diseño centralizados

---

## 📁 Estructura de Directorios

```
sistemaPago/
├── README.md                      # Documentación principal
├── api/                           # Backend .NET 9 Web API
│   ├── Controllers/               # Controladores de API (12 endpoints)
│   ├── Services/                  # Servicios de negocio (16 servicios)
│   ├── Data/                      # Contexto EF Core y configuración BD
│   ├── Domain/                    # Entidades de dominio
│   ├── Dtos/                      # Objetos de transferencia de datos
│   ├── appsettings.json           # Configuración (conexión BD, JWT)
│   └── Monrys.Api.csproj          # Proyecto de API
├── web/                           # Frontend Angular 20
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/              # Servicios, modelos, autenticación, API
│   │   │   ├── layout/            # Layout principal (sidebar, topbar)
│   │   │   ├── pages/             # 13 módulos del sistema
│   │   │   │   ├── bancos, caja, clientes, configuracion, custodios
│   │   │   │   ├── dashboard, landing, login, operaciones, reportes
│   │   │   │   ├── tasas, transferencias, usuarios
│   │   │   └── shared/            # Componentes compartidos (comprobante, icon, modal, etc.)
│   │   │   └── app.routes.ts      # Definición de rutas
│   │   └── styles.scss            # Estilos globales y diseño
│   ├── package.json               # Dependencias de Node.js
│   ├── angular.json               # Configuración de Angular CLI
│   └── proxy.conf.json            # Proxy /api → http://localhost:5100
├── database/                      # Scripts de base de datos
│   ├── 01_schema.sql              # Esquema (28 tablas migradas)
│   ├── 02_seed.sql                # Datos base + roles/permisos
│   ├── 03_functions.sql           # Funciones y triggers (correlativos, custodio, cierre)
│   └── migracion/                 # Scripts ETL generados (134,391 filas)
├── docs/analisis/                 # Análisis técnico de reingeniería
│   ├── 00-analisis-tecnico.md
│   ├── 01-estructura-datos.md
│   ├── 02-menu-flujo-y-programas.md
│   ├── 03-reglas-negocio-formularios.md
│   └── 04-reportes.md
├── design-system/monrys-capital/  # Design system (UI/UX Pro Max)
│   ├── MASTER.md
│   ├── pages/landing.md
│   └── pages/dashboard.md
└── tools/                         # Herramientas ETL y utilidades
    └── etl_vfp_to_postgres.py     # Migración de DBF a PostgreSQL
```

---

## 📦 Requisitos de Instalación

### Software necesario

| Componente | Versión mínima | Notas |
|------------|----------------|-------|
| .NET SDK | 9.0 | Para ejecutar la API |
| Node.js | 18+ | Para el frontend Angular |
| npm | 9+ | Gestor de paquetes de Node |
| PostgreSQL | 18 | Base de datos de producción |
| SQLite | — | Incluido en .NET (modo demo) |
| Git | 2+ | Control de versiones |

### Variables de entorno

Crear un archivo `.env` o configurar las variables del sistema:

```bash
# Base de datos
Database__Provider=sqlite          # o "postgres" para producción
ConnectionStrings__Monrys=...      # Cadena de conexión (solo si es postgres)

# API
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5100
```

---

## 🚀 Cómo Ejecutar

### Opción 1: Modo Demo (SQLite — recomendado para pruebas)

```powershell
# 1. Ejecutar la API (puerto 5100)
$env:Database__Provider = "sqlite"
dotnet run --project api --urls http://localhost:5100

# 2. Ejecutar el frontend (puerto 4200)
cd web
npm start
```

Abrir **http://localhost:4200** → Landing → *Iniciar sesión* → `ADMIN` / `admin123`.

Swagger de la API: **http://localhost:5100/swagger**

### Opción 2: Con PostgreSQL (datos reales)

```powershell
# 1) Crear la base de datos en UTF-8
psql -U postgres -d postgres -c "CREATE DATABASE monrys ENCODING 'UTF8' TEMPLATE template0 LOCALE_PROVIDER icu LOCALE 'und';"

# 2) Cargar esquema, funciones y datos migrados (orden importante)
psql -U postgres -d monrys -f database\01_schema.sql
psql -U postgres -d monrys -f database\03_functions.sql
Get-ChildItem database\migracion\*.sql | ForEach-Object { psql -U postgres -d monrys -f $_.FullName }
psql -U postgres -d monrys -f database\02_seed.sql

# 3) Ejecutar la API
dotnet run --project api
```

### Migración de datos desde Visual FoxPro

El proyecto incluye una herramienta ETL en Python que migra los datos originales (`.DBF`) desde `C:\Mundodolar\datos\`:

```powershell
python tools\etl_vfp_to_postgres.py
```

Esto genera los archivos SQL en `database\migracion\` listos para importar a PostgreSQL.

---

## 🔐 Seguridad

| Característica | Implementación | Beneficio |
|----------------|----------------|-----------|
| Hash de contraseñas | PBKDF2 | Las contraseñas no se guardan en texto plano |
| Autenticación | JWT (JSON Web Token) | Sesiones stateless seguras |
| Autorización | Roles y permisos declarativos | Control de acceso por módulo y acción |
| Auditoría | Bitácora automática | Registro de todas las mutaciones con usuario y fecha |
| Anulaciones | Soft-delete con motivo | No hay borrado físico, todo queda trazable |
| CORS | Configurado en API | Acceso controlado desde el frontend |

---

## 📊 Migración de Datos (ETL)

Se migraron **134,391 filas** desde el sistema original de Visual FoxPro (43 archivos `.DBF`):

| Tabla | Filas migradas |
|-------|---------------|
| Clientes | 393 |
| Compras | 8,859 |
| Ventas | 2,234 |
| Caja | 9,129 |
| Transferencias | 23,904 |
| Compensaciones (custodio) | 8,265 |
| Custodios | 195 |
| Bitácora / Auditoría | 54,845 |

**Total con seed**: 134,479 filas.

Reglas aplicadas en la migración:
- Conversión cp1252 → UTF-8
- Fechas VFP (JDN + milisegundos) → ISO 8601
- Omisión de registros borrados lógicamente (`SET DELETED ON`)
- ID canónico de operación: `ejercicio(4) + secuencia(12)`
- Monedas y tasas conservadas con precisión decimal

---

## 🔑 Credenciales de Demo

| Usuario | Clave | Rol | Permisos |
|---------|-------|-----|----------|
| `ADMIN` | `admin123` | Administrador | Todos los módulos y reportes |

> ⚠️ Cambiar la contraseña en el primer ingreso.

---

## 📄 Licencia

Este proyecto es de uso interno para **MONRYS CAPITAL LLC**.

---

## 🤝 Contribución

1. Fork el repositorio
2. Crea una rama para tu característica (`git checkout -b feature/nueva-funcionalidad`)
3. Commit tus cambios (`git commit -m 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abre un Pull Request

---

## 📞 Soporte

Para consultas sobre el sistema, contactar al administrador de sistemas de MONRYS CAPITAL LLC.

---

**Última actualización**: Octubre 2026  
**Versión**: 1.0.0 — Reingeniería VFP → Web
