import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReporteService } from '../../core/services/reporte.service';
import { ComprobanteService } from '../../core/services/comprobante.service';
import { Cliente, EstadoCuenta, EstadoCuentaLinea, Configuracion } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';
import { exportarCsv, fechaTexto, imprimir } from '../../shared/export';

@Component({
  selector: 'app-estado-cuenta',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, ClienteAutocompleteComponent, FechaPipe, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Reportes</p>
        <h1 class="page-title">Estado de cuenta</h1>
        <p class="page-sub">Línea de tiempo tabular de movimientos y saldo corriente por cliente.</p>
      </div>
      <div class="row gap-2 wrap">
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/estado-general">Estado general</a>
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/cuadre">Cuadre de caja</a>
        <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
          <app-icon name="arrowDown" [size]="14" /> CSV
        </button>
        <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
          <app-icon name="print" [size]="14" /> Imprimir
        </button>
      </div>
    </div>

    <div class="toolbar">
      <div class="form-group grow" style="max-width: 300px">
        <label class="form-label">Cliente <span class="req">*</span></label>
        <app-cliente-autocomplete [cliente]="cliente" [invalido]="falloCliente" (clienteChange)="onCliente($event)" />
        @if (falloCliente) {
          <span class="form-error"><app-icon name="alert" [size]="12" /> Seleccione un cliente</span>
        }
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="desde">Desde</label>
        <input id="desde" class="input" type="date" [(ngModel)]="desde" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="hasta">Hasta</label>
        <input id="hasta" class="input" type="date" [(ngModel)]="hasta" />
      </div>
      <button class="btn btn-primary" type="button" (click)="cargar()">
        <app-icon name="search" [size]="15" /> Consultar
      </button>
    </div>

    @if (cargando) {
      <div class="card"><div class="loading-panel"><span class="spinner"></span> Consultando estado de cuenta…</div></div>
    } @else if (consultado) {
      @if (!lineas.length) {
        <div class="card">
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="file" [size]="36" /></span>
            <strong>Sin movimientos en el rango</strong>
            <span class="small">El cliente no registra movimientos entre las fechas seleccionadas.</span>
          </div>
        </div>
      } @else {
        <div class="print-area">
        @if (!empresa) {
          <div class="loading-panel"><span class="spinner"></span> Cargando encabezado…</div>
        } @else {
          <header class="papel-head">
            <div class="papel-empresa">
              <strong>{{ empresa.nombreEmpresa || 'Monrys Capital' }}</strong>
              @if (empresa.rif) { <span>RIF: {{ empresa.rif }}</span> }
              @if (empresa.direccion) { <span>{{ empresa.direccion }}</span> }
              @if (empresa.telefono) { <span>Tel: {{ empresa.telefono }}</span> }
            </div>
            <div class="papel-titulo">
              <h1>Estado de Cuenta</h1>
              <p class="papel-periodo">Cliente: {{ cliente?.nombre || cliente?.razonSocial || cliente?.id }}</p>
              @if (desde || hasta) { <p class="papel-periodo">{{ desde | fecha }} → {{ hasta | fecha }}</p> }
            </div>
          </header>
        }
        <div class="grid-3 mb-4">
          <article class="kpi">
            <span class="kpi-label"><app-icon name="file" [size]="14" /> Líneas</span>
            <span class="kpi-value">{{ lineas.length }}</span>
            <span class="kpi-sub">Movimientos encontrados</span>
          </article>
          <article class="kpi">
            <span class="kpi-label"><app-icon name="trending" [size]="14" /> Último saldo</span>
            <span class="kpi-value">{{ ultimoSaldo | monto }}</span>
            <span class="kpi-sub">Saldo corriente en Bs</span>
          </article>
          <article class="kpi">
            <span class="kpi-label"><app-icon name="swap" [size]="14" /> Entradas</span>
            <span class="kpi-value">{{ entradas | monto }}</span>
            <span class="kpi-sub">Suma de entradas en Bs</span>
          </article>
        </div>

        <div class="card" style="padding: 0">
          <div class="timeline">
            <div class="timeline-item" style="background: rgba(39, 47, 66, 0.55)">
              <strong class="small text-muted uppercase">Fecha</strong>
              <strong class="small text-muted uppercase">Sentido</strong>
              <strong class="small text-muted uppercase">Descripción</strong>
              <strong class="small text-muted uppercase text-right">Monto</strong>
              <strong class="small text-muted uppercase text-right">Saldo</strong>
            </div>
            @for (l of lineas; track $index) {
              <div class="timeline-item">
                <span class="mono small">{{ l.fecha | fecha }}</span>
                <span class="badge" [class.badge-success]="positivo(l)" [class.badge-danger]="!positivo(l)">
                  {{ positivo(l) ? 'ENTRADA' : 'SALIDA' }}
                </span>
                <span class="text-muted small">
                  {{ l.descripcion || '—' }}
                  <span class="mono" style="opacity: 0.75">
                    · {{ l.tipo }}{{ l.referencia ? ' #' + l.referencia : '' }}
                  </span>
                </span>
                <span class="mono num text-right" [class.text-success]="positivo(l)" [class.text-danger]="!positivo(l)">
                  {{ (l.montoBs ?? l.monto) | monto }}
                  @if (l.tasa) {
                    <span class="small text-muted" style="display: block">
                      {{ l.monto | monto }} × {{ l.tasa | monto }}
                    </span>
                  }
                </span>
                <span class="mono num text-right">{{ l.acumuladoBs | monto }}</span>
              </div>
            }
          </div>
        </div>
        </div>
      }
    } @else {
      <div class="card">
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="search" [size]="36" /></span>
          <strong>Seleccione un cliente</strong>
          <span class="small">Elija un cliente y pulse «Consultar» para ver su historial.</span>
        </div>
      </div>
    }
  `,
  styles: [
    `
      a {
        cursor: pointer;
      }
      .timeline-item.strong {
        font-weight: 600;
      }

      /* ------------------------------------------------ encabezado de papel */
      .papel-head {
        border-bottom: 3px double #0f172a;
        padding-bottom: 14px;
        margin-bottom: 20px;
      }
      .papel-empresa {
        display: flex;
        flex-direction: column;
        gap: 2px;
        font-size: 12px;
        color: #334155;
        margin-bottom: 16px;
      }
      .papel-empresa strong {
        font-size: 15px;
        color: #0f172a;
        letter-spacing: 0.06em;
        text-transform: uppercase;
      }
      .papel-titulo {
        text-align: center;
      }
      .papel-titulo h1 {
        margin: 0;
        font-size: 17px;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        color: #0f172a;
      }
      .papel-periodo {
        margin: 6px 0 0;
        font-size: 12px;
        color: #475569;
        font-family: var(--font-mono, 'IBM Plex Mono', monospace);
      }

      @media print {
        .papel-head {
          border-bottom: 3px double #000 !important;
          padding-bottom: 14px;
          margin-bottom: 20px;
        }
        .papel-empresa strong,
        .papel-titulo h1,
        .papel-periodo {
          color: #000 !important;
        }
      }
    `,
  ],
})
export class EstadoCuentaComponent implements OnInit {
  cliente: Cliente | null = null;
  falloCliente = false;
  desde = '';
  hasta = '';
  cargando = false;
  consultado = false;
  lineas: EstadoCuentaLinea[] = [];
  resumen: EstadoCuenta | null = null;
  empresa: Configuracion | null = null;

  private readonly service = inject(ReporteService);
  private readonly configService = inject(ComprobanteService);

  get ultimoSaldo(): number {
    if (this.resumen?.saldoBs != null) return this.resumen.saldoBs;
    return this.lineas.length ? Number(this.lineas[this.lineas.length - 1].acumuladoBs || 0) : 0;
  }

  get entradas(): number {
    if (this.resumen?.totalEntradasBs != null) return this.resumen.totalEntradasBs;
    return this.lineas
      .filter((l) => this.positivo(l))
      .reduce((s, l) => s + Number(l.montoBs ?? l.monto ?? 0), 0);
  }

  /** Sin rango por defecto: el histórico migrado (2022-2025) queda fuera de un
   *  filtro corto, así que por defecto se muestra la cuenta completa. */
  ngOnInit(): void {
    this.desde = '';
    this.hasta = '';
    this.configService.configuracion().subscribe({
      next: (c) => (this.empresa = c),
      error: () => (this.empresa = null),
    });
  }

  onCliente(c: Cliente | null): void {
    this.cliente = c;
    this.falloCliente = !c;
    if (c) this.cargar();
  }

  cargar(): void {
    this.falloCliente = !this.cliente;
    if (this.falloCliente) return;
    this.cargando = true;
    this.consultado = true;
    this.service.estadoCuenta(this.cliente!.id, this.desde, this.hasta).subscribe({
      next: (r) => {
        this.resumen = r ?? null;
        this.lineas = Array.isArray(r?.lineas)
          ? r.lineas
          : this.service.normalizarLista<EstadoCuentaLinea>(r);
        this.cargando = false;
      },
      error: () => {
        this.lineas = [];
        this.resumen = null;
        this.cargando = false;
      },
    });
  }

  positivo(l: EstadoCuentaLinea): boolean {
    return (l.sentido ?? 'ENTRADA') !== 'SALIDA';
  }

  /** Imprime el estado de cuenta (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta la línea de tiempo a CSV compatible con Excel. */
  exportar(): void {
    const encabezados = ['Fecha', 'Tipo', 'Origen', 'Descripción', 'Referencia', 'Sentido', 'Monto Bs', 'Acumulado Bs'];
    const filas = this.lineas.map((l) => [
      fechaTexto(l.fecha),
      l.tipo || '',
      l.origen || '',
      l.descripcion || '',
      l.referencia || '',
      l.sentido || '',
      Number(l.montoBs ?? l.monto ?? 0),
      Number(l.acumuladoBs ?? 0),
    ]);
    const nombre = this.cliente?.razonSocial || this.cliente?.nombre || this.cliente?.id || 'cliente';
    exportarCsv(`estado_cuenta_${nombre}_${this.desde || 'histórico'}_${this.hasta || 'hoy'}`, encabezados, filas);
  }
}
