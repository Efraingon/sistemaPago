import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CajaService } from '../../core/services/caja.service';
import { MonedaService } from '../../core/services/tasas.service';
import { ClienteService } from '../../core/services/cliente.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Arqueo, ArqueoDenominacion, CajaMovimiento, CodificacionOpcion, Moneda } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { ComprobanteComponent } from '../../shared/comprobante/comprobante.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';
import { Cliente } from '../../core/models';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-caja',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    ModalComponent,
    IconComponent,
    ClienteAutocompleteComponent,
    ComprobanteComponent,
    FechaPipe,
    MontoPipe,
  ],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Efectivo</p>
        <h1 class="page-title">Caja</h1>
        <p class="page-sub">Ingresos y egresos con codificación, arqueo y cierre diario.</p>
      </div>
      <div class="row gap-2 wrap">
        <button class="btn btn-ghost" type="button" (click)="abrirArqueo()">
          <app-icon name="banknote" [size]="16" /> Arqueo
        </button>
        <a class="btn btn-ghost" routerLink="/caja/cierre">
          <app-icon name="lock" [size]="16" /> Cierre
        </a>
        <button class="btn btn-primary" type="button" (click)="nuevo('IN')">
          <app-icon name="plus" [size]="16" /> Nuevo movimiento
        </button>
        <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
          <app-icon name="arrowDown" [size]="14" /> CSV
        </button>
        <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
          <app-icon name="print" [size]="14" /> Imprimir
        </button>
      </div>
    </div>

    <!-- Filtros -->
    <div class="toolbar">
      <div class="form-group w-auto">
        <label class="form-label" for="desde">Desde</label>
        <input id="desde" class="input" type="date" [(ngModel)]="desde" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="hasta">Hasta</label>
        <input id="hasta" class="input" type="date" [(ngModel)]="hasta" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="origen">Origen</label>
        <select id="origen" class="select" [(ngModel)]="origen" (ngModelChange)="cargar()">
          <option value="">Todos</option>
          <option value="IN">Ingresos (IN)</option>
          <option value="EG">Egresos (EG)</option>
        </select>
      </div>
      <div class="form-group grow" style="max-width: 260px">
        <label class="form-label">Cliente</label>
        <app-cliente-autocomplete [cliente]="filtroCliente" (clienteChange)="onFiltroCliente($event)" />
      </div>
      <button class="btn btn-ghost" type="button" (click)="limpiar()">
        <app-icon name="refresh" [size]="15" /> Limpiar
      </button>
    </div>

    <!-- Totales -->
    <div class="grid-3 mb-4">
      <article class="kpi">
        <span class="kpi-label"><app-icon name="arrowDown" [size]="14" /> Ingresos</span>
        <span class="kpi-value text-success">{{ totalIngresos | monto }}</span>
        <span class="kpi-sub">Bs — en el rango</span>
      </article>
      <article class="kpi">
        <span class="kpi-label"><app-icon name="arrowUp" [size]="14" /> Egresos</span>
        <span class="kpi-value text-danger">{{ totalEgresos | monto }}</span>
        <span class="kpi-sub">Bs — en el rango</span>
      </article>
      <article class="kpi">
        <span class="kpi-label"><app-icon name="wallet" [size]="14" /> Neto</span>
        <span class="kpi-value">{{ totalIngresos - totalEgresos | monto }}</span>
        <span class="kpi-sub">Bs</span>
      </article>
    </div>

    <!-- Tabla -->
    <div class="table-wrap print-area">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando movimientos…</div>
      } @else if (!items.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="wallet" [size]="36" /></span>
          <strong>Sin movimientos de caja</strong>
          <span class="small">No hay registros para los filtros seleccionados.</span>
          <button class="btn btn-secondary btn-sm mt-1" type="button" (click)="nuevo('IN')">Registrar ingreso</button>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Origen</th>
              <th>Descripción</th>
              <th>Cliente</th>
              <th>Moneda</th>
              <th class="num">Monto</th>
              <th class="text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (m of items; track m.id) {
              <tr [class.fila-anulada]="m.anulado">
                <td class="mono">{{ m.fecha | fecha }}</td>
                <td>
                  <span class="badge" [class.badge-success]="esIngreso(m)" [class.badge-danger]="!esIngreso(m)">
                    {{ etiquetaOrigen(m) }}
                  </span>
                  @if (m.anulado) {
                    <span class="badge badge-muted" style="margin-left: 6px">ANULADO</span>
                  }
                </td>
                <td>{{ m.descripcion || '—' }}</td>
                <td>{{ m.cliente || '—' }}</td>
                <td>{{ m.moneda || 'Bs' }}</td>
                <td class="num" [class.text-success]="esIngreso(m)" [class.text-danger]="!esIngreso(m)">
                  {{ (esIngreso(m) ? '+' : '-') + (m.monto | monto) }}
                </td>
                <td class="text-right" style="white-space: nowrap">
                  <button class="icon-btn" type="button" (click)="comprobante(m)" title="Comprobante" aria-label="Comprobante">
                    <app-icon name="print" [size]="15" />
                  </button>
                  <button
                    class="icon-btn danger"
                    type="button"
                    (click)="anular(m)"
                    [attr.title]="m.anulado ? 'Ya anulado' : m.cerrado ? 'Día cerrado: movimiento congelado' : 'Anular'"
                    [disabled]="m.anulado || m.cerrado"
                    aria-label="Anular movimiento"
                  >
                    <app-icon name="close" [size]="15" />
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>

    @if (total > tamano) {
      <div class="pagination">
        <span>Página <strong class="mono">{{ pagina }}</strong> · {{ total }} resultados</span>
        <div class="pages">
          <button class="icon-btn" type="button" [disabled]="pagina <= 1" (click)="irPagina(pagina - 1)" aria-label="Anterior">
            <app-icon name="chevronLeft" [size]="15" />
          </button>
          <button class="icon-btn" type="button" [disabled]="pagina >= paginas" (click)="irPagina(pagina + 1)" aria-label="Siguiente">
            <app-icon name="chevronRight" [size]="15" />
          </button>
        </div>
      </div>
    }

    <!-- ===== Modal nuevo movimiento ===== -->
    <app-modal [visible]="modalNuevo" [title]="form.controls.origen.value === 'IN' ? 'Nuevo ingreso' : 'Nuevo egreso'" ancho="lg" (closed)="modalNuevo = false">
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-row mb-3">
          <div class="form-group">
            <span class="form-label">Tipo de movimiento <span class="req">*</span></span>
            <div class="row gap-4 mt-1">
              <label class="check">
                <input type="radio" formControlName="origen" value="IN" />
                <span class="text-success">Ingreso (IN)</span>
              </label>
              <label class="check">
                <input type="radio" formControlName="origen" value="EG" />
                <span class="text-danger">Egreso (EG)</span>
              </label>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label" for="fecha">Fecha <span class="req">*</span></label>
            <input id="fecha" class="input" type="date" formControlName="fecha" />
          </div>
        </div>

        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="monto">Monto <span class="req">*</span></label>
            <input id="monto" class="input mono" type="number" step="0.01" min="0" formControlName="monto" [class.is-invalid]="fallo('monto')" placeholder="0,00" />
            @if (fallo('monto')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Monto mayor a cero</span>
            }
          </div>
          <div class="form-group">
            <label class="form-label" for="monedaId">Moneda <span class="req">*</span></label>
            <select id="monedaId" class="select" formControlName="monedaId" [class.is-invalid]="fallo('monedaId')">
              <option [ngValue]="null">Seleccione…</option>
              @for (m of monedas; track m.id) {
                <option [ngValue]="m.id">{{ m.descripcion }}</option>
              }
            </select>
            @if (fallo('monedaId')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Moneda requerida</span>
            }
          </div>
          <div class="form-group">
            <label class="form-label" for="codi">Código de codificación <span class="req">*</span></label>
            <select
              id="codi"
              class="select"
              [formControlName]="esIngresoForm ? 'codiIngresoId' : 'codiEgresoId'"
              [class.is-invalid]="fallo(esIngresoForm ? 'codiIngresoId' : 'codiEgresoId')"
            >
              <option [ngValue]="null">Seleccione…</option>
              @for (c of (esIngresoForm ? codificacionesIngreso : codificacionesEgreso); track c.id) {
                <option [ngValue]="c.id">{{ c.id }} — {{ c.descripcion }}</option>
              }
            </select>
            @if (fallo(esIngresoForm ? 'codiIngresoId' : 'codiEgresoId')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Código de codificación requerido</span>
            }
            <span class="form-hint">{{ esIngresoForm ? 'Código de ingreso' : 'Código de egreso' }} según tabla de codificación.</span>
          </div>
        </div>

        <div class="form-group mb-3">
          <label class="form-label" for="descripcion">Descripción <span class="req">*</span></label>
          <input id="descripcion" class="input" formControlName="descripcion" [class.is-invalid]="fallo('descripcion')" placeholder="Concepto del movimiento" />
          @if (fallo('descripcion')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Descripción requerida</span>
          }
        </div>

        <div class="form-group mb-4">
          <span class="form-label">Cliente (opcional)</span>
          <div class="mt-1">
            <app-cliente-autocomplete [cliente]="formCliente" (clienteChange)="formCliente = $event" placeholder="Asociar a un cliente…" />
          </div>
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalNuevo = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Registrar movimiento
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- ===== Modal arqueo ===== -->
    <app-modal [visible]="modalArqueo" title="Arqueo de caja" ancho="lg" (closed)="modalArqueo = false">
      <div class="form-group mb-3" style="max-width: 220px">
        <label class="form-label" for="arqueoFecha">Fecha</label>
        <input id="arqueoFecha" class="input" type="date" [(ngModel)]="arqueoFecha" (ngModelChange)="cargarArqueo()" />
      </div>

      @if (cargandoArqueo) {
        <div class="loading-panel"><span class="spinner"></span> Cargando arqueo…</div>
      } @else if (!arqueo) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="banknote" [size]="32" /></span>
          <strong>Sin denominaciones</strong>
          <span class="small">El backend no devolvió denominaciones para esta fecha.</span>
        </div>
      } @else {
        <div class="table-wrap mb-3">
          <table class="table">
            <thead>
              <tr>
                <th>Moneda</th>
                <th class="num">Valor</th>
                <th class="num" style="width: 150px">Cantidad</th>
                <th class="num">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              @for (d of arqueo.denominaciones; track d.id) {
                <tr>
                  <td>{{ d.moneda }}</td>
                  <td class="num">{{ d.valor | monto }}</td>
                  <td class="num">
                    <input
                      class="input mono"
                      type="number"
                      min="0"
                      style="text-align: right; padding: 6px 10px"
                      [(ngModel)]="d.cantidad"
                      (ngModelChange)="recalcular(d)"
                      aria-label="Cantidad de billetes"
                    />
                  </td>
                  <td class="num text-gold">{{ subtotal(d) | monto }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (arqueo.totalesPorMoneda?.length) {
          <div class="row gap-3 wrap mb-3">
            @for (t of arqueo.totalesPorMoneda; track t.moneda || t.descripcion) {
              <div class="total-chip">
                <span class="small text-muted">{{ t.moneda || t.descripcion }}</span>
                <strong class="mono">{{ (t.monto ?? t.total ?? 0) | monto }}</strong>
              </div>
            }
          </div>
        }

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalArqueo = false">Cerrar</button>
          <button class="btn btn-secondary" type="button" (click)="cargarArqueo()">
            <app-icon name="refresh" [size]="15" /> Actualizar
          </button>
        </div>
      }
    </app-modal>

    <!-- Comprobante -->
    <app-comprobante
      [visible]="modalComprobante"
      [tipo]="comprobanteTipo"
      [id]="comprobanteId"
      (closed)="modalComprobante = $event"
    />
  `,
  styles: [
    `
      .total-chip {
        display: flex;
        flex-direction: column;
        gap: 3px;
        padding: 10px 16px;
        border: 1px solid var(--color-border);
        border-radius: 9px;
        background: var(--color-muted);
        min-width: 150px;
      }
      .total-chip strong {
        font-size: 17px;
        color: var(--color-primary);
      }
      .icon-btn + .icon-btn {
        margin-inline-start: 6px;
      }
      .icon-btn:disabled {
        opacity: 0.35;
        cursor: not-allowed;
      }
      /* Movimiento anulado: conserva la fila para auditoría pero queda atenuada. */
      .fila-anulada td {
        opacity: 0.5;
      }
    `,
  ],
})
export class CajaComponent implements OnInit {
  items: CajaMovimiento[] = [];
  total = 0;
  pagina = 1;
  tamano = 15;
  desde = '';
  hasta = '';
  origen = '';
  filtroCliente: Cliente | null = null;
  cargando = false;

  modalNuevo = false;
  guardando = false;
  formCliente: Cliente | null = null;
  monedas: Moneda[] = [];
  codificacionesIngreso: CodificacionOpcion[] = [];
  codificacionesEgreso: CodificacionOpcion[] = [];

  modalArqueo = false;
  cargandoArqueo = false;
  arqueo: Arqueo | null = null;
  arqueoFecha = '';

  modalComprobante = false;
  comprobanteId: number | null = null;
  comprobanteTipo: 'ingreso' | 'egreso' = 'ingreso';

  private readonly service = inject(CajaService);
  private readonly monedaService = inject(MonedaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    origen: ['IN' as 'IN' | 'EG', Validators.required],
    fecha: [this.hoyIso(), Validators.required],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    monedaId: [null as string | null, Validators.required],
    descripcion: ['', Validators.required],
    // La codificación exigible depende del origen (se asigna en ngOnInit).
    codiIngresoId: [null as string | null],
    codiEgresoId: [null as string | null],
  });

  get paginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamano));
  }

  get esIngresoForm(): boolean {
    return this.form.controls.origen.value === 'IN';
  }

  get totalIngresos(): number {
    // KPIs siempre en Bs (montoBs propio) y excluyendo anulados.
    return this.items
      .filter((m) => !m.anulado && this.esIngreso(m))
      .reduce((s, m) => s + Number(m.montoBs ?? m.monto ?? 0), 0);
  }

  get totalEgresos(): number {
    return this.items
      .filter((m) => !m.anulado && !this.esIngreso(m))
      .reduce((s, m) => s + Number(m.montoBs ?? m.monto ?? 0), 0);
  }

  ngOnInit(): void {
    this.arqueoFecha = this.hoyIso();

    // Sólo la codificación del origen activo (IN/EG) es obligatoria.
    const aplicarValidadoresCodi = (): void => {
      const esIngreso = this.form.controls.origen.value === 'IN';
      this.form.controls.codiIngresoId.setValidators(esIngreso ? Validators.required : null);
      this.form.controls.codiEgresoId.setValidators(esIngreso ? null : Validators.required);
    };
    aplicarValidadoresCodi();
    this.form.controls.origen.valueChanges.subscribe(aplicarValidadoresCodi);

    this.cargar();
    this.monedaService.listar().subscribe({
      next: (m) => {
        this.monedas = Array.isArray(m) ? m : [];
        const principal = this.monedas.find((x) => x.principal) ?? this.monedas[0];
        if (principal) this.form.controls.monedaId.setValue(principal.id);
      },
      error: () => (this.monedas = []),
    });
    this.service.codificaciones().subscribe({
      next: (c) => {
        this.codificacionesIngreso = c?.ingresos ?? [];
        this.codificacionesEgreso = c?.egresos ?? [];
      },
      error: () => {
        this.codificacionesIngreso = [];
        this.codificacionesEgreso = [];
      },
    });
  }

  cargar(): void {
    this.cargando = true;
    this.service
      .listar({
        pagina: this.pagina,
        tamano: this.tamano,
        desde: this.desde,
        hasta: this.hasta,
        origen: this.origen,
        clienteId: this.filtroCliente?.id,
      })
      .subscribe({
        next: (page) => {
          if (Array.isArray(page)) {
            this.items = page as any;
            this.total = this.items.length;
          } else {
            this.items = page?.items ?? [];
            this.total = page?.total ?? this.items.length;
          }
          this.cargando = false;
        },
        error: () => {
          this.items = [];
          this.cargando = false;
        },
      });
  }

  onFiltroCliente(c: Cliente | null): void {
    this.filtroCliente = c;
    this.pagina = 1;
    this.cargar();
  }

  limpiar(): void {
    this.desde = '';
    this.hasta = '';
    this.origen = '';
    this.filtroCliente = null;
    this.pagina = 1;
    this.cargar();
  }

  irPagina(p: number): void {
    this.pagina = Math.min(Math.max(1, p), this.paginas);
    this.cargar();
  }

  /** Espejo de OrigenesCaja del backend: orígenes que suman valor a caja. */
  private static readonly ORIGENES_ENTRADA = ['IN', 'DE', 'RE', 'C1', 'VE', 'IB', 'T6'];

  esIngreso(m: CajaMovimiento): boolean {
    const o = String(m.origen ?? '').toUpperCase();
    return CajaComponent.ORIGENES_ENTRADA.includes(o) || m.tipo === 'ingreso';
  }

  etiquetaOrigen(m: CajaMovimiento): string {
    return this.esIngreso(m) ? 'Ingreso' : 'Egreso';
  }

  // ---------- Nuevo ----------
  nuevo(origen: 'IN' | 'EG'): void {
    const principal = this.monedas.find((x) => x.principal) ?? this.monedas[0];
    this.form.reset({
      origen,
      fecha: this.hoyIso(),
      monto: 0,
      monedaId: principal?.id ?? null,
      descripcion: '',
      codiIngresoId: null,
      codiEgresoId: null,
    });
    this.formCliente = null;
    this.modalNuevo = true;
  }

  fallo(campo: string): boolean {
    const control = this.form.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const esIngreso = v.origen === 'IN';
    const codi = esIngreso ? v.codiIngresoId : v.codiEgresoId;
    if (codi == null) {
      this.toast.error(`Indique el código de ${esIngreso ? 'ingreso' : 'egreso'}.`);
      return;
    }

    this.guardando = true;
    this.service
      .crear({
        origen: v.origen,
        fecha: v.fecha,
        monto: Number(v.monto),
        descripcion: v.descripcion,
        monedaId: v.monedaId!,
        clienteId: this.formCliente?.id,
        codiIngresoId: esIngreso ? codi : null,
        codiEgresoId: esIngreso ? null : codi,
      })
      .subscribe({
        next: () => {
          this.guardando = false;
          this.modalNuevo = false;
          this.toast.success(esIngreso ? 'Ingreso registrado.' : 'Egreso registrado.');
          this.cargar();
        },
        error: () => (this.guardando = false),
      });
  }

  // ---------- Arqueo ----------
  abrirArqueo(): void {
    this.modalArqueo = true;
    this.cargarArqueo();
  }

  cargarArqueo(): void {
    this.cargandoArqueo = true;
    this.arqueo = null;
    this.service.arqueo(this.arqueoFecha).subscribe({
      next: (a) => {
        this.arqueo = {
          fecha: a?.fecha ?? this.arqueoFecha,
          denominaciones: (a?.denominaciones ?? []).map((d) => ({ ...d, cantidad: Number(d.cantidad) || 0 })),
          totalesPorMoneda: a?.totalesPorMoneda ?? [],
        };
        this.cargandoArqueo = false;
      },
      error: () => {
        this.arqueo = null;
        this.cargandoArqueo = false;
      },
    });
  }

  recalcular(d: ArqueoDenominacion): void {
    d.cantidad = Math.max(0, Number(d.cantidad) || 0);
    d.subtotal = Math.round(d.valor * d.cantidad * 100) / 100;
    // Recalcular totales por moneda localmente
    if (this.arqueo) {
      const porMoneda = new Map<string, number>();
      for (const den of this.arqueo.denominaciones) {
        const key = den.moneda ?? 'Bs';
        porMoneda.set(key, (porMoneda.get(key) ?? 0) + den.subtotal);
      }
      this.arqueo.totalesPorMoneda = [...porMoneda.entries()].map(([moneda, total]) => ({ moneda, total }));
    }
  }

  subtotal(d: ArqueoDenominacion): number {
    return d.subtotal ?? Math.round(d.valor * d.cantidad * 100) / 100;
  }

  // ---------- Acciones ----------
  anular(m: CajaMovimiento): void {
    if (m.anulado) return;
    const motivo = prompt('Motivo de anulación:');
    if (!motivo || !motivo.trim()) return;
    this.service.anular(m.ejercicio, m.id, motivo.trim()).subscribe({
      next: () => {
        this.toast.success('Movimiento anulado.');
        this.cargar();
      },
      error: () => undefined,
    });
  }

  comprobante(m: CajaMovimiento): void {
    this.comprobanteId = m.id;
    this.comprobanteTipo = this.esIngreso(m) ? 'ingreso' : 'egreso';
    this.modalComprobante = true;
  }

  /** Imprime el listado de movimientos de caja (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta el listado de movimientos de caja a CSV. */
  exportar(): void {
    const encabezados = ['Fecha', 'Origen', 'Descripción', 'Cliente', 'Moneda', 'Monto'];
    const filas = this.items.map((m) => [
      m.fecha,
      this.etiquetaOrigen(m),
      m.descripcion || '',
      m.cliente || '',
      m.moneda || 'Bs',
      Number(m.monto || 0),
    ]);
    const sufijo = (this.desde || this.hasta) ? `${this.desde}_${this.hasta}` : new Date().toISOString().slice(0, 10);
    exportarCsv(`caja_movimientos_${sufijo}`, encabezados, filas);
  }

  private hoyIso(): string {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
