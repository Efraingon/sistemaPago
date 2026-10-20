import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ClienteService } from '../../core/services/cliente.service';
import { BancoService } from '../../core/services/banco.service';
import { MonedaService } from '../../core/services/tasas.service';
import { Cliente, CuentaCliente, TipoPersona } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { MontoPipe } from '../../shared/pipes/pipes';
import { ToastService } from '../../shared/toast/toast.service';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, ModalComponent, IconComponent, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Directorio</p>
        <h1 class="page-title">Clientes</h1>
        <p class="page-sub">{{ total }} cliente(s) registrado(s)</p>
      </div>
      <button class="btn btn-primary" type="button" (click)="nuevo()">
        <app-icon name="plus" [size]="16" /> Nuevo cliente
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
      <div class="form-group grow">
        <label class="form-label" for="buscar">Buscar</label>
        <input
          id="buscar"
          class="input"
          type="search"
          placeholder="Nombre, RIF, cédula o email…"
          [(ngModel)]="buscar"
          (ngModelChange)="alBuscar($event)"
          autocomplete="off"
        />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="estado">Estado</label>
        <select id="estado" class="select" [(ngModel)]="filtroActivo" (ngModelChange)="cargar()">
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </div>
      <button class="btn btn-ghost" type="button" (click)="limpiar()">
        <app-icon name="refresh" [size]="15" /> Limpiar
      </button>
    </div>

    <!-- Tabla -->
    <div class="table-wrap print-area">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando clientes…</div>
      } @else if (!filtrados.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="users" [size]="36" /></span>
          <strong>No hay clientes</strong>
          <span class="small">Ajuste la búsqueda o registre un cliente nuevo.</span>
          <button class="btn btn-secondary btn-sm mt-1" type="button" (click)="nuevo()">Registrar cliente</button>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>RIF / Cédula</th>
              <th>Nombre / Razón social</th>
              <th>Email</th>
              <th>Teléfonos</th>
              <th>Estado</th>
              <th class="text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (c of filtrados; track c.id) {
              <tr>
                <td class="mono">{{ documento(c) }}</td>
                <td>
                  <button class="link-name" type="button" (click)="verDetalle(c)" title="Ver detalle">
                    {{ nombre(c) }}
                  </button>
                  <span class="badge badge-muted" style="margin-inline-start: 8px">{{ c.persona === 'J' ? 'Jurídica' : 'Natural' }}</span>
                </td>
                <td>{{ c.email || '—' }}</td>
                <td class="mono small">{{ c.telefonos || '—' }}</td>
                <td>
                  <span class="badge" [class.badge-success]="c.activo" [class.badge-danger]="!c.activo">
                    {{ c.activo ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
                <td class="text-right" style="white-space: nowrap">
                  <button class="icon-btn" type="button" (click)="verDetalle(c)" title="Detalle" aria-label="Ver detalle">
                    <app-icon name="eye" [size]="15" />
                  </button>
                  <button class="icon-btn" type="button" (click)="editar(c)" title="Editar" aria-label="Editar cliente">
                    <app-icon name="edit" [size]="15" />
                  </button>
                  <button class="icon-btn danger" type="button" (click)="eliminar(c)" title="Eliminar" aria-label="Eliminar cliente">
                    <app-icon name="trash" [size]="15" />
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>

    <!-- Paginación -->
    @if (total > tamano) {
      <div class="pagination">
        <span>
          Página <strong class="mono">{{ pagina }}</strong> de
          <strong class="mono">{{ paginas }}</strong> · {{ total }} resultados
        </span>
        <div class="pages">
          <button class="icon-btn" type="button" [disabled]="pagina <= 1" (click)="irPagina(pagina - 1)" aria-label="Página anterior">
            <app-icon name="chevronLeft" [size]="15" />
          </button>
          <button class="icon-btn" type="button" [disabled]="pagina >= paginas" (click)="irPagina(pagina + 1)" aria-label="Página siguiente">
            <app-icon name="chevronRight" [size]="15" />
          </button>
        </div>
      </div>
    }

    <!-- Modal alta/edición -->
    <app-modal [visible]="modalForm" [title]="editando ? 'Editar cliente' : 'Nuevo cliente'" ancho="lg" (closed)="modalForm = false">
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-group mb-3">
          <span class="form-label">Tipo de persona <span class="req">*</span></span>
          <div class="row gap-4 mt-1">
            <label class="check">
              <input type="radio" formControlName="persona" value="N" (change)="cambiarPersona()" />
              Natural
            </label>
            <label class="check">
              <input type="radio" formControlName="persona" value="J" (change)="cambiarPersona()" />
              Jurídica
            </label>
          </div>
        </div>

        @if (esJuridica) {
          <div class="form-row mb-3">
            <div class="form-group">
              <label class="form-label" for="razonSocial">Razón social <span class="req">*</span></label>
              <input id="razonSocial" class="input" formControlName="razonSocial" [class.is-invalid]="fallo('razonSocial')" placeholder="Empresa, C.A." />
              @if (fallo('razonSocial')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> Razón social requerida</span>
              }
            </div>
            <div class="form-group">
              <label class="form-label" for="rif">RIF <span class="req">*</span></label>
              <input id="rif" class="input mono" formControlName="rif" [class.is-invalid]="fallo('rif')" placeholder="J-12345678-9" />
              @if (fallo('rif')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> RIF requerido</span>
              }
            </div>
          </div>
        } @else {
          <div class="form-row mb-3">
            <div class="form-group">
              <label class="form-label" for="nombres">Nombres <span class="req">*</span></label>
              <input id="nombres" class="input" formControlName="nombres" [class.is-invalid]="fallo('nombres')" placeholder="Juan" />
              @if (fallo('nombres')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> Requerido</span>
              }
            </div>
            <div class="form-group">
              <label class="form-label" for="apellidos">Apellidos <span class="req">*</span></label>
              <input id="apellidos" class="input" formControlName="apellidos" [class.is-invalid]="fallo('apellidos')" placeholder="Pérez" />
              @if (fallo('apellidos')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> Requerido</span>
              }
            </div>
            <div class="form-group">
              <label class="form-label" for="cedula">Cédula <span class="req">*</span></label>
              <input id="cedula" class="input mono" formControlName="cedula" [class.is-invalid]="fallo('cedula')" placeholder="V-12345678" />
              @if (fallo('cedula')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> Cédula requerida</span>
              }
            </div>
          </div>
        }

        <div class="form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" [class.is-invalid]="fallo('email')" placeholder="correo@ejemplo.com" />
            @if (fallo('email')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Email inválido</span>
            }
          </div>
          <div class="form-group">
            <label class="form-label" for="telefonos">Teléfonos</label>
            <input id="telefonos" class="input mono" formControlName="telefonos" placeholder="0414-0000000" />
          </div>
        </div>

        <div class="form-group mb-3">
          <label class="form-label" for="direccion">Dirección</label>
          <input id="direccion" class="input" formControlName="direccion" placeholder="Dirección física" />
        </div>

        <div class="form-group mb-3">
          <label class="form-label" for="observaciones">Observaciones</label>
          <textarea id="observaciones" class="textarea" formControlName="observaciones" rows="2" placeholder="Notas internas"></textarea>
        </div>

        <label class="check mb-4">
          <input type="checkbox" formControlName="activo" />
          Cliente activo
        </label>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalForm = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Guardar cliente
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- Modal detalle -->
    <app-modal [visible]="modalDetalle" [title]="detalle ? nombre(detalle) : 'Detalle'" ancho="lg" (closed)="modalDetalle = false">
      @if (detalle) {
        <div class="detalle-grid mb-4">
          <div class="detalle-item">
            <div class="dt">Documento</div>
            <div class="dd mono">{{ documento(detalle) }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Tipo</div>
            <div class="dd">{{ detalle.persona === 'J' ? 'Jurídica' : 'Natural' }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Email</div>
            <div class="dd">{{ detalle.email || '—' }}</div>
          </div>
          <div class="detalle-item">
            <div class="dt">Teléfonos</div>
            <div class="dd mono">{{ detalle.telefonos || '—' }}</div>
          </div>
          <div class="detalle-item" style="grid-column: 1 / -1">
            <div class="dt">Dirección</div>
            <div class="dd">{{ detalle.direccion || '—' }}</div>
          </div>
          @if (detalle.observaciones) {
            <div class="detalle-item" style="grid-column: 1 / -1">
              <div class="dt">Observaciones</div>
              <div class="dd">{{ detalle.observaciones }}</div>
            </div>
          }
        </div>

        <!-- Cuentas bancarias -->
        <div class="row-between mb-2">
          <h4 class="subtitulo">Cuentas bancarias</h4>
          <button class="btn btn-secondary btn-sm" type="button" (click)="modalCuenta = true">
            <app-icon name="plus" [size]="14" /> Agregar cuenta
          </button>
        </div>

        @if (cargandoCuentas) {
          <div class="loading-panel" style="padding: 20px"><span class="spinner"></span></div>
        } @else if (!cuentas.length) {
          <div class="empty-state" style="padding: 24px">
            <span class="empty-ico"><app-icon name="landmark" [size]="28" /></span>
            <span class="small">Este cliente no tiene cuentas vinculadas.</span>
          </div>
        } @else {
          <div class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>Banco</th>
                  <th>Número</th>
                  <th>Moneda</th>
                  <th class="num">Saldo</th>
                  <th class="text-right">Acción</th>
                </tr>
              </thead>
              <tbody>
                @for (cta of cuentas; track cta.id) {
                  <tr>
                    <td>{{ cta.banco || bancoNombre(cta.bancoId) }}</td>
                    <td class="mono">{{ cta.numero }}</td>
                    <td>{{ cta.moneda || '—' }}</td>
                    <td class="num">{{ (cta.saldo ?? 0) | monto }}</td>
                    <td class="text-right">
                      <button class="icon-btn danger" type="button" (click)="eliminarCuenta(cta)" aria-label="Eliminar cuenta">
                        <app-icon name="trash" [size]="14" />
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }

        <div class="row gap-2 mt-4" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalDetalle = false">Cerrar</button>
          <button class="btn btn-secondary" type="button" (click)="editar(detalle)">
            <app-icon name="edit" [size]="15" /> Editar
          </button>
        </div>
      }
    </app-modal>

    <!-- Modal nueva cuenta -->
    <app-modal [visible]="modalCuenta" title="Nueva cuenta bancaria" (closed)="modalCuenta = false">
      <form [formGroup]="formCuenta" (ngSubmit)="guardarCuenta()" novalidate>
        <div class="form-group mb-3">
          <label class="form-label" for="bancoId">Banco <span class="req">*</span></label>
          <select id="bancoId" class="select" formControlName="bancoId" [class.is-invalid]="falloCuenta('bancoId')">
            <option [ngValue]="null">Seleccione…</option>
            @for (b of bancos; track b.id) {
              <option [ngValue]="b.id">{{ b.nombre }}</option>
            }
          </select>
          @if (falloCuenta('bancoId')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Banco requerido</span>
          }
        </div>
        <div class="form-row-2 form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="numero">Número de cuenta <span class="req">*</span></label>
            <input id="numero" class="input mono" formControlName="numero" [class.is-invalid]="falloCuenta('numero')" placeholder="0134-0000-0000-0000" />
            @if (falloCuenta('numero')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Requerido</span>
            }
          </div>
          <div class="form-group">
            <label class="form-label" for="monedaId">Moneda</label>
            <select id="monedaId" class="select" formControlName="monedaId">
              <option [ngValue]="null">—</option>
              @for (m of monedas; track m.id) {
                <option [ngValue]="m.id">{{ m.descripcion }}</option>
              }
            </select>
          </div>
        </div>
        <div class="form-group mb-4">
          <label class="form-label" for="descripcion">Descripción</label>
          <input id="descripcion" class="input" formControlName="descripcion" placeholder="Cuenta corriente / ahorro" />
        </div>
        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalCuenta = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardandoCuenta">Agregar</button>
        </div>
      </form>
    </app-modal>
  `,
  styles: [
    `
      .link-name {
        background: none;
        border: 0;
        padding: 0;
        color: var(--color-foreground);
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        text-align: start;
        transition: color 200ms ease;
      }
      .link-name:hover {
        color: var(--color-primary);
        text-decoration: underline;
        text-underline-offset: 3px;
      }
      .subtitulo {
        font-size: 14px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--color-muted-foreground);
      }
      .icon-btn + .icon-btn {
        margin-inline-start: 6px;
      }
    `,
  ],
})
export class ClientesComponent implements OnInit {
  // Lista
  items: Cliente[] = [];
  total = 0;
  pagina = 1;
  tamano = 10;
  buscar = '';
  filtroActivo = '';
  cargando = false;
  private timer: ReturnType<typeof setTimeout> | null = null;

  // Form
  modalForm = false;
  editando: Cliente | null = null;
  guardando = false;

  // Detalle
  modalDetalle = false;
  detalle: Cliente | null = null;
  cuentas: CuentaCliente[] = [];
  cargandoCuentas = false;

  // Cuentas
  modalCuenta = false;
  guardandoCuenta = false;
  bancos: { id: string; nombre: string }[] = [];
  monedas: { id: string; descripcion: string }[] = [];

  private readonly service = inject(ClienteService);
  private readonly bancoService = inject(BancoService);
  private readonly monedaService = inject(MonedaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    persona: ['N' as TipoPersona, Validators.required],
    razonSocial: [''],
    rif: [''],
    nombres: [''],
    apellidos: [''],
    cedula: [''],
    email: ['', Validators.email],
    telefonos: [''],
    direccion: [''],
    observaciones: [''],
    activo: [true],
  });

  readonly formCuenta = this.fb.nonNullable.group({
    bancoId: [null as string | null, Validators.required],
    numero: ['', Validators.required],
    monedaId: [null as string | null],
    descripcion: [''],
  });

  get filtrados(): Cliente[] {
    if (!this.filtroActivo) return this.items;
    const activo = this.filtroActivo === 'true';
    return this.items.filter((c) => !!c.activo === activo);
  }

  get paginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamano));
  }

  get esJuridica(): boolean {
    return this.form.controls.persona.value === 'J';
  }

  ngOnInit(): void {
    this.cargar();
    this.bancoService.listar().subscribe({
      next: (b) => (this.bancos = Array.isArray(b) ? b : []),
      error: () => (this.bancos = []),
    });
    this.cargarMonedas();
  }

  cargar(): void {
    this.cargando = true;
    this.service
      .listar({ pagina: this.pagina, tamano: this.tamano, buscar: this.buscar })
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

  alBuscar(valor: string): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      this.pagina = 1;
      this.cargar();
    }, 300);
    this.buscar = valor;
  }

  limpiar(): void {
    this.buscar = '';
    this.filtroActivo = '';
    this.pagina = 1;
    this.cargar();
  }

  irPagina(p: number): void {
    this.pagina = Math.min(Math.max(1, p), this.paginas);
    this.cargar();
  }

  nombre(c: Cliente): string {
    return (
      (c.nombre ?? '').trim() ||
      (c.razonSocial ?? '').trim() ||
      `${c.nombres ?? ''} ${c.apellidos ?? ''}`.trim() ||
      `Cliente #${c.id}`
    );
  }

  documento(c: Cliente): string {
    return c.rif || c.cedula || '—';
  }

  // ---------- Alta / edición ----------
  nuevo(): void {
    this.editando = null;
    this.form.reset({ persona: 'N', razonSocial: '', rif: '', nombres: '', apellidos: '', cedula: '', email: '', telefonos: '', direccion: '', observaciones: '', activo: true });
    this.modalForm = true;
  }

  editar(c: Cliente | null): void {
    if (!c) return;
    this.editando = c;
    this.form.reset({
      persona: (c.persona === 'J' ? 'J' : 'N') as TipoPersona,
      razonSocial: c.razonSocial ?? '',
      rif: c.rif ?? '',
      nombres: c.nombres ?? '',
      apellidos: c.apellidos ?? '',
      cedula: c.cedula ?? '',
      email: c.email ?? '',
      telefonos: c.telefonos ?? '',
      direccion: c.direccion ?? '',
      observaciones: c.observaciones ?? '',
      activo: !!c.activo,
    });
    this.modalDetalle = false;
    this.modalForm = true;
  }

  cambiarPersona(): void {
    // Limpia validaciones cruzadas al cambiar de tipo
    this.form.controls.razonSocial.clearValidators();
    this.form.controls.razonSocial.updateValueAndValidity();
  }

  fallo(campo: string): boolean {
    const control = this.form.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  guardar(): void {
    const v = this.form.getRawValue();
    // Validación condicional según tipo de persona
    let valido = true;
    if (v.persona === 'J') {
      valido = v.razonSocial.trim().length > 0 && v.rif.trim().length > 0;
      if (!v.razonSocial.trim()) this.form.controls.razonSocial.setErrors({ requerido: true });
      if (!v.rif.trim()) this.form.controls.rif.setErrors({ requerido: true });
    } else {
      valido = v.nombres.trim().length > 0 && v.apellidos.trim().length > 0 && v.cedula.trim().length > 0;
      if (!v.nombres.trim()) this.form.controls.nombres.setErrors({ requerido: true });
      if (!v.apellidos.trim()) this.form.controls.apellidos.setErrors({ requerido: true });
      if (!v.cedula.trim()) this.form.controls.cedula.setErrors({ requerido: true });
    }
    if (this.form.invalid || !valido) {
      this.form.markAllAsTouched();
      return;
    }

    const body: Partial<Cliente> = {
      persona: v.persona,
      razonSocial: v.persona === 'J' ? v.razonSocial : null as any,
      nombres: v.persona === 'N' ? v.nombres : null as any,
      apellidos: v.persona === 'N' ? v.apellidos : null as any,
      rif: v.persona === 'J' ? v.rif : '',
      cedula: v.persona === 'N' ? v.cedula : '',
      email: v.email,
      telefonos: v.telefonos,
      direccion: v.direccion,
      observaciones: v.observaciones,
      activo: v.activo,
    };

    this.guardando = true;
    const req = this.editando
      ? this.service.actualizar(this.editando.id, body)
      : this.service.crear(body);

    req.subscribe({
      next: () => {
        this.guardando = false;
        this.modalForm = false;
        this.toast.success(this.editando ? 'Cliente actualizado.' : 'Cliente creado.');
        this.editando = null;
        this.cargar();
      },
      error: () => (this.guardando = false),
    });
  }

  eliminar(c: Cliente): void {
    if (!confirm(`¿Eliminar el cliente "${this.nombre(c)}"? Esta acción no se puede deshacer.`)) return;
    this.service.eliminar(c.id).subscribe({
      next: () => {
        this.toast.success('Cliente eliminado.');
        this.cargar();
      },
      error: () => undefined,
    });
  }

  // ---------- Detalle + cuentas ----------
  verDetalle(c: Cliente): void {
    this.detalle = c;
    this.modalDetalle = true;
    this.cargandoCuentas = true;
    this.service.cuentas(c.id).subscribe({
      next: (lista) => {
        this.cuentas = Array.isArray(lista) ? lista : [];
        this.cargandoCuentas = false;
      },
      error: () => {
        this.cuentas = [];
        this.cargandoCuentas = false;
      },
    });
  }

  bancoNombre(id?: string): string {
    return this.bancos.find((b) => b.id === id)?.nombre ?? '—';
  }

  /** Imprime el listado de clientes (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta el listado de clientes a CSV. */
  exportar(): void {
    const encabezados = ['RIF / Cédula', 'Nombre / Razón social', 'Tipo', 'Email', 'Teléfonos', 'Estado'];
    const filas = this.filtrados.map((c) => [
      c.rif || c.cedula || '—',
      this.nombre(c),
      c.persona === 'J' ? 'Jurídica' : 'Natural',
      c.email || '',
      c.telefonos || '',
      c.activo ? 'Activo' : 'Inactivo',
    ]);
    exportarCsv('clientes_listado', encabezados, filas);
  }

  falloCuenta(campo: string): boolean {
    const control = this.formCuenta.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  guardarCuenta(): void {
    if (this.formCuenta.invalid || !this.detalle) {
      this.formCuenta.markAllAsTouched();
      return;
    }
    const v = this.formCuenta.getRawValue();
    this.guardandoCuenta = true;
    this.service
      .crearCuenta(this.detalle.id, {
        bancoId: v.bancoId ?? '',
        numero: v.numero,
        monedaId: v.monedaId ?? undefined,
        descripcion: v.descripcion,
      })
      .subscribe({
        next: () => {
          this.guardandoCuenta = false;
          this.modalCuenta = false;
          this.toast.success('Cuenta agregada.');
          this.formCuenta.reset({ bancoId: null, numero: '', monedaId: null, descripcion: '' });
          if (this.detalle) this.verDetalle(this.detalle);
        },
        error: () => (this.guardandoCuenta = false),
      });
  }

  eliminarCuenta(cta: CuentaCliente): void {
    if (!this.detalle || !confirm(`¿Eliminar la cuenta ${cta.numero}?`)) return;
    this.service.eliminarCuenta(this.detalle.id, cta.id).subscribe({
      next: () => {
        this.toast.success('Cuenta eliminada.');
        this.verDetalle(this.detalle!);
      },
      error: () => undefined,
    });
  }

  private cargarMonedas(): void {
    this.monedaService.listar().subscribe({
      next: (m) => (this.monedas = Array.isArray(m) ? m : []),
      error: () => (this.monedas = []),
    });
  }
}
