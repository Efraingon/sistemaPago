import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { AuthService } from '../core/auth.service';
import { TasaService } from '../core/services/tasas.service';
import { IconComponent } from '../shared/icon/icon.component';
import { FechaPipe, MontoPipe } from '../shared/pipes/pipes';

interface ItemMenu {
  ruta: string;
  etiqueta: string;
  icono: string;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, IconComponent, FechaPipe, MontoPipe],
  template: `
    <div class="shell" [class.colapsado]="colapsado" [class.menu-abierto]="menuAbierto">
      <!-- Overlay móvil -->
      <div class="side-overlay no-print" (click)="menuAbierto = false"></div>

      <!-- Sidebar -->
      <aside class="sidebar no-print" aria-label="Navegación principal">
        <div class="brand">
          <span class="brand-mark" aria-hidden="true">
            <svg viewBox="0 0 32 32" width="26" height="26">
              <rect width="32" height="32" rx="7" fill="#0F172A" stroke="rgba(245,158,11,.45)" />
              <path d="M7 23V9h4l5 8 5-8h4v14h-4v-8l-5 8-5-8v8z" fill="#F59E0B" />
            </svg>
          </span>
          <span class="brand-name">
            <strong>Monrys</strong>
            <em>Capital</em>
          </span>
        </div>

        <nav class="menu">
          <p class="menu-seccion">Operación</p>
          @for (item of menuOperacion; track item.ruta) {
            <a class="menu-item" [routerLink]="item.ruta" routerLinkActive="activo" [routerLinkActiveOptions]="{ exact: item.ruta === '/dashboard' }" (click)="cerrarMenuMovil()">
              <app-icon [name]="item.icono" [size]="18" />
              <span class="menu-label">{{ item.etiqueta }}</span>
            </a>
          }

          <p class="menu-seccion">Tesorería</p>
          @for (item of menuTesoreria; track item.ruta) {
            <a class="menu-item" [routerLink]="item.ruta" routerLinkActive="activo" (click)="cerrarMenuMovil()">
              <app-icon [name]="item.icono" [size]="18" />
              <span class="menu-label">{{ item.etiqueta }}</span>
            </a>
          }

          <p class="menu-seccion">Administración</p>
          @for (item of menuAdmin; track item.ruta) {
            <a class="menu-item" [routerLink]="item.ruta" routerLinkActive="activo" (click)="cerrarMenuMovil()">
              <app-icon [name]="item.icono" [size]="18" />
              <span class="menu-label">{{ item.etiqueta }}</span>
            </a>
          }
        </nav>

        <button type="button" class="collapse-btn" (click)="colapsado = !colapsado" [attr.aria-label]="colapsado ? 'Expandir menú' : 'Colapsar menú'">
          <app-icon [name]="colapsado ? 'chevronRight' : 'chevronLeft'" [size]="16" />
          <span class="menu-label">Colapsar</span>
        </button>
      </aside>

      <!-- Main -->
      <div class="main">
        <header class="topbar no-print">
          <button type="button" class="icon-btn burger" (click)="menuAbierto = !menuAbierto" aria-label="Abrir menú">
            <app-icon name="menu" [size]="18" />
          </button>

          <div class="tasa-chip" title="Tasa del día">
            <span class="tasa-label">Tasa hoy</span>
            @if (tasaCargada) {
              <span class="tasa-valor mono">{{ tasa?.tasaVenta | monto: 2 }} Bs</span>
              <span class="tasa-compra mono">compra {{ tasa?.tasaCompra | monto: 2 }}</span>
            } @else {
              <span class="tasa-valor mono">—</span>
            }
          </div>

          <div class="topbar-right">
            <span class="fecha-hoy mono">{{ hoy | fecha }}</span>
            @if (auth.usuario$ | async; as usuario) {
              <div class="user-chip" title="{{ usuario.nombre }}">
                <span class="avatar" aria-hidden="true">{{ iniciales(usuario.nombre) }}</span>
                <span class="user-meta">
                  <strong>{{ usuario.nombre }}</strong>
                  <small>{{ (usuario.roles ?? []) [0] || usuario.tipo || 'Usuario' }}</small>
                </span>
              </div>
            }
            <button type="button" class="btn btn-ghost btn-sm" (click)="auth.logout()">
              <app-icon name="logout" [size]="16" />
              <span class="salir-txt">Salir</span>
            </button>
          </div>
        </header>

        <main class="content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
      }

      .shell {
        display: flex;
        min-height: 100vh;
      }

      /* ---------- Sidebar ---------- */
      .sidebar {
        position: fixed;
        inset-block: 0;
        inset-inline-start: 0;
        width: var(--sidebar-width);
        background: rgba(10, 15, 30, 0.92);
        border-inline-end: 1px solid var(--color-border);
        display: flex;
        flex-direction: column;
        padding: 18px 14px;
        gap: 18px;
        z-index: 500;
        transition: width 220ms ease, transform 220ms ease;
        backdrop-filter: blur(8px);
      }

      .colapsado .sidebar {
        width: 74px;
      }

      .colapsado .sidebar .menu-label,
      .colapsado .sidebar .brand-name,
      .colapsado .sidebar .menu-seccion {
        opacity: 0;
        width: 0;
        overflow: hidden;
        white-space: nowrap;
      }

      .colapsado .sidebar .menu-item {
        justify-content: center;
        padding-inline: 0;
      }

      .colapsado .collapse-btn {
        justify-content: center;
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 4px 6px 14px;
        border-bottom: 1px solid var(--color-border);
      }

      .brand-mark {
        display: inline-flex;
        flex: 0 0 auto;
      }

      .brand-name {
        display: flex;
        flex-direction: column;
        line-height: 1.1;
        transition: opacity 200ms ease;
      }

      .brand-name strong {
        font-size: 16px;
        font-weight: 700;
        letter-spacing: -0.01em;
        color: var(--color-foreground);
      }

      .brand-name em {
        font-style: normal;
        font-family: var(--font-mono);
        font-size: 10.5px;
        text-transform: uppercase;
        letter-spacing: 0.22em;
        color: var(--color-primary);
      }

      .menu {
        display: flex;
        flex-direction: column;
        gap: 2px;
        overflow-y: auto;
        flex: 1;
        padding-inline-end: 4px;
      }

      .menu-seccion {
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.16em;
        color: #64748b;
        margin: 16px 8px 6px;
        transition: opacity 200ms ease;
      }

      .menu-seccion:first-child {
        margin-top: 4px;
      }

      .menu-item {
        display: flex;
        align-items: center;
        gap: 11px;
        padding: 9px 12px;
        border-radius: 8px;
        color: var(--color-muted-foreground);
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        border: 1px solid transparent;
        transition: background-color 200ms ease, color 200ms ease, border-color 200ms ease;
      }

      .menu-item:hover {
        background: rgba(39, 47, 66, 0.7);
        color: var(--color-foreground);
      }

      .menu-item.activo {
        background: rgba(245, 158, 11, 0.12);
        color: var(--color-primary);
        border-color: rgba(245, 158, 11, 0.3);
      }

      .menu-label {
        transition: opacity 200ms ease;
        white-space: nowrap;
      }

      .collapse-btn {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 9px 12px;
        background: transparent;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        color: var(--color-muted-foreground);
        font-size: 13px;
        cursor: pointer;
        transition: color 200ms ease, border-color 200ms ease;
      }

      .collapse-btn:hover {
        color: var(--color-foreground);
        border-color: #475569;
      }

      /* ---------- Main / topbar ---------- */
      .main {
        flex: 1;
        min-width: 0;
        margin-inline-start: var(--sidebar-width);
        transition: margin-inline-start 220ms ease;
        display: flex;
        flex-direction: column;
      }

      .colapsado .main {
        margin-inline-start: 74px;
      }

      .topbar {
        position: sticky;
        top: 0;
        z-index: 400;
        height: var(--topbar-height);
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 0 24px;
        background: rgba(15, 23, 42, 0.86);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid var(--color-border);
      }

      .burger {
        display: none;
      }

      .tasa-chip {
        display: flex;
        align-items: baseline;
        gap: 9px;
        padding: 6px 14px;
        border: 1px solid rgba(245, 158, 11, 0.3);
        background: rgba(245, 158, 11, 0.08);
        border-radius: 999px;
      }

      .tasa-label {
        font-size: 10.5px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        color: var(--color-muted-foreground);
      }

      .tasa-valor {
        font-size: 14.5px;
        font-weight: 600;
        color: var(--color-primary);
      }

      .tasa-compra {
        font-size: 11.5px;
        color: var(--color-muted-foreground);
      }

      .topbar-right {
        margin-inline-start: auto;
        display: flex;
        align-items: center;
        gap: 14px;
      }

      .fecha-hoy {
        font-size: 13px;
        color: var(--color-muted-foreground);
      }

      .user-chip {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 5px 12px 5px 5px;
        border: 1px solid var(--color-border);
        border-radius: 999px;
        background: var(--color-card);
      }

      .avatar {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        background: rgba(139, 92, 246, 0.25);
        border: 1px solid rgba(139, 92, 246, 0.5);
        color: #ddd6fe;
        font-size: 11.5px;
        font-weight: 700;
        display: grid;
        place-content: center;
        letter-spacing: 0.03em;
      }

      .user-meta {
        display: flex;
        flex-direction: column;
        line-height: 1.15;
      }

      .user-meta strong {
        font-size: 13px;
        font-weight: 600;
        color: var(--color-foreground);
      }

      .user-meta small {
        font-size: 11px;
        color: var(--color-muted-foreground);
        text-transform: uppercase;
        letter-spacing: 0.06em;
      }

      .content {
        flex: 1;
        padding: 26px 28px 48px;
        max-width: 1360px;
        width: 100%;
      }

      .side-overlay {
        display: none;
      }

      /* ---------- Responsive ---------- */
      @media (max-width: 1024px) {
        .content {
          padding: 22px 20px 40px;
        }
        .user-meta {
          display: none;
        }
      }

      @media (max-width: 768px) {
        .sidebar {
          transform: translateX(-100%);
          width: var(--sidebar-width) !important;
        }
        .menu-abierto .sidebar {
          transform: none;
        }
        .menu-abierto .side-overlay {
          display: block;
          position: fixed;
          inset: 0;
          background: rgba(4, 8, 18, 0.6);
          z-index: 450;
        }
        .main,
        .colapsado .main {
          margin-inline-start: 0;
        }
        .burger {
          display: inline-flex;
        }
        .topbar {
          padding: 0 16px;
          gap: 10px;
        }
        .fecha-hoy,
        .tasa-compra {
          display: none;
        }
        .content {
          padding: 18px 14px 36px;
        }
        .colapsado .sidebar .menu-label,
        .colapsado .sidebar .brand-name,
        .colapsado .sidebar .menu-seccion {
          opacity: 1;
          width: auto;
        }
        .colapsado .sidebar .menu-item {
          justify-content: flex-start;
          padding-inline: 12px;
        }
        .collapse-btn {
          display: none;
        }
      }

      @media (max-width: 375px) {
        .salir-txt {
          display: none;
        }
        .tasa-chip {
          padding: 5px 10px;
        }
      }
    `,
  ],
})
export class LayoutComponent implements OnInit {
  colapsado = false;
  menuAbierto = false;
  tasa: { tasaVenta?: number; tasaCompra?: number } | null = null;
  tasaCargada = false;
  hoy = new Date();

