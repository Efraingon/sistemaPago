import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustodioService } from '../../core/services/custodio.service';
import { MonedaService } from '../../core/services/tasas.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Cliente, Custodio, CustodioMovimiento, Moneda } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { ComprobanteComponent } from '../../shared/comprobante/comprobante.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-custodios',
  standalone: true,
  imports: [
    ReactiveFormsModule,
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
        <p class="eyebrow">Resguardo</p>
        <h1 class="page-title">Custodios</h1>
        <p class="page-sub">Saldos disponibles por cliente y moneda con entradas y salidas documentadas.</p>
      </div>
      <div class="row gap-2 wrap">
        <button class="btn btn-secondary" type="button" (click)="accion('entregar')">
          <app-icon name="arrowUp" [size]="16" /> Entregar
        </button>
        <button class="btn btn-primary" type="button" (click)="accion('recibir')">
          <app-icon name="arrowDown" [size]="16" /> Recibir
        </button>
        <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
          <app-icon name="arrowDown" [size]="14" /> CSV
        </button>
        <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
          <app-icon name="print" [size]="14" /> Imprimir
        </button>
      </div>
    </div>

    <!-- Resumen -->
    <div class="grid-3 mb-4">
      <article class="kpi">
        <span class="kpi-label"><app-icon name="shield" [size]="14" /> Custodia total (Bs)</span>
        <span class="kpi-value">{{ totalBs | monto }}</span>
        <span class="kpi-sub">Suma de disponibles convertida</span>
      </article>
      <article class="kpi">
        <span class="kpi-label"><app-icon name="users" [size]="14" /> Clientes con custodia</span>
        <span class="kpi-value">{{ items.length }}</span>
        <span class="kpi-sub">Registros activos</span>
      </article>
      <article class="kpi">
        <span class="kpi-label"><app-icon name="coin" [size]="14" /> Monedas</span>
        <span class="kpi-value" style="font-size: 20px">{{ monedasTexto }}</span>
        <span class="kpi-sub">En resguardo</span>
      </article>
    </div>

    <!-- Tabla -->
    <div class="table-wrap print-area">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando custodios…</div>
      } @else if (!items.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="shield" [size]="36" /></span>
          <strong>Sin saldos de custodia</strong>
          <span class="small">Ningún cliente tiene activos en resguardo.</span>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Cliente</th>
              <th>Moneda</th>
              <th class="num">Disponible</th>
              <th class="num">Disponible (Bs)</th>
              <th class="text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (c of items; track c.clienteId + '-' + c.monedaId) {
              <tr class="row-click" (click)="verMovimientos(c)">
                <td>{{ c.cliente || 'Cliente #' + c.clienteId }}</td>
                <td>{{ c.moneda || '—' }}</td>
                <td class="num">{{ c.disponible | monto }}</td>
                <td class="num text-gold">{{ c.disponibleBs | monto }}</td>
                <td class="text-right" style="white-space: nowrap">
                  <button class="icon-btn" type="button" (click)="$event.stopPropagation(); verMovimientos(c)" title="Movimientos" aria-label="Ver movimientos">
                    <app-icon name="clock" [size]="15" />
                  </button>
                  <button class="icon-btn" type="button" (click)="$event.stopPropagation(); recibir(c)" title="Recibir" aria-label="Recibir en custodia">
                    <app-icon name="arrowDown" [size]="15" />
                  </button>
                  <button class="icon-btn" type="button" (click)="$event.stopPropagation(); entregar(c)" title="Entregar" aria-label="Entregar de custodia">
                    <app-icon name="arrowUp" [size]="15" />
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>

    <!-- Modal movimiento (recibir / entregar) -->
    <app-modal
      [visible]="modalMov"
      [title]="tipoMov === 'recibir' ? 'Recibir en custodia' : 'Entregar de custodia'"
      (closed)="modalMov = false"
    >
      <form [formGroup]="form" (ngSubmit)="guardarMov()" novalidate>
        <div class="form-group mb-3">
          <span class="form-label">Cliente <span class="req">*</span></span>
          <div class="mt-1">
            <app-cliente-autocomplete [cliente]="formCliente" [invalido]="clienteFallo" (clienteChange)="onCliente($event)" />
          </div>
          @if (clienteFallo) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Seleccione un cliente</span>
          }
        </div>

        <div class="form-row mb-3">
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
            <label class="form-label" for="monto">Monto <span class="req">*</span></label>
            <input id="monto" class="input mono" type="number" step="0.01" min="0" formControlName="monto" [class.is-invalid]="fallo('monto')" placeholder="0,00" />
            @if (fallo('monto')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Monto mayor a cero</span>
            }
          </div>
        </div>

        <div class="form-group mb-4">
          <label class="form-label" for="descripcion">Descripción</label>
          <textarea id="descripcion" class="textarea" formControlName="descripcion" rows="2" placeholder="Referencia del resguardo"></textarea>
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalMov = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Registrando…
            } @else {
              {{ tipoMov === 'recibir' ? 'Registrar recepción' : 'Registrar entrega' }}
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- Modal movimientos -->
    <app-modal [visible]="modalHist" [title]="'Movimientos de custodia — ' + (clienteNombre || '')" ancho="lg" (closed)="modalHist = false">
      @if (cargandoHist) {
        <div class="loading-panel"><span class="spinner"></span> Cargando movimientos…</div>
      } @else if (!historial.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="inbox" [size]="32" /></span>
          <strong>Sin movimientos</strong>
          <span class="small">Este cliente no registra entradas ni salidas de custodia.</span>
        </div>
      } @else {
        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Tipo</th>
                <th>Moneda</th>
                <th>Descripción</th>
                <th class="num">Monto</th>
                <th class="num">Saldo</th>
                <th class="text-right" style="white-space: nowrap">Comprobante</th>
              </tr>
            </thead>
            <tbody>
              @for (h of historial; track h.id ?? $index) {
                <tr>
                  <td class="mono">{{ h.fecha | fecha }}</td>
                  <td>
                    <span class="badge" [class.badge-success]="esEntrada(h)" [class.badge-danger]="!esEntrada(h)">
                      {{ etiquetaHist(h) }}
                    </span>
                  </td>
                  <td>{{ nombreMoneda(h.monedaId) }}</td>
                  <td>{{ h.descripcion || '—' }}</td>
                  <td class="num" [class.text-success]="esEntrada(h)" [class.text-danger]="!esEntrada(h)">
                    {{ h.monto | monto }}
                  </td>
                  <td class="num">{{ (h.saldo ?? 0) | monto }}</td>
                  <td class="text-right" style="white-space: nowrap">
                    <button
                      class="icon-btn"
                      type="button"
                      (click)="$event.stopPropagation(); abrirComprobanteCustodio(h)"
                      title="Comprobante"
                      aria-label="Comprobante de movimiento"
                    >
                      <app-icon name="print" [size]="14" />
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
      <div class="row gap-2 mt-3" style="justify-content: flex-end">
        <button class="btn btn-ghost" type="button" (click)="modalHist = false">Cerrar</button>
      </div>
    </app-modal>

    <!-- Comprobante de movimiento de custodia -->
    <app-comprobante
      [visible]="modalComprobante"
      [tipo]="comprobanteTipo"
      [id]="comprobanteId"
      (closed)="modalComprobante = false"
    />
  `,
  styles: [
    `
      .icon-btn + .icon-btn {
        margin-inline-start: 6px;
      }
    `,
  ],
})
export class CustodiosComponent implements OnInit {
  items: Custodio[] = [];
  monedas: Moneda[] = [];
  cargando = false;

  modalMov = false;
  tipoMov: 'recibir' | 'entregar' = 'recibir';
  guardando = false;
  formCliente: Cliente | null = null;
  clienteFallo = false;

  modalHist = false;
  cargandoHist = false;
  historial: CustodioMovimiento[] = [];
  clienteNombre = '';

  private readonly service = inject(CustodioService);
  private readonly monedaService = inject(MonedaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    monedaId: [null as string | null, Validators.required],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    descripcion: [''],
  });

  get totalBs(): number {
    return this.items.reduce((s, c) => s + Number(c.disponibleBs || 0), 0);
  }

  get monedasTexto(): string {
    const set = new Set(this.items.map((c) => c.moneda).filter(Boolean));
    return set.size ? [...set].join(' · ') : '—';
  }

  ngOnInit(): void {
    this.cargar();
    this.monedaService.listar().subscribe({
      next: (m) => {
        this.monedas = Array.isArray(m) ? m : [];
        const principal = this.monedas.find((x) => x.principal) ?? this.monedas[0];
        if (principal) this.form.controls.monedaId.setValue(principal.id);
      },
      error: () => (this.monedas = []),
    });
  }

  cargar(): void {
    this.cargando = true;
    this.service.listar().subscribe({
      next: (lista) => {
        this.items = Array.isArray(lista) ? lista : ((lista as any)?.items ?? []);
        this.cargando = false;
      },
      error: () => {
        this.items = [];
        this.cargando = false;
      },
    });
  }

  // ---------- Movimientos (recibir / entregar) ----------
  accion(tipo: 'recibir' | 'entregar'): void {
    this.tipoMov = tipo;
    this.formCliente = null;
    this.clienteFallo = false;
    const principal = this.monedas.find((x) => x.principal) ?? this.monedas[0];
    this.form.reset({ monedaId: principal?.id ?? null, monto: 0, descripcion: '' });
    this.modalMov = true;
  }

  recibir(c: Custodio): void {
    this.accion('recibir');
    this.formCliente = { id: c.clienteId, nombre: c.cliente, persona: 'N', activo: true } as Cliente;
    this.form.controls.monedaId.setValue(c.monedaId);
  }

  entregar(c: Custodio): void {
    this.accion('entregar');
    this.formCliente = { id: c.clienteId, nombre: c.cliente, persona: 'N', activo: true } as Cliente;
    this.form.controls.monedaId.setValue(c.monedaId);
  }

  onCliente(c: Cliente | null): void {
    this.formCliente = c;
    this.clienteFallo = !c;
  }

  fallo(campo: string): boolean {
    const ctrl = this.form.get(campo);
    return !!ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  guardarMov(): void {
    this.clienteFallo = !this.formCliente;
    if (this.form.invalid || this.clienteFallo) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body = {
      clienteId: this.formCliente!.id,
      monedaId: v.monedaId!,
      monto: Number(v.monto),
      descripcion: v.descripcion,
    };
    this.guardando = true;
    const req = this.tipoMov === 'recibir' ? this.service.recibir(body) : this.service.entregar(body);
    req.subscribe({
      next: () => {
        this.guardando = false;
        this.modalMov = false;
        this.toast.success(this.tipoMov === 'recibir' ? 'Recepción registrada.' : 'Entrega registrada.');
        this.cargar();
      },
      error: () => (this.guardando = false),
    });
  }

  // ---------- Historial ----------
  verMovimientos(c: Custodio): void {
    this.clienteNombre = c.cliente || `Cliente #${c.clienteId}`;
    this.modalHist = true;
    this.cargandoHist = true;
    this.historial = [];
    this.service.movimientos(c.clienteId).subscribe({
      next: (lista) => {
        this.historial = Array.isArray(lista) ? lista : ((lista as any)?.items ?? []);
        this.cargandoHist = false;
      },
      error: () => {
        this.historial = [];
        this.cargandoHist = false;
      },
    });
  }

  esEntrada(h: CustodioMovimiento): boolean {
    // normalizado en el servicio: 'entrada' | 'salida'
    return h.tipo === 'entrada';
  }

  /** Imprime el listado de saldos de custodia (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta los saldos de custodia a CSV. */
  exportar(): void {
    const encabezados = ['Cliente', 'Moneda', 'Disponible', 'Disponible (Bs)'];
    const filas = this.items.map((c) => [
      c.cliente || '—',
      c.moneda || '—',
      Number(c.disponible || 0),
      Number(c.disponibleBs || 0),
    ]);
    filas.push(['TOTALES', '', this.items.reduce((s, c) => s + Number(c.disponible || 0), 0), this.items.reduce((s, c) => s + Number(c.disponibleBs || 0), 0)]);
    exportarCsv('custodios_saldos', encabezados, filas);
  }

  etiquetaHist(h: CustodioMovimiento): string {
    return h.tipo === 'entrada' ? 'Entrada' : 'Salida';
  }

  // ---------- Comprobante de movimiento de custodia ----------
  modalComprobante = false;
  comprobanteTipo: 'entrega' | 'recepcion' = 'recepcion';
  comprobanteId: string | number | null = null;

  abrirComprobanteCustodio(h: CustodioMovimiento): void {
    this.comprobanteTipo = h.tipo === 'entrada' ? 'recepcion' : 'entrega';
    this.comprobanteId = h.id ?? null;
    this.modalComprobante = true;
  }

  nombreMoneda(id?: string): string {
    if (!id) return '—';
    return this.monedas.find((m) => m.id === id)?.descripcion ?? id;
  }
}
