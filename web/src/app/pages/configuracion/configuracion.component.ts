import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfiguracionService } from '../../core/services/usuario.service';
import { ToastService } from '../../shared/toast/toast.service';
import { AuditoriaItem, Configuracion } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { FechaPipe } from '../../shared/pipes/pipes';

/** Configuración general (datos de empresa) + auditoría del sistema.
 *  La forma es dinámica: el backend define los campos, la UI los edita todos. */
@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [FormsModule, IconComponent, FechaPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Administración</p>
        <h1 class="page-title">Configuración</h1>
        <p class="page-sub">Datos de empresa usados en comprobantes y auditoría de cambios.</p>
      </div>
    </div>

    <div class="tabs" role="tablist">
      <button class="tab" [class.active]="tab === 'general'" (click)="tab = 'general'" type="button">General</button>
      <button class="tab" [class.active]="tab === 'auditoria'" (click)="tab = 'auditoria'; cargarAuditoria()" type="button">
        Auditoría
      </button>
    </div>

    <!-- ===== General ===== -->
    @if (tab === 'general') {
      <div class="grid-2">
        <section class="card">
          <div class="row-between mb-3">
            <h2 class="card-title" style="margin: 0">Datos de empresa</h2>
            @if (cargando) {
              <span class="spinner" style="width: 16px; height: 16px"></span>
            }
          </div>

          @if (!claves.length && !cargando) {
            <div class="empty-state">
              <span class="empty-ico"><app-icon name="settings" [size]="32" /></span>
              <strong>Sin configuración</strong>
              <span class="small">El backend no devolvió parámetros configurables.</span>
            </div>
          } @else {
            <form (ngSubmit)="guardar()" #formCfg="ngForm">
              @for (k of claves; track k) {
                <div class="form-group mb-3">
                  <label class="form-label" [attr.for]="'cfg-' + k">{{ etiqueta(k) }}</label>
                  @if (esBooleano(k)) {
                    <label class="check">
                      <input type="checkbox" [(ngModel)]="valores[k]" [name]="k" />
                      Activo
                    </label>
                  } @else if (esNumerico(k)) {
                    <input
                      [id]="'cfg-' + k"
                      class="input mono"
                      type="number"
                      step="0.01"
                      [(ngModel)]="valores[k]"
                      [name]="k"
                    />
                  } @else {
                    <input
                      [id]="'cfg-' + k"
                      class="input"
                      [class.mono]="esCodigo(k)"
                      [(ngModel)]="valores[k]"
                      [name]="k"
                      [placeholder]="etiqueta(k)"
                    />
                  }
                </div>
              }

              <div class="row gap-2" style="justify-content: flex-end">
                <button class="btn btn-ghost" type="button" (click)="recargar()">Recargar</button>
                <button class="btn btn-primary" type="submit" [disabled]="guardando">
                  @if (guardando) {
                    <span class="spinner" style="width: 16px; height: 16px"></span> Guardando…
                  } @else {
                    Guardar configuración
                  }
                </button>
              </div>
            </form>
          }
        </section>

        <section class="card">
          <h2 class="card-title">Vista previa de comprobante</h2>
          <div class="prev">
            <div class="prev-head">
              <svg viewBox="0 0 32 32" width="30" height="30" aria-hidden="true">
                <rect width="32" height="32" rx="7" fill="#0F172A" stroke="rgba(245,158,11,.45)" />
                <path d="M7 23V9h4l5 8 5-8h4v14h-4v-8l-5 8-5-8v8z" fill="#F59E0B" />
              </svg>
              <div>
                <strong>{{ valor('nombreEmpresa') || 'Monrys Capital' }}</strong>
                <span class="mono small text-muted">{{ valor('rif') ? 'RIF ' + valor('rif') : 'Casa de cambio' }}</span>
              </div>
            </div>
            <div class="prev-hairline"></div>
            <p class="small text-muted">{{ valor('direccion') || 'Dirección de la empresa' }}</p>
            <p class="small text-muted mono">{{ valor('telefono') || 'Teléfono' }} {{ valor('email') ? '· ' + valor('email') : '' }}</p>
            <p class="small mt-3">{{ valor('mensajePie') || 'Mensaje al pie de los comprobantes.' }}</p>
          </div>
          <p class="form-hint mt-3">
            Estos datos encabezán los comprobantes imprimibles (recibos, compras, ventas y movimientos de caja).
          </p>
        </section>
      </div>
    }

    <!-- ===== Auditoría ===== -->
    @if (tab === 'auditoria') {
      <div class="toolbar">
        <div class="form-group w-auto">
          <label class="form-label" for="aTabla">Tabla</label>
          <input id="aTabla" class="input" [(ngModel)]="aTabla" (ngModelChange)="cargarAuditoria()" placeholder="clientes, compras…" />
        </div>
        <div class="form-group w-auto">
          <label class="form-label" for="aDesde">Desde</label>
          <input id="aDesde" class="input" type="date" [(ngModel)]="aDesde" (ngModelChange)="cargarAuditoria()" />
        </div>
        <div class="form-group w-auto">
          <label class="form-label" for="aHasta">Hasta</label>
          <input id="aHasta" class="input" type="date" [(ngModel)]="aHasta" (ngModelChange)="cargarAuditoria()" />
        </div>
        <div class="form-group w-auto">
          <label class="form-label" for="aUsuario">Usuario</label>
          <input id="aUsuario" class="input" [(ngModel)]="aUsuario" (ngModelChange)="cargarAuditoria()" placeholder="usuario" />
        </div>
        <button class="btn btn-ghost" type="button" (click)="limpiarAuditoria()">
          <app-icon name="refresh" [size]="15" /> Limpiar
        </button>
      </div>

      <div class="table-wrap">
        @if (cargandoAud) {
          <div class="loading-panel"><span class="spinner"></span> Cargando auditoría…</div>
        } @else if (!auditoria.length) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="clock" [size]="36" /></span>
            <strong>Sin registros de auditoría</strong>
            <span class="small">No hay cambios registrados con estos filtros.</span>
          </div>
        } @else {
          <table class="table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Usuario</th>
                <th>Tabla</th>
                <th>Registro</th>
                <th>Acción</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              @for (a of auditoria; track $index) {
                <tr>
                  <td class="mono">{{ a.fecha | fecha }}</td>
                  <td>{{ a.usuario || '—' }}</td>
                  <td class="mono small">{{ a.tabla || '—' }}</td>
                  <td class="mono">{{ a.registroId ?? '—' }}</td>
                  <td>
                    <span
                      class="badge"
                      [class.badge-danger]="esAccion(a, 'DELETE') || esAccion(a, 'anul')"
                      [class.badge-success]="esAccion(a, 'INSERT') || esAccion(a, 'POST')"
                      [class.badge-gold]="esAccion(a, 'UPDATE') || esAccion(a, 'PUT')"
                    >
                      {{ a.accion || '—' }}
                    </span>
                  </td>
                  <td class="small text-muted">{{ a.detalle || a.datos || '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>

      @if (audTotal > audTamano) {
        <div class="pagination">
          <span>Página <strong class="mono">{{ audPagina }}</strong> · {{ audTotal }} eventos</span>
          <div class="pages">
            <button class="icon-btn" type="button" [disabled]="audPagina <= 1" (click)="pagAud(-1)" aria-label="Anterior">
              <app-icon name="chevronLeft" [size]="15" />
            </button>
            <button class="icon-btn" type="button" [disabled]="audPagina >= Math.ceil(audTotal / audTamano)" (click)="pagAud(1)" aria-label="Siguiente">
              <app-icon name="chevronRight" [size]="15" />
            </button>
          </div>
        </div>
      }
    }
  `,
  styles: [
    `
      .prev {
        border: 1px solid var(--color-border);
        border-radius: 10px;
        padding: 18px;
        background: var(--color-background-deep);
      }
      .prev-head {
        display: flex;
        align-items: center;
        gap: 12px;
      }
      .prev-head strong {
        display: block;
        font-size: 15.5px;
        color: var(--color-foreground);
      }
      .prev-hairline {
        height: 1px;
        background: linear-gradient(90deg, var(--color-primary), transparent);
        margin: 14px 0;
      }
      .tab {
        cursor: pointer;
      }
      button {
        cursor: pointer;
      }
    `,
  ],
})
export class ConfiguracionComponent implements OnInit {
  tab: 'general' | 'auditoria' = 'general';

  config: Configuracion | null = null;
  valores: Record<string, any> = {};
  claves: string[] = [];
  cargando = false;
  guardando = false;

  // Auditoría
  auditoria: AuditoriaItem[] = [];
  cargandoAud = false;
  aTabla = '';
  aDesde = '';
  aHasta = '';
  aUsuario = '';
  audPagina = 1;
  audTamano = 20;
  audTotal = 0;

  readonly Math = Math;

  private readonly service = inject(ConfiguracionService);
  private readonly toast = inject(ToastService);

  ngOnInit(): void {
    this.recargar();
  }

  recargar(): void {
    this.cargando = true;
    this.service.obtener().subscribe({
      next: (cfg: Configuracion | null) => {
        const datos: Configuracion = cfg ?? {};
        this.config = datos;
        const valores: Record<string, any> = {};
        const claves: string[] = [];
        for (const clave of Object.keys(datos)) {
          if (clave === 'id') continue;
          const v = (datos as any)[clave];
          if (v !== null && typeof v === 'object') continue;
          claves.push(clave);
          valores[clave] = v ?? '';
        }
        this.valores = valores;
        this.claves = claves;
        this.cargando = false;
      },
      error: () => {
        this.config = null;
        this.claves = [];
        this.cargando = false;
      },
    });
  }

  pagAud(delta: number): void {
    this.audPagina = Math.max(1, this.audPagina + delta);
    this.cargarAuditoria();
  }

  valor(campo: string): string {
    const v = this.valores[campo] ?? (this.config as any)?.[campo];
    return v === null || v === undefined ? '' : String(v);
  }

  etiqueta(campo: string): string {
    const mapa: Record<string, string> = {
      nombreEmpresa: 'Nombre de la empresa',
      rif: 'RIF',
      direccion: 'Dirección',
      telefono: 'Teléfono',
      email: 'Email',
      mensajePie: 'Mensaje al pie del comprobante',
      paginaWeb: 'Página web',
      monedaBase: 'Moneda base',
    };
    if (mapa[campo]) return mapa[campo];
    // nombre camelCase → "Nombre empresa"
    return campo
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (c) => c.toUpperCase())
      .trim();
  }

  esBooleano(campo: string): boolean {
    return typeof this.valores[campo] === 'boolean';
  }

  esNumerico(campo: string): boolean {
    return typeof this.valores[campo] === 'number';
  }

  esCodigo(campo: string): boolean {
    return ['rif', 'telefono', 'telefonoEmpresa', 'fax'].includes(campo);
  }

  guardar(): void {
    if (!this.config) return;
    this.guardando = true;
    const body: Configuracion = { ...this.config, ...this.valores };
    this.service.guardar(body).subscribe({
      next: (resp: Configuracion | null) => {
        this.guardando = false;
        this.toast.success('Configuración guardada.');
        if (resp && typeof resp === 'object') this.recargar();
      },
      error: () => (this.guardando = false),
    });
  }

  // ---------- Auditoría ----------
  cargarAuditoria(): void {
    this.cargandoAud = true;
    this.service
      .auditoria({
        tabla: this.aTabla,
        desde: this.aDesde,
        hasta: this.aHasta,
        usuario: this.aUsuario,
        pagina: this.audPagina,
        tamano: this.audTamano,
      })
      .subscribe({
        next: (lista: AuditoriaItem[] | { items: AuditoriaItem[]; total?: number }) => {
          this.auditoria = Array.isArray(lista) ? lista : ((lista as any)?.items ?? []);
          this.audTotal = (lista as any)?.total ?? this.auditoria.length;
          this.cargandoAud = false;
        },
        error: () => {
          this.auditoria = [];
          this.cargandoAud = false;
        },
      });
  }

  limpiarAuditoria(): void {
    this.aTabla = '';
    this.aDesde = '';
    this.aHasta = '';
    this.aUsuario = '';
    this.audPagina = 1;
    this.cargarAuditoria();
  }

  esAccion(a: AuditoriaItem, texto: string): boolean {
    return String(a.accion ?? '').toUpperCase().includes(texto.toUpperCase());
  }
}