  readonly auth = inject(AuthService);
  private readonly tasaService = inject(TasaService);

  readonly menuOperacion: ItemMenu[] = [
    { ruta: '/dashboard', etiqueta: 'Dashboard', icono: 'grid' },
    { ruta: '/clientes', etiqueta: 'Clientes', icono: 'users' },
    { ruta: '/operaciones/compras', etiqueta: 'Compras', icono: 'arrowDown' },
    { ruta: '/operaciones/ventas', etiqueta: 'Ventas', icono: 'arrowUp' },
    { ruta: '/caja', etiqueta: 'Caja', icono: 'wallet' },
    { ruta: '/caja/cierre', etiqueta: 'Cierre de caja', icono: 'lock' },
  ];

  readonly menuTesoreria: ItemMenu[] = [
    { ruta: '/bancos', etiqueta: 'Bancos', icono: 'landmark' },
    { ruta: '/transferencias', etiqueta: 'Transferencias', icono: 'transfer' },
    { ruta: '/custodios', etiqueta: 'Custodios', icono: 'shield' },
    { ruta: '/tasas', etiqueta: 'Tasas', icono: 'percent' },
  ];

  readonly menuAdmin: ItemMenu[] = [
    { ruta: '/reportes/centro', etiqueta: 'Centro de reportes', icono: 'print' },
    { ruta: '/reportes/estado-general', etiqueta: 'Estado general', icono: 'chart' },
    { ruta: '/reportes/cuadre', etiqueta: 'Cuadre de caja', icono: 'banknote' },
    { ruta: '/reportes/estado-cuenta', etiqueta: 'Estado de cuenta', icono: 'file' },
    { ruta: '/usuarios', etiqueta: 'Usuarios', icono: 'userCog' },
    { ruta: '/configuracion', etiqueta: 'Configuración', icono: 'settings' },
  ];

  ngOnInit(): void {
    this.tasaService.actual().subscribe({
      next: (t) => {
        this.tasa = t ?? null;
        this.tasaCargada = true;
      },
      error: () => (this.tasaCargada = true),
    });
  }

  cerrarMenuMovil(): void {
    this.menuAbierto = false;
  }

  iniciales(nombre: string): string {
    return (nombre || '')
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? '')
      .join('');
  }
}
