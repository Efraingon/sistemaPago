import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { InformeService } from '../../core/services/informe.service';
import { BancoService } from '../../core/services/banco.service';
import { AuthService } from '../../core/auth.service';
import { Banco, Cliente } from '../../core/models';
import { Informe, InformeColumna, InformeDef, InformeParametro } from '../../core/models/informe';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { exportarCsv, fechaTexto, imprimir, numeroTexto } from '../../shared/export';

/** Visor genérico de informes: formulario de parámetros según el catálogo,
 *  documento imprimible (estilo reporte VFP) y exportación a CSV. */
@Component({
  selector: 'app-informe',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, ClienteAutocompleteComponent],
  template: `
    <div class="page-header no-print">
      <div>
        <p class="eyebrow">Centro de Reportes</p>
        <h1 class="page-title">{{ def?.nombre || 'Informe' }}</h1>
        <p class="page-sub">
          {{ def?.descripcion }}
          @if (def?.origenVfp) {
            <span class="chip chip-out origen-chip">Origen VFP: {{ def?.origenVfp }}</span>
          }
        </p>
      </div>
      <a class="btn btn-ghost btn-sm" routerLink="/reportes/centro">
        <app-icon name="chevronLeft" [size]="14" /> Volver al centro
      </a>
    </div>

    @if (cargandoDef) {
      <div class="card no-print"><div class="loading-panel"><span class="spinner"></span> Cargando informe…</div></div>
    } @else if (!def) {
      <div class="empty-state no-print">
        <span class="empty-ico"><app-icon name="alert" [size]="32" /></span>
        <strong>Informe no encontrado</strong>
        <span class="small">No existe «{{ id }}» en el catálogo.</span>
        <a class="btn btn-secondary btn-sm" routerLink="/reportes/centro">Volver al centro</a>
      </div>
    } @else {
      <!-- ------------------------------------------------ parámetros -->
      @if (def.parametros?.length) {
        <div class="toolbar no-print params-bar">
          @for (p of def.parametros; track p.clave) {
            <div class="form-group w-auto" [class.param-cliente]="p.tipo === 'cliente'">
              <label class="form-label" [for]="'p-' + p.clave">
                {{ p.etiqueta }}
                @if (p.requerido) { <span class="req">*</span> }
              </label>

              @switch (p.tipo) {
                @case ('fecha') {
                  <input
                    [id]="'p-' + p.clave"
                    class="input"
                    type="date"
                    [class.is-invalid]="faltan.has(p.clave)"
                    [(ngModel)]="valores[p.clave]"
                  />
                }
                @case ('cliente') {
                  <app-cliente-autocomplete
                    [cliente]="clienteSel"
                    (clienteChange)="alElegirCliente($event)"
                    [invalido]="faltan.has(p.clave)"
                  />
                }
                @case ('banco') {
                  <select
                    [id]="'p-' + p.clave"
                    class="input"
                    [class.is-invalid]="faltan.has(p.clave)"
                    [(ngModel)]="valores[p.clave]"
                  >
                    <option value="">Seleccione un banco…</option>
                    @for (b of bancos; track b.id) {
                      <option [value]="b.id">{{ b.nombre }}</option>
                    }
                  </select>
                }
                @case ('select') {
                  <select [id]="'p-' + p.clave" class="input" [(ngModel)]="valores[p.clave]">
                    @for (o of p.opciones; track o.valor) {
                      <option [value]="o.valor">{{ o.etiqueta }}</option>
                    }
                  </select>
                }
                @default {
                  <input [id]="'p-' + p.clave" class="input" type="text" [(ngModel)]="valores[p.clave]" />
                }
              }
            </div>
          }
          <div class="form-group w-auto param-btn">
            <label class="form-label">&nbsp;</label>
            <button class="btn btn-primary" type="button" [disabled]="ejecutando" (click)="ejecutar()">
              @if (ejecutando) { <span class="spinner"></span> Ejecutando… }
              @else { <app-icon name="chart" [size]="15" /> Ejecutar informe }
            </button>
          </div>
          @if (faltan.size) {
            <span class="msg-error">Complete los campos obligatorios (*).</span>
          }
        </div>
      } @else {
        // Sin parámetros definidos: ejecutar directamente con un clic
        <div class="toolbar no-print params-bar">
          <div class="form-group w-auto param-btn">
            <label class="form-label">&nbsp;</label>
            <button class="btn btn-primary" type="button" [disabled]="ejecutando" (click)="ejecutar()">
              @if (ejecutando) { <span class="spinner"></span> Ejecutando… }
              @else { <app-icon name="chart" [size]="15" /> Ejecutar informe }
            </button>
          </div>
        </div>
      }

      @if (error) {
        <div class="empty-state no-print">
          <span class="empty-ico"><app-icon name="alert" [size]="32" /></span>
          <strong>No se pudo generar el informe</strong>
          <span class="small">{{ error }}</span>
        </div>
      }

      @if (informe) {
        <!-- ------------------------------------------------ acciones -->
        <div class="acciones no-print">
          <span class="small tot-filas">{{ filasTotales }} filas · {{ informe.secciones.length }} sección(es)</span>
          <div class="row gap-2 wrap">
            <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
              <app-icon name="arrowDown" [size]="14" /> Exportar CSV (Excel)
            </button>
            <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
              <app-icon name="print" [size]="14" /> Imprimir / PDF
            </button>
          </div>
        </div>

        <!-- ------------------------------------------------ documento -->
        <div class="print-area papel">
          <header class="papel-head">
            <div class="papel-empresa">
              <strong>{{ informe.empresa.nombre }}</strong>
              @if (informe.empresa.rif) { <span>RIF: {{ informe.empresa.rif }}</span> }
              @if (informe.empresa.direccion) { <span>{{ informe.empresa.direccion }}</span> }
              @if (informe.empresa.telefono) { <span>Tel: {{ informe.empresa.telefono }}</span> }
            </div>
            <div class="papel-titulo">
              <h1>{{ informe.titulo }}</h1>
              @if (informe.subtitulo) { <h2>{{ informe.subtitulo }}</h2> }
              @if (informe.periodo) { <p class="papel-periodo">{{ informe.periodo }}</p> }
            </div>
          </header>

          @for (s of informe.secciones; track $index) {
            @if (s.nombre) { <h3 class="doc-seccion">{{ s.nombre }}</h3> }
            @if (!s.filas.length) {
              <p class="doc-vacio">Sin registros para este filtro.</p>
            } @else {
              <div class="doc-wrap">
                <table class="doc-table">
                  <thead>
                    <tr>
                      @for (c of s.columnas; track c.clave) {
                        <th [class.num]="esNum(c)">{{ c.etiqueta }}</th>
                      }
                    </tr>
                  </thead>
                  <tbody>
                    @for (fila of s.filas; track $index) {
                      <tr>
                        @for (c of s.columnas; track c.clave) {
                          <td [class.num]="esNum(c)">{{ fmt(c, fila[c.clave]) }}</td>
                        }
                      </tr>
                    }
                  </tbody>
                  @if (s.totales) {
                    <tfoot>
                      <tr class="fila-total">
                        @for (c of s.columnas; track c.clave) {
                          <td [class.num]="esNum(c)">
                            @if ($first) { TOTALES }
                            @else if (c.totalizable) { fmt(c, s.totales![c.clave]) }
                          </td>
                        }
                      </tr>
                    </tfoot>
                  }
                </table>
              </div>
            }
          }

          @if (informe.notas?.length) {
            <div class="doc-notas">
              @for (n of informe.notas; track $index) { <p>• {{ n.texto }}</p> }
            </div>
          }

          <footer class="papel-foot">
            <span>Generado por {{ usuario }} el {{ generadoTexto() }}</span>
            <span>{{ informe.empresa.nombre }}</span>
          </footer>
        </div>
      }
    }
  `,
  styles: [
    `
      .origen-chip {
        margin-inline-start: 8px;
        font-size: 11px;
        vertical-align: middle;
      }
      .params-bar {
        align-items: flex-end;
      }
      .param-cliente {
        min-width: 260px;
      }
      .param-btn {
        display: flex;
        flex-direction: column;
      }
      .req {
        color: #ef4444;
      }
      .msg-error {
        color: #ef4444;
        font-size: 12px;
        align-self: center;
      }
      .acciones {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        flex-wrap: wrap;
        margin-bottom: 14px;
      }
      .tot-filas {
        color: #64748b;
      }

      /* ------------------------------------------------ documento papel */
      .papel {
        background: #fff;
        color: #0f172a;
        border-radius: 14px;
        padding: 34px 38px 26px;
        box-shadow: 0 18px 40px rgba(2, 6, 23, 0.35);
        font-size: 13px;
      }
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
      .papel-titulo h2 {
        margin: 4px 0 0;
        font-size: 13.5px;
        font-weight: 600;
        color: #334155;
      }
      .papel-periodo {
        margin: 6px 0 0;
        font-size: 12px;
        color: #475569;
        font-family: var(--font-mono, 'IBM Plex Mono', monospace);
      }
      .doc-seccion {
        margin: 22px 0 8px;
        font-size: 12.5px;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: #0f172a;
        border-bottom: 2px solid #f59e0b;
        padding-bottom: 4px;
        break-after: avoid;
      }
      .doc-vacio {
        color: #64748b;
        font-style: italic;
        font-size: 12.5px;
      }
      .doc-wrap {
        overflow-x: auto;
      }
      .doc-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 12.5px;
      }
      .doc-table thead th {
        background: #0f172a;
        color: #fff;
        text-align: left;
        padding: 7px 10px;
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        white-space: nowrap;
      }
      .doc-table td {
        padding: 6px 10px;
        border-bottom: 1px solid #e2e8f0;
        color: #0f172a;
        vertical-align: top;
      }
      .doc-table tbody tr:nth-child(even) td {
        background: #f8fafc;
      }
      .doc-table .num,
      .doc-table th.num,
      .doc-table td.num {
        text-align: right;
        font-variant-numeric: tabular-nums;
        font-family: var(--font-mono, 'IBM Plex Mono', monospace);
        white-space: nowrap;
      }
      .doc-table .fila-total td {
        font-weight: 700;
        background: #eef2ff !important;
        border-top: 2px solid #0f172a;
        border-bottom: 0;
      }
      .doc-table tr {
        break-inside: avoid;
      }
      .doc-notas {
        margin-top: 16px;
        font-size: 11px;
        color: #475569;
      }
      .doc-notas p {
        margin: 2px 0;
      }
      .papel-foot {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        margin-top: 24px;
        padding-top: 10px;
        border-top: 1px solid #cbd5e1;
        font-size: 10.5px;
        color: #64748b;
      }

      /* ------------------------------------------------ impresión */
      @media print {
        .papel {
          background: #fff !important;
          color: #000 !important;
          box-shadow: none !important;
          border-radius: 0;
          padding: 0;
        }
        .papel-empresa strong,
        .papel-titulo h1,
        .doc-seccion {
          color: #000 !important;
        }
        .doc-table thead th {
          background: #e5e7eb !important;
          color: #000 !important;
          border: 1px solid #94a3b8;
        }
        .doc-table td {
          border-color: #cbd5e1 !important;
          color: #000 !important;
        }
        .doc-table tbody tr:nth-child(even) td {
          background: #f8fafc !important;
        }
        .doc-table .fila-total td {
          background: #e5e7eb !important;
          border-top: 2px solid #000 !important;
        }
      }
    `,
  ],
})
export class InformeComponent implements OnInit {
  id = '';
  def: InformeDef | null = null;
  cargandoDef = true;
  ejecutando = false;
  error = '';
  informe: Informe | null = null;
  valores: Record<string, string> = {};
  faltan = new Set<string>();
  clienteSel: Cliente | null = null;
  bancos: Banco[] = [];

