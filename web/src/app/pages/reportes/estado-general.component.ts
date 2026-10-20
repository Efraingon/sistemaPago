import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReporteService } from '../../core/services/reporte.service';
import { ComprobanteService } from '../../core/services/comprobante.service';
import { ClienteService } from '../../core/services/cliente.service';
import { Cliente, EstadoGeneralItem, Configuracion } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { MontoPipe } from '../../shared/pipes/pipes';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-estado-general',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, ClienteAutocompleteComponent, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Reportes</p>
        <h1 class="page-title">Estado general</h1>
        <p class="page-sub">Compras, ventas, custodia y consolidado por cliente.</p>
      </div>
      <div class="row gap-2 wrap">
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/cuadre">Cuadre de caja</a>
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/estado-cuenta">Estado de cuenta</a>
        <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
          <app-icon name="arrowDown" [size]="14" /> CSV
        </button>
        <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
          <app-icon name="print" [size]="14" /> Imprimir
        </button>
      </div>
    </div>

    <div class="toolbar">
      <div class="form-group w-auto">
        <label class="form-label" for="fechaCorte">Fecha de corte</label>
        <input id="fechaCorte" class="input" type="date" [(ngModel)]="fechaCorte" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group grow" style="max-width: 280px">
        <label class="form-label">Cliente (opcional)</label>
        <app-cliente-autocomplete [cliente]="cliente" (clienteChange)="onCliente($event)" />
      </div>
      <button class="btn btn-ghost" type="button" (click)="limpiar()">
        <app-icon name="refresh" [size]="15" /> Limpiar
      </button>
    </div>

    <div class="table-wrap print-area">
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
            <h1>Estado General de Posiciones</h1>
            @if (fechaCorte) { <p class="papel-periodo">Fecha de corte: {{ fechaCorte }}</p> }
            @if (cliente) { <p class="papel-periodo">Cliente: {{ cliente.nombre || cliente.razonSocial || cliente.id }}</p> }
          </div>
        </header>
      }
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Calculando estado general…</div>
      } @else if (!items.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="chart" [size]="36" /></span>
          <strong>Sin datos para la fecha de corte</strong>
          <span class="small">No hay operaciones ni custodios registrados hasta el {{ fechaCorte }}.</span>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Cliente</th>
              <th class="num">Compras Bs</th>
              <th class="num">Ventas Bs</th>
              <th class="num">Custodio</th>
              <th class="num">Custodio Bs</th>
              <th class="num">Consolidado</th>
            </tr>
          </thead>
          <tbody>
            @for (f of items; track $index) {
              <tr>
                <td>{{ f.cliente || '—' }}</td>
                <td class="num">{{ f.comprasBs | monto }}</td>
                <td class="num">{{ f.ventasBs | monto }}</td>
                <td class="num">{{ f.custodio | monto }}</td>
                <td class="num">{{ f.custodioBs | monto }}</td>
                <td class="num" [class.text-success]="f.consolidado >= 0" [class.text-danger]="f.consolidado < 0">
                  {{ f.consolidado | monto }}
                </td>
              </tr>
            }
          </tbody>
          <tfoot>
            <tr>
              <td>Totales</td>
              <td class="num">{{ totales.compras | monto }}</td>
              <td class="num">{{ totales.ventas | monto }}</td>
              <td class="num">{{ totales.custodio | monto }}</td>
              <td class="num">{{ totales.custodioBs | monto }}</td>
              <td class="num text-gold">{{ totales.consolidado | monto }}</td>
            </tr>
          </tfoot>
        </table>
      }
    </div>

    @if (items.length > 10) {
      <p class="small text-muted mt-2">Mostrando {{ items.length }} clientes en la respuesta.</p>
    }
  `,
  styles: [
    `
      a {
        cursor: pointer;
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
export class EstadoGeneralComponent implements OnInit {
  fechaCorte = '';
  cliente: Cliente | null = null;
  items: EstadoGeneralItem[] = [];
  cargando = false;
  empresa: Configuracion | null = null;

  private readonly service = inject(ReporteService);
  private readonly configService = inject(ComprobanteService);

  get totales() {
    return this.items.reduce(
      (acc, f) => ({
        compras: acc.compras + Number(f.comprasBs || 0),
        ventas: acc.ventas + Number(f.ventasBs || 0),
        custodio: acc.custodio + Number(f.custodio || 0),
        custodioBs: acc.custodioBs + Number(f.custodioBs || 0),
        consolidado: acc.consolidado + Number(f.consolidado || 0),
      }),
      { compras: 0, ventas: 0, custodio: 0, custodioBs: 0, consolidado: 0 }
    );
  }

  ngOnInit(): void {
    this.fechaCorte = this.hoyIso();
    this.configService.configuracion().subscribe({
      next: (c) => (this.empresa = c),
      error: () => (this.empresa = null),
    });
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.service.estadoGeneral({ fechaCorte: this.fechaCorte, clienteId: this.cliente?.id }).subscribe({
      next: (r) => {
        this.items = this.service.normalizarLista<EstadoGeneralItem>(r);
        this.cargando = false;
      },
      error: () => {
        this.items = [];
        this.cargando = false;
      },
    });
  }

  onCliente(c: Cliente | null): void {
    this.cliente = c;
    this.cargar();
  }

  limpiar(): void {
    this.cliente = null;
    this.fechaCorte = this.hoyIso();
    this.cargar();
  }

  /** Imprime el estado general (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta la tabla de clientes a CSV compatible con Excel. */
  exportar(): void {
    const encabezados = ['Cliente', 'Compras Bs', 'Ventas Bs', 'Custodio', 'Custodio Bs', 'Consolidado'];
    const filas: (string | number)[][] = this.items.map((f) => [
      f.cliente || '',
      Number(f.comprasBs || 0),
      Number(f.ventasBs || 0),
      Number(f.custodio || 0),
      Number(f.custodioBs || 0),
      Number(f.consolidado || 0),
    ]);
    filas.push([
      'TOTALES',
      this.totales.compras,
      this.totales.ventas,
      this.totales.custodio,
      this.totales.custodioBs,
      this.totales.consolidado,
    ]);
    exportarCsv(`estado_general_${this.fechaCorte}`, encabezados, filas);
  }

  private hoyIso(): string {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
