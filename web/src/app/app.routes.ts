import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { LandingComponent } from './pages/landing/landing.component';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  // Públicas
  { path: '', component: LandingComponent, title: 'Monrys Capital — Casa de cambio' },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent),
    title: 'Iniciar sesión — Monrys Capital',
  },
  // Autenticadas (layout con sidebar + topbar)
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent),
        title: 'Dashboard — Monrys Capital',
      },
      {
        path: 'clientes',
        loadComponent: () => import('./pages/clientes/clientes.component').then((m) => m.ClientesComponent),
        title: 'Clientes — Monrys Capital',
      },
      {
        path: 'operaciones/compras',
        loadComponent: () => import('./pages/operaciones/operaciones.component').then((m) => m.OperacionesComponent),
        data: { tipo: 'compra' },
        title: 'Compras — Monrys Capital',
      },
      {
        path: 'operaciones/ventas',
        loadComponent: () => import('./pages/operaciones/operaciones.component').then((m) => m.OperacionesComponent),
        data: { tipo: 'venta' },
        title: 'Ventas — Monrys Capital',
      },
      {
        path: 'caja',
        loadComponent: () => import('./pages/caja/caja.component').then((m) => m.CajaComponent),
        title: 'Caja — Monrys Capital',
      },
      {
        path: 'caja/cierre',
        loadComponent: () => import('./pages/caja/cierre.component').then((m) => m.CierreComponent),
        title: 'Cierre de caja — Monrys Capital',
      },
      {
        path: 'bancos',
        loadComponent: () => import('./pages/bancos/bancos.component').then((m) => m.BancosComponent),
        title: 'Bancos — Monrys Capital',
      },
      {
        path: 'transferencias',
        loadComponent: () => import('./pages/transferencias/transferencias.component').then((m) => m.TransferenciasComponent),
        title: 'Transferencias — Monrys Capital',
      },
      {
        path: 'custodios',
        loadComponent: () => import('./pages/custodios/custodios.component').then((m) => m.CustodiosComponent),
        title: 'Custodios — Monrys Capital',
      },
      {
        path: 'tasas',
        loadComponent: () => import('./pages/tasas/tasas.component').then((m) => m.TasasComponent),
        title: 'Tasas — Monrys Capital',
      },
      {
        path: 'reportes/centro',
        loadComponent: () => import('./pages/reportes/centro-reportes.component').then((m) => m.CentroReportesComponent),
        title: 'Centro de reportes — Monrys Capital',
      },
      {
        path: 'reportes/informe/:id',
        loadComponent: () => import('./pages/reportes/informe.component').then((m) => m.InformeComponent),
        title: 'Informe — Monrys Capital',
      },
      {
        path: 'reportes/estado-general',
        loadComponent: () => import('./pages/reportes/estado-general.component').then((m) => m.EstadoGeneralComponent),
        title: 'Estado general — Monrys Capital',
      },
      {
        path: 'reportes/cuadre',
        loadComponent: () => import('./pages/reportes/cuadre.component').then((m) => m.CuadreComponent),
        title: 'Cuadre de caja — Monrys Capital',
      },
      {
        path: 'reportes/estado-cuenta',
        loadComponent: () => import('./pages/reportes/estado-cuenta.component').then((m) => m.EstadoCuentaComponent),
        title: 'Estado de cuenta — Monrys Capital',
      },
      {
        path: 'usuarios',
        loadComponent: () => import('./pages/usuarios/usuarios.component').then((m) => m.UsuariosComponent),
        title: 'Usuarios — Monrys Capital',
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./pages/configuracion/configuracion.component').then((m) => m.ConfiguracionComponent),
        title: 'Configuración — Monrys Capital',
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
