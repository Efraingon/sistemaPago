import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TransferenciaService } from '../../core/services/transferencia.service';
import { BancoService } from '../../core/services/banco.service';
import { MonedaService, TasaService } from '../../core/services/tasas.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Banco, Moneda, Transferencia } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';
import { Cliente } from '../../core/models';
import { exportarCsv, imprimir } from '../../shared/export';

/** Catálogo VFP de ORIGTRAN para movimientos manuales.
 *  CO/VE/CU (compra/venta/custodia) se generan solos desde las operaciones;
 *  aquí solo se registran depósitos, retiros y transferencias. */
const ORIGENES: { cod: string; label: string }[] = [
  { cod: 'DP', label: 'Depósito' },
  { cod: 'RT', label: 'Retiro' },
  { cod: 'T1', label: 'Transf. nacional — mismo titular' },
  { cod: 'T2', label: 'Transf. nacional — terceros' },
  { cod: 'T3', label: 'Transf. internacional — mismo titular' },
  { cod: 'T4', label: 'Transf. internacional — terceros' },
];

@Component({
  selector: 'app-transferencias',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    ModalComponent,
    IconComponent,
    ClienteAutocompleteComponent,
    FechaPipe,
    MontoPipe,
  ],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Tesorería</p>
        <h1 class="page-title">Transferencias</h1>
        <p class="page-sub">Movimientos bancarios con beneficiario, cuenta y motivo.</p>
      </div>
      <button class="btn btn-primary" type="button" (click)="nueva()">
        <app-icon name="plus" [size]="16" /> Nueva transferencia
      </button>
      <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
        <app-icon name="arrowDown" [size]="14" /> CSV
      </button>
      <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
        <app-icon name="print" [size]="14" /> Imprimir
      </button>
    </div>

    <!-- Filtros -->
    <div class="toolbar">
      <div class="form-group w-auto">
        <label class="form-label" for="desde">Desde</label>
        <input id="desde" class="input" type="date" [(ngModel)]="desde" (ngModelChange)="filtroCambio()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="hasta">Hasta</label>
        <input id="hasta" class="input" type="date" [(ngModel)]="hasta" (ngModelChange)="filtroCambio()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="bancoF">Banco</label>
        <select id="bancoF" class="select" [(ngModel)]="bancoFiltro" (ngModelChange)="filtroCambio()">
          <option [ngValue]="null">Todos</option>
          @for (b of bancos; track b.id) {
            <option [ngValue]="b.id">{{ b.nombre }}</option>
          }
        </select>
      </div>
      <div class="form-group grow" style="max-width: 240px">
        <label class="form-label">Cliente</label>
        <app-cliente-autocomplete [cliente]="clienteFiltro" (clienteChange)="onClienteFiltro($event)" />
      </div>
      <button class="btn btn-ghost" type="button" (click)="limpiar()">
        <app-icon name="refresh" [size]="15" /> Limpiar
      </button>
    </div>

    <!-- Tabla -->
    <div class="table-wrap print-area">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando transferencias…</div>
      } @else if (!items.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="transfer" [size]="36" /></span>
          <strong>Sin transferencias</strong>
          <span class="small">No hay registros para los filtros seleccionados.</span>
          <button class="btn btn-secondary btn-sm mt-1" type="button" (click)="nueva()">Registrar transferencia</button>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Origen</th>
              <th>Nº</th>
              <th>Beneficiario</th>
              <th>Banco</th>
              <th>Cliente</th>
              <th>Motivo</th>
              <th class="num">Monto</th>
              <th class="text-right">Acción</th>
            </tr>
          </thead>
          <tbody>
            @for (t of items; track t.id) {
              <tr [class.row-anulado]="t.anulado">
                <td class="mono">{{ t.fecha | fecha }}</td>
                <td><span class="chip" [class.chip-out]="!esIngreso(t)">{{ t.origen || '—' }}</span></td>
                <td class="mono">{{ t.numeroTran || '—' }}</td>
                <td>{{ t.beneficiario || '—' }}</td>
                <td>{{ t.banco || bancoNombre(t.bancoId) }}</td>
                <td>
                  {{ t.cliente || '—' }}
                  @if (t.anulado) {
                    <span class="badge badge-danger ms-1">ANULADO</span>
                  }
                </td>
                <td class="small text-muted">{{ t.motivo || nombreOrigen(t.origen) || '—' }}</td>
                <td class="num">
                  <span [class.text-success]="esIngreso(t)" [class.text-danger]="!esIngreso(t)">{{ (t.monto ?? 0) | monto }}</span>
                  @if (t.montoBs) {
                    <span class="small text-muted d-block">≈ {{ t.montoBs | monto }} Bs</span>
                  }
                </td>
                <td class="text-right">
                  <button
                    class="icon-btn danger"
                    type="button"
                    (click)="anular(t)"
                    [disabled]="t.anulado"
                    title="Anular"
                    aria-label="Anular transferencia"
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
        <div class="pager-btns">
          <button class="icon-btn" type="button" [disabled]="pagina <= 1" (click)="irPagina(pagina - 1)" aria-label="Anterior">
            <app-icon name="chevron-left" [size]="15" />
          </button>
          <button class="icon-btn" type="button" [disabled]="pagina >= paginas" (click)="irPagina(pagina + 1)" aria-label="Siguiente">
            <app-icon name="chevron-right" [size]="15" />
          </button>
        </div>
      </div>
    }

    <!-- Modal nueva -->
    <app-modal [visible]="modalNueva" title="Nueva transferencia" ancho="lg" (closed)="modalNueva = false">
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="origen">Origen <span class="req">*</span></label>
            <select id="origen" class="select" formControlName="origen">
              @for (o of origenes; track o.cod) {
                <option [value]="o.cod">{{ o.label }}</option>
              }
            </select>
          </div>
          <div class="form-group">
            <label class="form-label" for="tipoTran">Sentido <span class="req">*</span></label>
            <select id="tipoTran" class="select" formControlName="tipoTran">
              <option value="CR">Crédito — entra a la cuenta</option>
              <option value="DB">Débito — sale de la cuenta</option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label" for="fecha">Fecha <span class="req">*</span></label>
            <input id="fecha" class="input" type="date" formControlName="fecha" />
          </div>
        </div>

        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="numeroTran">Nº de referencia</label>
            <input id="numeroTran" class="input mono" formControlName="numeroTran" placeholder="00012345" />
          </div>
          <div class="form-group">
            <label class="form-label" for="cuentaTran">Cuenta de origen</label>
            <input id="cuentaTran" class="input mono" formControlName="cuentaTran" placeholder="0134-…" />
          </div>
          <div class="form-group">
            <label class="form-label" for="bancoId">Banco <span class="req">*</span></label>
            <select id="bancoId" class="select" formControlName="bancoId" [class.is-invalid]="fallo('bancoId')">
              <option [ngValue]="null">Seleccione…</option>
              @for (b of bancos; track b.id) {
                <option [ngValue]="b.id">{{ b.nombre }}</option>
              }
            </select>
            @if (fallo('bancoId')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Banco requerido</span>
            }
          </div>
        </div>

        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="beneficiario">Beneficiario <span class="req">*</span></label>
            <input id="beneficiario" class="input" formControlName="beneficiario" [class.is-invalid]="fallo('beneficiario')" placeholder="Nombre del beneficiario" />
            @if (fallo('beneficiario')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Requerido</span>
            }
          </div>
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
            @if (equivaleBs !== null) {
              <span class="form-hint">≈ {{ equivaleBs | monto }} Bs (tasa {{ tasaActiva | monto }})</span>
            }
          </div>
        </div>

        <div class="form-group mb-3">
          <span class="form-label">Cliente (opcional)</span>
          <div class="mt-1">
            <app-cliente-autocomplete [cliente]="formCliente" (clienteChange)="formCliente = $event" placeholder="Asociar a un cliente…" />
          </div>
        </div>

        <div class="form-group mb-4">
          <label class="form-label" for="motivo">Motivo <span class="req">*</span></label>
          <textarea id="motivo" class="textarea" formControlName="motivo" rows="2" [class.is-invalid]="fallo('motivo')" placeholder="Concepto de la transferencia"></textarea>
          @if (fallo('motivo')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Motivo requerido</span>
          }
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalNueva = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Registrar transferencia
            }
          </button>
        </div>
      </form>
    </app-modal>
  `,
  styles: [
    `
      .icon-btn:disabled {
        opacity: 0.35;
        cursor: not-allowed;
      }
      .row-anulado td {
        opacity: 0.55;
      }
      .chip-out {
        background: color-mix(in srgb, var(--danger, #ef4444) 14%, transparent);
        color: var(--danger, #ef4444);
      }
      .form-hint {
        display: block;
        margin-top: 4px;
        font-size: 0.72rem;
        color: var(--text-muted, #64748b);
      }
    `,
  ],
})
export class TransferenciasComponent implements OnInit {
  readonly origenes = ORIGENES;

  items: Transferencia[] = [];
  cargando = false;
  desde = '';
  hasta = '';
  bancoFiltro: string | null = null;
  clienteFiltro: Cliente | null = null;

  pagina = 1;
  tamano = 15;
  total = 0;

  modalNueva = false;
  guardando = false;
  formCliente: Cliente | null = null;

  bancos: Banco[] = [];
  monedas: Moneda[] = [];
  tasaActiva = 0;
  monedaNacionalId: string | null = null;

  private readonly service = inject(TransferenciaService);
  private readonly bancoService = inject(BancoService);
  private readonly monedaService = inject(MonedaService);
  private readonly tasaService = inject(TasaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    origen: ['T1', Validators.required],
    tipoTran: ['DB', Validators.required],
    fecha: [this.hoyIso(), Validators.required],
    numeroTran: [''],
    cuentaTran: [''],
    beneficiario: ['', Validators.required],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    bancoId: [null as string | null, Validators.required],
    monedaId: [null as string | null, Validators.required],
    motivo: ['', Validators.required],
  });

  ngOnInit(): void {
    this.cargar();
    this.bancoService.listar().subscribe({
      next: (b) => (this.bancos = Array.isArray(b) ? b : []),
      error: () => (this.bancos = []),
    });
    this.monedaService.listar().subscribe({
      next: (m) => {
        this.monedas = Array.isArray(m) ? m : [];
        // la moneda por defecto es la nacional (Bs) — no requiere conversión
        const nacional = this.monedas.find((x) => x.nacional) ?? this.monedas[0];
        if (nacional) {
          this.form.controls.monedaId.setValue(nacional.id);
          this.monedaNacionalId = nacional.id;
        }
      },
      error: () => (this.monedas = []),
    });
    this.tasaService.actual().subscribe({
      next: (t) => (this.tasaActiva = t?.tasaActiva ?? 0),
      error: () => (this.tasaActiva = 0),
    });
  }

  get paginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamano));
  }

  /** Equivalente en Bs del monto, solo si la moneda NO es la nacional. */
  get equivaleBs(): number | null {
    const v = this.form.getRawValue();
    const esDivisa = !!v.monedaId && v.monedaId !== this.monedaNacionalId;
    const monto = Number(v.monto) || 0;
    if (!esDivisa || !monto || !this.tasaActiva) return null;
    return Math.round(monto * this.tasaActiva * 100) / 100;
  }

  cargar(): void {
    this.cargando = true;
    this.service
      .listar({
        desde: this.desde,
        hasta: this.hasta,
        bancoId: this.bancoFiltro,
        clienteId: this.clienteFiltro?.id,
        pagina: this.pagina,
        tamano: this.tamano,
      })
      .subscribe({
        next: (pagina) => {
          this.items = pagina.items;
          this.total = pagina.total;
          this.cargando = false;
        },
        error: () => {
          this.items = [];
          this.total = 0;
          this.cargando = false;
        },
      });
  }

  filtroCambio(): void {
    this.pagina = 1;
    this.cargar();
  }

  irPagina(p: number): void {
    this.pagina = Math.min(Math.max(1, p), this.paginas);
    this.cargar();
  }

  onClienteFiltro(c: Cliente | null): void {
    this.clienteFiltro = c;
    this.filtroCambio();
  }

  bancoNombre(id?: string): string {
    return this.bancos.find((b) => b.id === id)?.nombre ?? '—';
  }

  nombreOrigen(cod?: string): string {
    return ORIGENES.find((o) => o.cod === cod)?.label ?? '';
  }

  /** TIPOTRAN CR = crédito (entra), DB = débito (sale). */
  esIngreso(t: Transferencia): boolean {
    return String(t.tipoTran ?? '').toUpperCase() === 'CR';
  }

  limpiar(): void {
    this.desde = '';
    this.hasta = '';
    this.bancoFiltro = null;
    this.clienteFiltro = null;
    this.filtroCambio();
  }

  nueva(): void {
    const nacional = this.monedas.find((x) => x.nacional) ?? this.monedas[0];
    this.form.reset({
      origen: 'T1',
      tipoTran: 'DB',
      fecha: this.hoyIso(),
      numeroTran: '',
      cuentaTran: '',
      beneficiario: '',
      monto: 0,
      bancoId: null,
      monedaId: nacional?.id ?? null,
      motivo: '',
    });
    this.formCliente = null;
    this.modalNueva = true;
  }

  fallo(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const esDivisa = !!v.monedaId && v.monedaId !== this.monedaNacionalId;
    const monto = Number(v.monto);
    this.guardando = true;
    this.service
      .crear({
        origen: v.origen,
        tipoTran: v.tipoTran,
        fecha: v.fecha,
        numeroTran: v.numeroTran,
        cuentaTran: v.cuentaTran,
        beneficiario: v.beneficiario,
        monto,
        bancoId: v.bancoId!,
        monedaId: v.monedaId!,
        clienteId: this.formCliente?.id,
        motivo: v.motivo,
        // conversión a Bs con la tasa activa cuando la moneda es divisa
        ...(esDivisa && this.tasaActiva
          ? { tasa: this.tasaActiva, usd: monto, montoBs: Math.round(monto * this.tasaActiva * 100) / 100 }
          : {}),
      })
      .subscribe({
        next: () => {
          this.guardando = false;
          this.modalNueva = false;
          this.toast.success('Transferencia registrada.');
          this.cargar();
        },
        error: () => (this.guardando = false),
      });
  }

  anular(t: Transferencia): void {
    if (t.anulado) return;
    const motivo = prompt('Motivo de anulación:');
    if (!motivo || !motivo.trim()) return;
    this.service.anular(t.id, motivo.trim()).subscribe({
      next: () => {
        this.toast.success('Transferencia anulada.');
        this.cargar();
      },
      error: () => undefined,
    });
  }

  /** Imprime el listado de transferencias (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta el listado de transferencias a CSV. */
  exportar(): void {
    const encabezados = ['Fecha', 'Origen', 'Nº', 'Beneficiario', 'Banco', 'Cliente', 'Motivo', 'Monto'];
    const filas = this.items.map((t) => [
      t.fecha,
      t.origen || '—',
      t.numeroTran || '—',
      t.beneficiario || '—',
      t.banco || this.bancoNombre(t.bancoId) || '—',
      t.cliente || '—',
      t.motivo || '',
      Number(t.monto || 0),
    ]);
    const sufijo = (this.desde || this.hasta) ? `${this.desde}_${this.hasta}` : new Date().toISOString().slice(0, 10);
    exportarCsv(`transferencias_${sufijo}`, encabezados, filas);
  }

  private hoyIso(): string {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
