# Monrys Capital — Sistema de Pagos (SPA)

Frontend Angular 20 (standalone components) del sistema de pagos de casa de cambio.
Migración desde VFP. API .NET 9 en `http://localhost:5100`.

## Requisitos

- Node 24 + npm
- API .NET 9 corriendo en `http://localhost:5100` (modo demo SQLite; `/api/health` reporta modo)

## Desarrollo

```bash
cd web
npm install
npm start          # ng serve con proxy → http://127.0.0.1:4200
```

- `proxy.conf.json` envía `/api` → `http://localhost:5100` (sin problemas de CORS).
- Login demo: **ADMIN / admin123** (según la API en modo demo).

## Build de producción

```bash
npm run build      # → dist/web (compila sin errores)
```

En producción `environment.apiUrl = 'http://localhost:5100'` (llamada directa);
en dev `environment.development.apiUrl = '/api'` (vía proxy, fileReplacements en angular.json).

## Estructura

```
src/
  app/
    core/
      models.ts               # Tipos de todo el contrato de API
      api.service.ts          # Base HTTP (URL, params, errores → toasts)
      auth.service.ts         # Login JWT en localStorage + /auth/me
      auth.interceptor.ts     # Bearer + redirect /login en 401
      auth.guard.ts           # CanActivateFn
      services/               # 1 servicio por dominio (cliente, caja, tasas…)
    shared/
      toast/                  # Toasts propios (sin lib externas)
      modal/                  # Modal accesible (ESC, backdrop)
      comprobante/            # Recibo imprimible (@media print)
      cliente-autocomplete/   # Autocomplete de clientes
      icon/                   # Íconos SVG inline estilo Lucide (sin emojis)
      pipes/                  # fecha → dd/MM/yyyy · monto → 1.234,56 (es-VE)
    layout/                   # Sidebar colapsable + topbar (usuario, tasa, salir)
    pages/                    # Una pantalla por ruta
  environments/               # environment.ts / environment.development.ts
  styles.scss                 # Tokens --color-* / --font-* + utilitarios (.card, .btn, .table…)
```

## Rutas

| Ruta | Pantalla |
|------|----------|
| `/` | Landing pública |
| `/login` | Sesión (ADMIN/admin123) |
| `/dashboard` | KPIs + estado general + cuadre del día |
| `/clientes` | CRUD, detalle con cuentas bancarias |
| `/operaciones/compras`, `/operaciones/ventas` | Lista + alta + cuotas + comprobante |
| `/caja`, `/caja/cierre` | Movimientos, arqueo, cierre diario |
| `/bancos`, `/transferencias`, `/custodios`, `/tasas` | Tesorería |
| `/reportes/estado-general`, `/reportes/cuadre`, `/reportes/estado-cuenta` | Reportes |
| `/usuarios`, `/configuracion` | Usuarios+roles / Config+auditoría |

## Design system

`design-system/monrys-capital/MASTER.md` + `pages/landing.md` + `pages/dashboard.md`:
navy `#0F172A` / oro `#F59E0B` / acento `#8B5CF6`, IBM Plex Sans + Mono,
minimalismo suizo, hover 150-250 ms, focus visible, `prefers-reduced-motion`,
tablas financieras monoespaciadas alineadas a la derecha. Sin emojis como íconos.

## Ajustar contra la API real

Si un payload difiere del contrato, el cambio se hace en **un solo archivo**:
`src/app/core/services/<dominio>.service.ts` (y `core/models.ts` si cambian los tipos).
La normalización de respuestas flexibles está en `ReporteService.normalizarLista()`.