  private readonly service = inject(InformeService);
  private readonly bancoService = inject(BancoService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.service.catalogo().subscribe({
      next: (cat) => {
        const defs = (cat?.familias ?? []).flatMap((f) => f.informes ?? []);
        this.def = defs.find((d) => d.id === this.id) ?? null;
        if (this.def) {
          this.initParams();
          if (this.def.parametros?.some((p) => p.tipo === 'banco')) {
            this.bancoService.listar().subscribe({ next: (b) => (this.bancos = b ?? []), error: () => (this.bancos = []) });
          }
        }
        this.cargandoDef = false;
      },
      error: () => {
        this.def = null;
        this.cargandoDef = false;
      },
    });
  }

  private initParams(): void {
    for (const p of this.def?.parametros ?? []) {
      this.valores[p.clave] = this.resolverDefecto(p);
    }
  }

  /** Resuelve los tokens de defecto del catálogo («hoy», «-30d», ISO…). */
  private resolverDefecto(p: InformeParametro): string {
    const d = (p.defecto ?? '').trim();
    const hoy = new Date();
    const iso = (f: Date): string =>
      `${f.getFullYear()}-${String(f.getMonth() + 1).padStart(2, '0')}-${String(f.getDate()).padStart(2, '0')}`;
    if (d === 'hoy') return iso(hoy);
    if (/^-?\d+d$/.test(d)) {
      const dias = Number(d.replace('d', ''));
      return iso(new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() + dias));
    }
    if (d) return d;
    if (p.tipo === 'select') return p.opciones?.[0]?.valor ?? '';
    return '';
  }

  alElegirCliente(c: Cliente | null): void {
    this.clienteSel = c;
    this.valores['clienteId'] = c?.id ?? '';
    if (c) this.faltan.delete('clienteId');
  }

  ejecutar(): void {
    if (!this.def) return;
    this.faltan.clear();
    for (const p of this.def.parametros ?? []) {
      const v = (this.valores[p.clave] ?? '').trim();
      if (p.requerido && !v) this.faltan.add(p.clave);
    }
    if (this.faltan.size) return;

    const params: Record<string, string> = {};
    for (const [k, v] of Object.entries(this.valores)) if (v && v.trim()) params[k] = v.trim();

    this.ejecutando = true;
    this.error = '';
    this.service.ejecutar(this.id, params).subscribe({
      next: (inf) => {
        this.informe = inf;
        this.ejecutando = false;
        // El documento es la zona imprimible: al terminar la carga enfocamos el scroll.
        setTimeout(() => {
          const el = document.querySelector('.papel');
          el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 60);
      },
      error: (e) => {
        this.informe = null;
        this.error = e?.error?.title || e?.error?.detail || 'Error al ejecutar el informe.';
        this.ejecutando = false;
      },
    });
  }

  esNum(c: InformeColumna): boolean {
    return c.tipo === 'moneda' || c.tipo === 'numero';
  }

  fmt(c: InformeColumna, valor: unknown): string {
    if (valor === null || valor === undefined || valor === '') return '';
    if (c.tipo === 'fecha') return fechaTexto(valor);
    if (c.tipo === 'moneda') return numeroTexto(valor, 2);
    if (c.tipo === 'numero') return numeroTexto(valor, 4);
    return String(valor);
  }

  get filasTotales(): number {
    return (this.informe?.secciones ?? []).reduce((n, s) => n + s.filas.length, 0);
  }

  get usuario(): string {
    const u = this.auth.usuario as any;
    return u?.nombre || u?.usuario || 'ADMIN';
  }

  generadoTexto(): string {
    const g = this.informe?.generado ? new Date(this.informe.generado) : new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${p(g.getDate())}/${p(g.getMonth() + 1)}/${g.getFullYear()} ${p(g.getHours())}:${p(g.getMinutes())}`;
  }

  imprimir(): void {
    imprimir();
  }

  /** CSV multi-sección: filas de encabezado por sección + totales. */
  exportar(): void {
    if (!this.informe) return;
    const secciones = this.informe.secciones;
    const maxCols = Math.max(1, ...secciones.map((s) => s.columnas.length));

    // Encabezados posicionales: primera etiqueta disponible por columna.
    const encabezados: string[] = [];
    for (const s of secciones) {
      s.columnas.forEach((c, i) => {
        if (!encabezados[i]) encabezados[i] = c.etiqueta;
      });
    }
    while (encabezados.length < maxCols) encabezados.push('');

    const filas: (string | number | null)[][] = [];
    for (const s of secciones) {
      if (s.nombre) filas.push([`— ${s.nombre} —`]);
      const fila = (registro: Record<string, unknown>): (string | number | null)[] =>
        s.columnas.map((c) => {
          const v = registro[c.clave];
          if (c.tipo === 'fecha') return fechaTexto(v);
          if (c.tipo === 'moneda' || c.tipo === 'numero') return typeof v === 'number' ? v : Number(v ?? 0);
          return (v as string | number | null) ?? '';
        });
      for (const f of s.filas) filas.push(fila(f));
      if (s.totales) {
        const t = fila(s.totales as Record<string, unknown>);
        t[0] = 'TOTALES';
        filas.push(t);
      }
    }
    for (const n of this.informe.notas ?? []) filas.push(['', n.texto]);

    const params = this.def?.parametros?.some((p) => p.clave === 'desde') ? this.valores['desde'] : '';
    const sufijo = params || new Date().toISOString().slice(0, 10);
    exportarCsv(`informe_${this.id}_${sufijo}`, encabezados, filas);
  }
}
