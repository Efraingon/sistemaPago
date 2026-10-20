import { Component, OnInit, inject } from '@angular/core';
import { TitleCasePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { OperacionService } from '../../core/services/operacion.service';
import { TasaService, MonedaService } from '../../core/services/tasas.service';
import { BancoService } from '../../core/services/banco.service';
import { ClienteService } from '../../core/services/cliente.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Banco, Cliente, Cuota, Operacion, OperacionDetalle, TipoOperacion } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ClienteAutocompleteComponent } from '../../shared/cliente-autocomplete/cliente-autocomplete.component';
import { ComprobanteComponent } from '../../shared/comprobante/comprobante.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';

@Component({
  selector: 'app-operaciones',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    ModalComponent,
    IconComponent,
    ClienteAutocompleteComponent,
    ComprobanteComponent,
    FechaPipe,
    MontoPipe,
    TitleCasePipe,
  ],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Operación</p>
        <h1 class="page-title">{{ esCompra ? 'Compras de divisa' : 'Ventas de divisa' }}</h1>
        <p class="page-sub">{{ esCompra ? 'Compra de USD a clientes con registro de recepción y pago.' : 'Venta de USD a clientes con registro de entregas y cobros.' }}</p>
      </div>
      <button class="btn btn-primary" type="button" (click)="nueva()">
        <app-icon name="plus" [size]="16" /> Nueva {{ esCompra ? 'compra' : 'venta' }}
      </button>
    </div>

    <!-- Filtros -->
    <div class="toolbar">
      <div class="form-group grow" style="max-width: 300px">
        <label class="form-label">Cliente</label>
        <app-cliente-autocomplete [cliente]="filtroCliente" (clienteChange)="filtroCliente = $event" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="desde">Desde</label>
        <input id="desde" class="input" type="date" [(ngModel)]="desde" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="hasta">Hasta</label>
        <input id="hasta" class="input" type="date" [(ngModel)]="hasta" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="estado">Estado</label>
        <select id="estado" class="select" [(ngModel)]="estado" (ngModelChange)="cargar()">
          <option value="">Todos</option>
          <option value="pendiente">Pendiente</option>
          <option value="completada">Completada</option>
          <option value="anulada">Anulada</option>
        </select>
      </div>
      <button class="btn btn-ghost" type="button" (click)="limpiar()">
        <app-icon name="refresh" [size]="15" /> Limpiar
      </button>
    </div>

    <!-- Tabla -->
    <div class="table-wrap">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando {{ esCompra ? 'compras' : 'ventas' }}…</div>
      } @else if (!items.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="swap" [size]="36" /></span>
          <strong>Sin {{ esCompra ? 'compras' : 'ventas' }} registradas</strong>
          <span class="small">Ajuste los filtros o registre una operación nueva.</span>
          <button class="btn btn-secondary btn-sm mt-1" type="button" (click)="nueva()">Registrar operación</button>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Cliente</th>
              <th class="num">USD</th>
              <th class="num">Tasa</th>
              <th class="num">Monto Bs</th>
              <th>Estado</th>
              <th class="text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (op of items; track op.id) {
              <tr>
                <td class="mono">{{ op.fecha | fecha }}</td>
                <td>{{ op.cliente || '—' }}</td>
                <td class="num">{{ op.cantidad | monto }}</td>
                <td class="num">{{ op.tasa | monto }}</td>
                <td class="num">{{ (op.montoBs ?? montoDe(op)) | monto }}</td>
                <td>
                  <span
                    class="badge"
                    [class.badge-success]="op.estado === 'completada'"
                    [class.badge-gold]="op.estado === 'pendiente'"
                    [class.badge-danger]="op.estado === 'anulada'"
                  >
                    {{ op.estado || 'pendiente' }}
                  </span>
                </td>
                <td class="text-right" style="white-space: nowrap">
                  <button class="icon-btn" type="button" (click)="verDetalle(op)" title="Detalle" aria-label="Ver detalle">
                    <app-icon name="eye" [size]="15" />
                  </button>
                  <button class="icon-btn" type="button" (click)="abrirComprobante(op)" title="Comprobante" aria-label="Comprobante">
                    <app-icon name="print" [size]="15" />
                  </button>
                  <button
                    class="icon-btn danger"
                    type="button"
                    (click)="anular(op)"
                    title="Anular"
                    [disabled]="op.estado === 'anulada'"
                    aria-label="Anular operación"
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
        <span>
          Página <strong class="mono">{{ pagina }}</strong> · {{ total }} resultados
        </span>
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

    <!-- ===== Modal nueva operación ===== -->
    <app-modal
      [visible]="modalNueva"
      [title]="'Nueva ' + (esCompra ? 'compra' : 'venta')"
      ancho="lg"
      (closed)="modalNueva = false"
    >
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-group mb-3">
          <span class="form-label">Cliente <span class="req">*</span></span>
          <div class="mt-1">
            <app-cliente-autocomplete
              [cliente]="formSelCliente"
              [invalido]="clienteFallo"
              (clienteChange)="onCliente($event)"
            />
          </div>
          @if (clienteFallo) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Seleccione un cliente</span>
          }
        </div>

        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="fecha">Fecha <span class="req">*</span></label>
            <input id="fecha" class="input" type="date" formControlName="fecha" />
          </div>
          <div class="form-group">
            <label class="form-label" for="tasa">Tasa (Bs por USD) <span class="req">*</span></label>
            <input
              id="tasa"
              class="input mono"
              type="number"
              step="0.01"
              min="0"
              formControlName="tasa"
              (focus)="!tasaTocada && aplicarTasaActiva()"
            />
            <span class="form-hint">Tasa del día: <span class="mono text-gold">{{ tasaActiva | monto }}</span> (clic para aplicar)</span>
          </div>
          <div class="form-group">
            <label class="form-label" for="cantidad">Cantidad USD <span class="req">*</span></label>
            <input
              id="cantidad"
              class="input mono"
              type="number"
              step="0.01"
              min="0"
              formControlName="cantidad"
              [class.is-invalid]="fallo('cantidad')"
              (focus)="tasaTocada = true"
            />
            @if (fallo('cantidad')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Cantidad mayor a cero</span>
            }
          </div>
        </div>

        <!-- Monto calculado en vivo -->
        <div class="monto-vivo">
          <span class="mv-label">Monto calculado</span>
          <span class="mv-valor mono">{{ montoBsVivo | monto }} <small>Bs</small></span>
          <span class="mv-formula mono">{{ (form.controls.cantidad.value || 0) | monto }} USD × {{ (form.controls.tasa.value || 0) | monto }}</span>
        </div>

        <div class="form-group mt-3 mb-4">
          <label class="form-label" for="obs">Observaciones</label>
          <textarea id="obs" class="textarea" formControlName="observaciones" rows="2" placeholder="Referencia, banco, notas internas…"></textarea>
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalNueva = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Registrar {{ esCompra ? 'compra' : 'venta' }}
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- ===== Modal detalle ===== -->
    <app-modal
      [visible]="modalDetalle"
      [title]="'Detalle de ' + (esCompra ? 'compra' : 'venta') + (detalle ? ' #' + detalle.id : '')"
      ancho="xl"
      (closed)="modalDetalle = false"
    >
      @if (detalle) {
        <div class="detalle-grid mb-4">
          <div class="detalle-item">
            <div class="dt">Cliente</div>
            <div class="dd">{{ detalle.cliente || nombreCliente }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Fecha</div>
            <div class="dd mono">{{ detalle.fecha | fecha }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Cantidad USD</div>
            <div class="dd mono">{{ detalle.cantidad | monto }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Tasa</div>
            <div class="dd mono">{{ detalle.tasa | monto }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Monto Bs</div>
            <div class="dd mono text-gold">{{ (detalle.montoBs ?? montoDe(detalle)) | monto }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Estado</div>
            <div class="dd">
              <span
                class="badge"
                [class.badge-success]="detalle.estado === 'completada'"
                [class.badge-gold]="detalle.estado === 'pendiente'"
                [class.badge-danger]="detalle.estado === 'anulada'"
              >
                {{ detalle.estado || 'pendiente' }}
              </span>
            </div>
          </div>
        </div>

        <!-- Cuotas -->
        <h4 class="subtitulo mb-2">{{ esCompra ? 'Recepciones' : 'Entregas' }} y pagos</h4>
        @if (cargandoDetalle) {
          <div class="loading-panel" style="padding: 22px"><span class="spinner"></span></div>
        } @else if (!cuotas.length) {
          <div class="empty-state" style="padding: 26px">
            <span class="empty-ico"><app-icon name="clock" [size]="30" /></span>
            <strong>Sin cuotas registradas</strong>
            <span class="small">Use las acciones para registrar {{ esCompra ? 'recepciones' : 'entregas' }} y pagos.</span>
          </div>
        } @else {
          <div class="table-wrap mb-3">
            <table class="table">
              <thead>
                <tr>
                  <th>Tipo</th>
                  <th>Fecha</th>
                  <th class="num">Monto</th>
                  <th>Origen</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                @for (c of cuotas; track c.id) {
                  <tr>
                    <td>
                      <span class="badge" [class.badge-accent]="esPago(c)" [class.badge-gold]="!esPago(c)">
                        {{ etiquetaCuota(c) }}
                      </span>
                    </td>
                    <td class="mono">{{ c.fecha | fecha }}</td>
                    <td class="num">{{ (c.monto ?? c.cantidad ?? c.montoBs ?? 0) | monto }}</td>
                    <td>
                      {{ c.banco || c.bancoDebito || c.bancoCredito || (c.deCaja ? 'Caja' : null) || (c.tipoPago ? (c.tipoPago | titlecase) : null) || '—' }}
                    </td>
                    <td>{{ c.estado || 'registrada' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }

        <!-- Acciones -->
        @if (detalle.estado !== 'anulada') {
          <div class="acciones-det">
            <button class="btn btn-secondary btn-sm" type="button" (click)="abrirCuota('recepcion')">
              <app-icon name="arrowDown" [size]="15" />
              {{ esCompra ? 'Registrar recepción' : 'Registrar entrega' }}
            </button>
            <button class="btn btn-secondary btn-sm" type="button" (click)="abrirCuota('pago')">
              <app-icon name="banknote" [size]="15" />
              {{ esCompra ? 'Registrar pago' : 'Registrar cobro' }}
            </button>
            <button class="btn btn-ghost btn-sm" type="button" (click)="abrirComprobante(detalle)">
              <app-icon name="print" [size]="15" /> Comprobante
            </button>
            <button class="btn btn-danger btn-sm" type="button" (click)="anular(detalle)">
              <app-icon name="close" [size]="15" /> Anular
            </button>
          </div>
        }
      }
    </app-modal>

    <!-- ===== Modal cuota (recepción / pago) ===== -->
    <app-modal [visible]="modalCuota" [title]="tipoCuota === 'recepcion' ? tituloCuota : tituloPago" (closed)="modalCuota = false">
      <form [formGroup]="formCuota" (ngSubmit)="guardarCuota()" novalidate>
        <div class="form-row-2 form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="cfecha">Fecha <span class="req">*</span></label>
            <input id="cfecha" class="input" type="date" formControlName="fecha" />
          </div>
          <div class="form-group">
            <label class="form-label" for="cmonto">{{ tipoCuota === 'recepcion' ? 'Cantidad USD' : 'Monto a pagar' }} <span class="req">*</span></label>
            <input id="cmonto" class="input mono" type="number" step="0.01" min="0" formControlName="monto" [class.is-invalid]="falloCuota('monto')" />
            @if (falloCuota('monto')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Monto mayor a cero</span>
            }
          </div>
        </div>

        @if (tipoCuota === 'recepcion') {
          <label class="check mb-3">
            <input type="checkbox" formControlName="deCaja" />
            {{ esCompra ? 'Recepción' : 'Entrega' }} desde caja
          </label>
        } @else {
          <div class="form-row-2 form-row mb-3">
            <div class="form-group">
              <label class="form-label" for="debito">Banco a debitar</label>
              <select id="debito" class="select" formControlName="bancoDebitoId">
                <option [ngValue]="null">— Caja —</option>
                @for (b of bancos; track b.id) {
                  <option [ngValue]="b.id">{{ b.nombre }}</option>
                }
              </select>
            </div>
            <div class="form-group">
              <label class="form-label" for="credito">Banco a acreditar</label>
              <select id="credito" class="select" formControlName="bancoCreditoId">
                <option [ngValue]="null">—</option>
                @for (b of bancos; track b.id) {
                  <option [ngValue]="b.id">{{ b.nombre }}</option>
                }
              </select>
            </div>
          </div>
          <div class="form-group mb-3">
            <label class="form-label" for="tipoPago">Tipo de pago</label>
            <select id="tipoPago" class="select" formControlName="tipoPago">
              <option value="transferencia">Transferencia</option>
              <option value="efectivo">Efectivo</option>
              <option value="cheque">Cheque</option>
            </select>
          </div>
        }

        @if (tipoCuota === 'recepcion') {
          <div class="form-group mb-3">
            <label class="form-label" for="cbanco">Banco (si no es de caja)</label>
            <select id="cbanco" class="select" formControlName="bancoId">
              <option [ngValue]="null">—</option>
              @for (b of bancos; track b.id) {
                <option [ngValue]="b.id">{{ b.nombre }}</option>
              }
            </select>
          </div>
        }

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalCuota = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardandoCuota">
            @if (guardandoCuota) {
              <span class="spinner" style="width:16px;height:16px"></span> Registrando…
            } @else {
              Registrar
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- ===== Comprobante ===== -->
    <app-comprobante
      [visible]="modalComprobante"
      [tipo]="tipo"
      [id]="comprobanteId"
      (closed)="modalComprobante = $event"
    />
  `,
  styles: [
    `
      .monto-vivo {
        display: flex;
        align-items: baseline;
        gap: 16px;
        flex-wrap: wrap;
        padding: 16px 18px;
        background: rgba(245, 158, 11, 0.07);
        border: 1px solid rgba(245, 158, 11, 0.32);
        border-radius: 10px;
      }
      .mv-label {
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        color: var(--color-muted-foreground);
      }
      .mv-valor {
        font-size: 28px;
        font-weight: 600;
        color: var(--color-primary);
        letter-spacing: -0.02em;
        line-height: 1;
      }
      .mv-valor small {
        font-size: 14px;
        color: var(--color-muted-foreground);
        letter-spacing: 0.06em;
      }
      .mv-formula {
        margin-inline-start: auto;
        font-size: 12.5px;
        color: var(--color-muted-foreground);
      }
      .subtitulo {
        font-size: 13.5px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--color-muted-foreground);
      }
      .acciones-det {
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
        padding-top: 16px;
        border-top: 1px solid var(--color-border);
      }
      .icon-btn + .icon-btn {
        margin-inline-start: 6px;
      }
      .icon-btn:disabled {
        opacity: 0.35;
        cursor: not-allowed;
      }
    `,
  ],
})
export class OperacionesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  /** 'compra' | 'venta' según el data de la ruta (capa de datos). */
  readonly tipo: TipoOperacion = (this.route.snapshot.data['tipo'] as TipoOperacion) || 'compra';
  /** Bandera para la plantilla (¡no el string: ambos serían truthy!). */
  readonly esCompra = this.tipo === 'compra';

  items: Operacion[] = [];
  total = 0;
  pagina = 1;
  tamano = 10;
  desde = '';
  hasta = '';
  estado = '';
  filtroCliente: Cliente | null = null;
  cargando = false;

  // Nueva
  modalNueva = false;
  guardando = false;
  formSelCliente: Cliente | null = null;
  clienteFallo = false;
  tasaActiva = 0;
  tasaTocada = false;

  // Detalle
  modalDetalle = false;
  detalle: OperacionDetalle | null = null;
  nombreCliente = '';
  cuotas: Cuota[] = [];
  cargandoDetalle = false;

  // Cuota
  modalCuota = false;
  guardandoCuota = false;
  tipoCuota: 'recepcion' | 'pago' = 'recepcion';

  // Comprobante
  modalComprobante = false;
  comprobanteId: number | null = null;

  bancos: Banco[] = [];

  private readonly fb = inject(FormBuilder);
  private readonly service = inject(OperacionService);
  private readonly tasaService = inject(TasaService);
  private readonly bancoService = inject(BancoService);
  private readonly toast = inject(ToastService);

  readonly form = this.fb.nonNullable.group({
    fecha: [this.hoyIso(), Validators.required],
    tasa: [0, [Validators.required, Validators.min(0.01)]],
    cantidad: [0, [Validators.required, Validators.min(0.01)]],
    observaciones: [''],
  });

  readonly formCuota = this.fb.nonNullable.group({
    fecha: [this.hoyIso(), Validators.required],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    deCaja: [true],
    bancoId: [null as number | null],
    bancoDebitoId: [null as number | null],
    bancoCreditoId: [null as number | null],
    tipoPago: ['transferencia'],
  });

  get paginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamano));
  }

  get montoBsVivo(): number {
    const c = Number(this.form.controls.cantidad.value) || 0;
    const t = Number(this.form.controls.tasa.value) || 0;
    return Math.round(c * t * 100) / 100;
  }

  get tituloCuota(): string {
    return this.esCompra ? 'Registrar recepción' : 'Registrar entrega';
  }

  get tituloPago(): string {
    return this.esCompra ? 'Registrar pago' : 'Registrar cobro';
  }

  ngOnInit(): void {
    this.cargar();
    forkJoin({
      tasa: this.tasaService.actual(),
      bancos: this.bancoService.listar(),
    }).subscribe({
      next: ({ tasa, bancos }) => {
        this.tasaActiva = Number(tasa?.tasaVenta ?? tasa?.tasaActiva ?? 0);
        if (!this.form.controls.tasa.value) this.form.controls.tasa.setValue(this.tasaActiva);
        this.bancos = Array.isArray(bancos) ? bancos : [];
      },
      error: () => undefined,
    });
  }

  cargar(): void {
    this.cargando = true;
    this.service
      .listar(this.tipo, {
        pagina: this.pagina,
        tamano: this.tamano,
        clienteId: this.filtroCliente?.id,
        desde: this.desde,
        hasta: this.hasta,
        estado: this.estado,
      })
      .subscribe({
        next: (page) => {
          this.items = page?.items ?? [];
          this.total = page?.total ?? this.items.length;
          this.cargando = false;
        },
        error: () => {
          this.items = [];
          this.cargando = false;
        },
      });
  }

  limpiar(): void {
    this.desde = '';
    this.hasta = '';
    this.estado = '';
    this.filtroCliente = null;
    this.pagina = 1;
    this.cargar();
  }

  irPagina(p: number): void {
    this.pagina = Math.min(Math.max(1, p), this.paginas);
    this.cargar();
  }

  montoDe(op: Operacion): number {
    return Number(op?.montoBs ?? (Number(op?.cantidad) || 0) * (Number(op?.tasa) || 0)) || 0;
  }

  // ---------- Nueva ----------
  nueva(): void {
    this.form.reset({ fecha: this.hoyIso(), tasa: this.tasaActiva, cantidad: 0, observaciones: '' });
    this.formSelCliente = null;
    this.clienteFallo = false;
    this.tasaTocada = false;
    this.modalNueva = true;
  }

  onCliente(c: Cliente | null): void {
    this.formSelCliente = c;
    this.clienteFallo = !c;
  }

  aplicarTasaActiva(): void {
    this.form.controls.tasa.setValue(this.tasaActiva);
    this.tasaTocada = true;
  }

  fallo(campo: string): boolean {
    const control = this.form.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  guardar(): void {
    this.clienteFallo = !this.formSelCliente;
    if (this.form.invalid || this.clienteFallo) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.guardando = true;
    this.service
      .crear(this.tipo, {
        clienteId: this.formSelCliente!.id,
        fecha: v.fecha,
        tasa: Number(v.tasa),
        cantidad: Number(v.cantidad),
        observaciones: v.observaciones,
      })
      .subscribe({
        next: () => {
          this.guardando = false;
          this.modalNueva = false;
          this.toast.success(`${this.esCompra ? 'Compra' : 'Venta'} registrada.`);
          this.cargar();
        },
        error: () => (this.guardando = false),
      });
  }

  // ---------- Detalle ----------
  verDetalle(op: Operacion): void {
    this.detalle = op as OperacionDetalle;
    this.nombreCliente = op.cliente ?? '';
    this.modalDetalle = true;
    this.cargandoDetalle = true;
    this.cuotas = [];
    this.service.obtener(this.tipo, op.id).subscribe({
      next: (det) => {
        this.detalle = det;
        this.cuotas = this.service.cuotasDe(det);
        this.cargandoDetalle = false;
      },
      error: () => (this.cargandoDetalle = false),
    });
  }

  esPago(c: Cuota): boolean {
    const t = String(c.tipo ?? '').toLowerCase();
    return t.includes('pago') || t.includes('cobro') || t.includes('entrega');
  }

  etiquetaCuota(c: Cuota): string {
    const t = String(c.tipo ?? '').toLowerCase();
    if (t.includes('pago')) return this.esCompra ? 'Pago' : 'Cobro';
    if (t.includes('entrega')) return 'Entrega';
    if (t.includes('recepcion')) return this.esCompra ? 'Recepción' : 'Entrega';
    return c.tipo || 'Cuota';
  }

  abrirCuota(tipo: 'recepcion' | 'pago'): void {
    this.tipoCuota = tipo;
    this.formCuota.reset({
      fecha: this.hoyIso(),
      monto: 0,
      deCaja: true,
      bancoId: null,
      bancoDebitoId: null,
      bancoCreditoId: null,
      tipoPago: 'transferencia',
    });
    this.modalCuota = true;
  }

  falloCuota(campo: string): boolean {
    const control = this.formCuota.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  guardarCuota(): void {
    if (this.formCuota.invalid || !this.detalle) {
      this.formCuota.markAllAsTouched();
      return;
    }
    const v = this.formCuota.getRawValue();
    const esRecepcion = this.tipoCuota === 'recepcion';
    this.guardandoCuota = true;

    const req = esRecepcion
      ? this.service.recepcion(this.tipo, this.detalle.id, {
            fecha: v.fecha,
            cantidad: Number(v.monto),
            deCaja: !!v.deCaja,
            bancoId: v.bancoId,
          })
      : this.service.pago(this.tipo, this.detalle.id, {
            fecha: v.fecha,
            monto: Number(v.monto),
            bancoDebitoId: v.bancoDebitoId,
            bancoCreditoId: v.bancoCreditoId,
            tipoPago: v.tipoPago,
          });

    req.subscribe({
      next: () => {
        this.guardandoCuota = false;
        this.modalCuota = false;
        this.toast.success(esRecepcion ? 'Movimiento registrado.' : 'Pago registrado.');
        this.verDetalle(this.detalle!);
        this.cargar();
      },
      error: () => (this.guardandoCuota = false),
    });
  }

  anular(op: Operacion): void {
    if (op.estado === 'anulada') return;
    const motivo = prompt('Motivo de anulación:');
    if (!motivo || !motivo.trim()) return;
    this.service.anular(this.tipo, op.id, motivo.trim()).subscribe({
      next: () => {
        this.toast.success('Operación anulada.');
        this.modalDetalle = false;
        this.cargar();
      },
      error: () => undefined,
    });
  }

  // ---------- Comprobante ----------
  abrirComprobante(op: Operacion): void {
    this.comprobanteId = op.id;
    this.modalComprobante = true;
  }

  private hoyIso(): string {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
