import { Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { ComprobanteService } from '../../core/services/comprobante.service';
import { Comprobante, Configuracion, TipoComprobante } from '../../core/models';
import { ModalComponent } from '../modal/modal.component';
import { IconComponent } from '../icon/icon.component';
import { FechaPipe, MontoPipe } from '../pipes/pipes';

/** Comprobante tipo recibo/ticket, imprimible (@media print → solo .print-area).
 *  Uso: <app-comprobante [(visible)]="verComprobante" tipo="compra" [id]="opId" /> */
@Component({
  selector: 'app-comprobante',
  standalone: true,
  imports: [ModalComponent, IconComponent, FechaPipe, MontoPipe],
  template: `
    <app-modal [visible]="visible" [title]="tituloModal" ancho="md" (closed)="closed.emit(false)">
      <div class="print-area">
        @if (cargando) {
          <div class="loading-panel"><span class="spinner"></span> Generando comprobante…</div>
        } @else if (error) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="alert" [size]="32" /></span>
            <strong>No se pudo generar el comprobante</strong>
            <span class="small">{{ error }}</span>
          </div>
        } @else {
          <!-- Encabezado empresa -->
          <header class="comp-head">
            <div class="comp-empresa">
              <svg viewBox="0 0 32 32" width="34" height="34" aria-hidden="true">
                <rect width="32" height="32" rx="7" fill="#0F172A" />
                <path d="M7 23V9h4l5 8 5-8h4v14h-4v-8l-5 8-5-8v8z" fill="#F59E0B" />
              </svg>
              <div>
                <strong>{{ empresa('nombreEmpresa') || 'Monrys Capital' }}</strong>
                <span class="mono sub">{{ empresa('rif') ? 'RIF ' + empresa('rif') : 'Casa de cambio' }}</span>
              </div>
            </div>
            <div class="comp-tipo">
              <span class="badge badge-gold">{{ etiquetaTipo }}</span>
              @if (datos?.numero) {
                <span class="mono nro">Nº {{ datos.numero }}</span>
              }
            </div>
          </header>

          <div class="comp-hairline" aria-hidden="true"></div>

          <!-- Metadatos -->
          <section class="comp-meta">
            <div>
              <span class="dt">Fecha</span>
              <span class="dd mono">{{ (datos?.fecha || hoy) | fecha }}</span>
            </div>
            @if (datos?.cliente) {
              <div>
                <span class="dt">Cliente</span>
                <span class="dd">{{ datos?.cliente }}</span>
              </div>
            }
            @if (datos?.rif) {
              <div>
                <span class="dt">RIF / Cédula</span>
                <span class="dd mono">{{ datos?.rif }}</span>
              </div>
            }
            @if (datos?.tasa) {
              <div>
                <span class="dt">Tasa aplicada</span>
                <span class="dd mono">{{ datos?.tasa | monto }} Bs</span>
              </div>
            }
          </section>

          <!-- Detalle -->
          @if (lineas.length) {
            <table class="table comp-table">
              <thead>
                <tr>
                  <th>Concepto</th>
                  <th class="num">Cantidad</th>
                  <th class="num">Monto</th>
                </tr>
              </thead>
              <tbody>
                @for (l of lineas; track $index) {
                  <tr>
                    <td>{{ l.descripcion || '—' }}</td>
                    <td class="num">{{ l.cantidad != null ? (l.cantidad | monto) : '—' }}</td>
                    <td class="num">{{ (l.monto ?? l.valor ?? 0) | monto }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }

          <!-- Otros campos (respuestas planas del backend) -->
          @if (extras.length) {
            <section class="comp-extras">
              @for (e of extras; track e.clave) {
                <div class="extra-row">
                  <span class="dt">{{ e.clave }}</span>
                  <span class="dd mono">{{ e.valor }}</span>
                </div>
              }
            </section>
          }

          <footer class="comp-foot">
            <div class="total-line">
              <span>Total</span>
              <strong class="mono">{{ (datos?.total ?? totalLineas) | monto }}</strong>
            </div>
            <p class="pie">{{ empresa('mensajePie') || 'Comprobante válido ante Monrys Capital. Verifique sus datos antes de firmar.' }}</p>
            <div class="firmas">
              <div class="firma"><span>Elaborado por</span></div>
              <div class="firma"><span>Recibido por</span></div>
            </div>
          </footer>
        }
      </div>

      @if (!cargando && !error) {
        <div class="acciones no-print">
          <button class="btn btn-ghost" type="button" (click)="closed.emit(false)">Cerrar</button>
          <button class="btn btn-primary" type="button" (click)="imprimir()">
            <app-icon name="print" [size]="16" /> Imprimir
          </button>
        </div>
      }
    </app-modal>
  `,
  styles: [
    `
      .print-area {
        color: var(--color-foreground);
      }

      .comp-head {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
      }

      .comp-empresa {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .comp-empresa strong {
        display: block;
        font-size: 16.5px;
        font-weight: 700;
        letter-spacing: -0.01em;
      }

      .comp-empresa .sub {
        font-size: 11.5px;
        color: var(--color-muted-foreground);
        letter-spacing: 0.06em;
        text-transform: uppercase;
      }

      .comp-tipo {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: 6px;
      }

      .comp-tipo .nro {
        font-size: 13px;
        color: var(--color-primary);
      }

      .comp-hairline {
        height: 1px;
        background: linear-gradient(90deg, var(--color-primary), rgba(245, 158, 11, 0.1));
        margin: 16px 0;
      }

      .comp-meta {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 14px;
        margin-bottom: 18px;
      }

      .dt {
        display: block;
        font-size: 10.5px;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        color: var(--color-muted-foreground);
        margin-bottom: 3px;
      }

      .dd {
        font-size: 14px;
        color: inherit;
      }

      .comp-table {
        min-width: 0;
        margin-bottom: 16px;
      }

      .comp-extras {
        display: flex;
        flex-direction: column;
        gap: 7px;
        padding: 14px;
        border: 1px dashed var(--color-border);
        border-radius: 8px;
        margin-bottom: 16px;
      }

      .extra-row {
        display: flex;
        justify-content: space-between;
        gap: 14px;
        font-size: 13.5px;
      }

      .extra-row .dt {
        display: inline;
        text-transform: capitalize;
        letter-spacing: 0.02em;
        font-size: 12.5px;
        margin: 0;
      }

      .comp-foot {
        border-top: 1px solid var(--color-border);
        padding-top: 16px;
      }

      .total-line {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 16px;
        font-size: 14px;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--color-muted-foreground);
      }

      .total-line strong {
        font-size: 24px;
        color: var(--color-primary);
        font-weight: 600;
        letter-spacing: -0.02em;
      }

      .pie {
        margin-top: 14px;
        font-size: 11.5px;
        line-height: 1.55;
        color: var(--color-muted-foreground);
      }

      .firmas {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 32px;
        margin-top: 42px;
      }

      .firma {
        border-top: 1px solid var(--color-border);
        padding-top: 7px;
        text-align: center;
        font-size: 11.5px;
        color: var(--color-muted-foreground);
        text-transform: uppercase;
        letter-spacing: 0.08em;
      }

      .acciones {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 20px;
        padding-top: 16px;
        border-top: 1px solid var(--color-border);
      }

      /* Impresión: fondo blanco, texto negro */
      @media print {
        .print-area {
          color: #000 !important;
          background: #fff !important;
        }
        .print-area .dt,
        .print-area .sub,
        .print-area .pie,
        .print-area .firma,
        .print-area .total-line {
          color: #333 !important;
        }
        .print-area .badge-gold {
          color: #000 !important;
          background: transparent !important;
          border-color: #000 !important;
        }
        .print-area .total-line strong,
        .print-area .comp-tipo .nro {
          color: #000 !important;
        }
        .print-area .table th {
          background: #f1f5f9 !important;
          color: #000 !important;
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
        }
        .print-area .table td,
        .print-area .table th {
          border-color: #cbd5e1 !important;
          color: #000 !important;
        }
        .print-area .comp-hairline {
          background: #000 !important;
        }
        .print-area .comp-extras,
        .print-area .firma,
        .print-area .comp-foot {
          border-color: #94a3b8 !important;
        }
      }
    `,
  ],
})
export class ComprobanteComponent implements OnChanges {
  @Input() visible = false;
  @Input() tipo: TipoComprobante = 'compra';
  @Input() id: number | null = null;
  @Output() closed = new EventEmitter<boolean>();

  cargando = false;
  error = '';
  datos: Comprobante | null = null;
  empresaDatos: Configuracion | null = null;
  hoy = new Date();

  private readonly service = inject(ComprobanteService);

  get tituloModal(): string {
    return `Comprobante de ${this.etiquetaTipo.toLowerCase()}`;
  }

  get etiquetaTipo(): string {
    const mapa: Record<string, string> = {
      recibo: 'Recibo',
      compra: 'Compra',
      venta: 'Venta',
      ingreso: 'Ingreso de caja',
      egreso: 'Egreso de caja',
      entrega: 'Entrega',
      recepcion: 'Recepción',
    };
    return mapa[this.tipo] ?? 'Comprobante';
  }

  get lineas(): { descripcion?: string; cantidad?: number; monto?: number; valor?: number }[] {
    const l = this.datos?.lineas ?? this.datos?.detalles;
    return Array.isArray(l) ? l : [];
  }

  get totalLineas(): number {
    return this.lineas.reduce((s, l) => s + Number(l.monto ?? l.valor ?? 0), 0);
  }

  /** Campos de la respuesta que no son ni encabezado ni líneas (se listan al pie). */
  get extras(): { clave: string; valor: string }[] {
    if (!this.datos) return [];
    const omitir = new Set([
      'titulo', 'numero', 'fecha', 'cliente', 'rif', 'empresa', 'direccion',
      'telefono', 'tasa', 'total', 'lineas', 'detalles', 'id', 'moneda', 'estado',
    ]);
    const salida: { clave: string; valor: string }[] = [];
    for (const clave of Object.keys(this.datos)) {
      if (omitir.has(clave)) continue;
      const valor = (this.datos as any)[clave];
      if (valor === null || valor === undefined || typeof valor === 'object') continue;
      salida.push({ clave, valor: String(valor) });
    }
    return salida;
  }

  ngOnChanges(): void {
    if (this.visible && this.id != null) {
      this.cargar();
    }
    if (this.visible && !this.empresaDatos) {
      this.service.configuracion().subscribe({
        next: (c) => (this.empresaDatos = c),
        error: () => undefined,
      });
    }
  }

  empresa(campo: string): string {
    const v = (this.empresaDatos as any)?.[campo];
    return v == null ? '' : String(v);
  }

  imprimir(): void {
    window.print();
  }

  private cargar(): void {
    this.cargando = true;
    this.error = '';
    this.datos = null;
    this.service.obtener(this.tipo, this.id as number).subscribe({
      next: (resp) => {
        this.datos = (resp as any)?.comprobante ?? resp;
        this.cargando = false;
      },
      error: (err) => {
        this.cargando = false;
        this.error = err?.status === 404
          ? 'El comprobante no está disponible para este registro.'
          : 'Verifique la conexión con el servidor.';
      },
    });
  }
}
