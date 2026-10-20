import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BancoService } from '../../core/services/banco.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Banco, CuentaBanco } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { MontoPipe } from '../../shared/pipes/pipes';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-bancos',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, IconComponent, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Tesorería</p>
        <h1 class="page-title">Bancos</h1>
        <p class="page-sub">Entidades nacionales e internacionales y sus cuentas.</p>
      </div>
      <button class="btn btn-primary" type="button" (click)="nuevo()">
        <app-icon name="plus" [size]="16" /> Nuevo banco
      </button>
      <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
        <app-icon name="arrowDown" [size]="14" /> CSV
      </button>
      <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
        <app-icon name="print" [size]="14" /> Imprimir
      </button>
    </div>

    <div class="layout">
      <!-- Lista de bancos -->
      <section class="lista">
        @if (cargando) {
          <div class="card"><div class="loading-panel"><span class="spinner"></span> Cargando bancos…</div></div>
        } @else if (!bancos.length) {
          <div class="card">
            <div class="empty-state">
              <span class="empty-ico"><app-icon name="landmark" [size]="34" /></span>
              <strong>Sin bancos registrados</strong>
              <span class="small">Agregue la primera entidad financiera.</span>
            </div>
          </div>
        } @else {
          @for (b of bancos; track b.id) {
            <button
              class="banco-card"
              type="button"
              [class.sel]="seleccionado?.id === b.id"
              (click)="seleccionar(b)"
            >
              <span class="bc-ico"><app-icon name="landmark" [size]="18" /></span>
              <span class="bc-meta">
                <strong>{{ b.nombre }}</strong>
                <small>
                  <span class="badge" [class.badge-gold]="esNacional(b)" [class.badge-accent]="!esNacional(b)">
                    {{ esNacional(b) ? 'Nacional' : 'Internacional' }}
                  </span>
                  @if (b.cuentas?.length) {
                    <span class="badge badge-muted" style="margin-inline-start: 6px">{{ b.cuentas.length }} cuenta(s)</span>
                  }
                </small>
              </span>
              <span class="bc-saldo mono">{{ (b.saldo ?? 0) | monto }}</span>
              <span class="bc-acc" (click)="$event.stopPropagation(); editar(b)" role="button" aria-label="Editar banco">
                <app-icon name="edit" [size]="15" />
              </span>
            </button>
          }
        }
      </section>

      <!-- Cuentas del banco -->
      <section class="cuentas card">
        @if (!seleccionado) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="banknote" [size]="36" /></span>
            <strong>Seleccione un banco</strong>
            <span class="small">Verá aquí sus cuentas y podrás agregar nuevas.</span>
          </div>
        } @else {
          <div class="row-between mb-3">
            <div>
              <h2 class="card-title" style="margin: 0">Cuentas — {{ seleccionado.nombre }}</h2>
              <span class="small text-muted">{{ cuentas.length }} cuenta(s)</span>
            </div>
            <button class="btn btn-secondary btn-sm" type="button" (click)="modalCuenta = true">
              <app-icon name="plus" [size]="14" /> Nueva cuenta
            </button>
          </div>

          @if (cargandoCuentas) {
            <div class="loading-panel"><span class="spinner"></span> Cargando cuentas…</div>
          } @else if (!cuentas.length) {
            <div class="empty-state" style="padding: 32px">
              <span class="empty-ico"><app-icon name="inbox" [size]="30" /></span>
              <strong>Este banco no tiene cuentas</strong>
            </div>
          } @else {
            <div class="table-wrap print-area">
              <table class="table">
                <thead>
                  <tr>
                    <th>Número</th>
                    <th>Cuenta contable</th>
                    <th class="num">Saldo</th>
                    <th class="num">Débitos</th>
                    <th class="num">Créditos</th>
                    <th>Estatus</th>
                    <th class="text-right">Acción</th>
                  </tr>
                </thead>
                <tbody>
                  @for (c of cuentas; track c.id) {
                    <tr>
                      <td class="mono">{{ c.numeroCuenta }}</td>
                      <td class="mono">{{ c.cuentaContable || '—' }}</td>
                      <td class="num">{{ (c.saldo ?? 0) | monto }}</td>
                      <td class="num text-muted">{{ (c.debitos ?? 0) | monto }}</td>
                      <td class="num text-muted">{{ (c.creditos ?? 0) | monto }}</td>
                      <td>
                        <span class="badge" [class.badge-success]="c.activa !== false" [class.badge-muted]="c.activa === false">
                          {{ c.activa !== false ? 'Activa' : 'Inactiva' }}
                        </span>
                      </td>
                      <td class="text-right">
                        <button class="icon-btn danger" type="button" (click)="eliminarCuenta(c)" aria-label="Eliminar cuenta">
                          <app-icon name="trash" [size]="14" />
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }
      </section>
    </div>

    <!-- Modal banco -->
    <app-modal [visible]="modalBanco" [title]="editando ? 'Editar banco' : 'Nuevo banco'" (closed)="modalBanco = false">
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-group mb-3">
          <label class="form-label" for="nombre">Nombre <span class="req">*</span></label>
          <input id="nombre" class="input" formControlName="nombre" [class.is-invalid]="fallo('nombre')" placeholder="Banco Nacional" />
          @if (fallo('nombre')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Nombre requerido</span>
          }
        </div>
        <label class="check mb-4">
          <input type="checkbox" formControlName="nacional" />
          Entidad nacional
        </label>
        <label class="check mb-4">
          <input type="checkbox" formControlName="activo" />
          Banco activo
        </label>
        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalBanco = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">Guardar</button>
        </div>
      </form>
    </app-modal>

    <!-- Modal cuenta -->
    <app-modal [visible]="modalCuenta" title="Nueva cuenta" (closed)="modalCuenta = false">
      <form [formGroup]="formCuenta" (ngSubmit)="guardarCuenta()" novalidate>
        <div class="form-group mb-3">
          <label class="form-label" for="numero">Número <span class="req">*</span></label>
          <input id="numero" class="input mono" formControlName="numeroCuenta" [class.is-invalid]="falloCuenta('numeroCuenta')" placeholder="0134-0000-0000-0000" />
          @if (falloCuenta('numeroCuenta')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Requerido</span>
          }
        </div>
        <div class="form-group mb-4">
          <label class="form-label" for="ctacte">Cuenta contable</label>
          <input id="ctacte" class="input mono" formControlName="cuentaContable" placeholder="1110-01 (opcional)" />
        </div>
        <label class="check mb-4">
          <input type="checkbox" formControlName="activa" />
          Cuenta activa
        </label>
        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalCuenta = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardandoCuenta">Agregar</button>
        </div>
      </form>
    </app-modal>
  `,
  styles: [
    `
      .layout {
        display: grid;
        grid-template-columns: minmax(260px, 340px) minmax(0, 1fr);
        gap: 20px;
        align-items: start;
      }
      .lista {
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .banco-card {
        display: flex;
        align-items: center;
        gap: 13px;
        width: 100%;
        text-align: start;
        background: var(--color-card);
        border: 1px solid var(--color-border);
        border-radius: 10px;
        padding: 13px 15px;
        cursor: pointer;
        color: var(--color-foreground);
        transition: border-color 200ms ease, background-color 200ms ease;
      }
      .banco-card:hover {
        border-color: rgba(245, 158, 11, 0.45);
      }
      .banco-card.sel {
        border-color: var(--color-primary);
        background: rgba(245, 158, 11, 0.08);
      }
      .bc-ico {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: 9px;
        background: var(--color-muted);
        color: var(--color-primary);
        flex: 0 0 auto;
      }
      .bc-meta {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        gap: 5px;
      }
      .bc-meta strong {
        font-size: 14px;
        font-weight: 600;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .bc-saldo {
        font-size: 13.5px;
        color: var(--color-muted-foreground);
        white-space: nowrap;
      }
      .bc-acc {
        display: inline-flex;
        color: var(--color-muted-foreground);
        padding: 5px;
        border-radius: 6px;
        transition: color 200ms ease, background 200ms ease;
        cursor: pointer;
      }
      .bc-acc:hover {
        color: var(--color-primary);
        background: var(--color-muted);
      }
      @media (max-width: 900px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class BancosComponent implements OnInit {
  bancos: Banco[] = [];
  seleccionado: Banco | null = null;
  cuentas: CuentaBanco[] = [];
  cargando = false;
  cargandoCuentas = false;

  modalBanco = false;
  editando: Banco | null = null;
  guardando = false;

  modalCuenta = false;
  guardandoCuenta = false;

  private readonly service = inject(BancoService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    nacional: [true],
    activo: [true],
  });

  readonly formCuenta = this.fb.nonNullable.group({
    numeroCuenta: ['', Validators.required],
    cuentaContable: [''],
    activa: [true],
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.service.listar().subscribe({
      next: (lista) => {
        this.bancos = Array.isArray(lista) ? lista : [];
        this.cargando = false;
        if (this.seleccionado) {
          const actualizado = this.bancos.find((b) => b.id === this.seleccionado!.id);
          if (actualizado) this.seleccionar(actualizado);
          else this.seleccionado = null;
        }
      },
      error: () => {
        this.bancos = [];
        this.cargando = false;
      },
    });
  }

  seleccionar(b: Banco): void {
    this.seleccionado = b;
    this.cargandoCuentas = true;
    this.service.cuentas(b.id).subscribe({
      next: (c) => {
        this.cuentas = Array.isArray(c) ? c : [];
        this.cargandoCuentas = false;
      },
      error: () => {
        this.cuentas = [];
        this.cargandoCuentas = false;
      },
    });
  }

  // ---------- Banco ----------
  nuevo(): void {
    this.editando = null;
    this.form.reset({ nombre: '', nacional: true, activo: true });
    this.modalBanco = true;
  }

  editar(b: Banco): void {
    this.editando = b;
    this.form.reset({ nombre: b.nombre, nacional: this.esNacional(b), activo: b.activo !== false });
    this.modalBanco = true;
  }

  /** VFP BANCOS.NACIONAL: 1 = nacional, 2 = internacional. */
  esNacional(b: Banco): boolean {
    return Number(b.nacional ?? 1) === 1;
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
    // el backend exige Nacional como 1 (nacional) | 2 (internacional)
    const nacional = v.nacional ? 1 : 2;
    this.guardando = true;
    const req = this.editando
      ? this.service.actualizar(this.editando.id, { nombre: v.nombre, nacional, activo: v.activo })
      : this.service.crear({ nombre: v.nombre, nacional, activo: v.activo });
    req.subscribe({
      next: () => {
        this.guardando = false;
        this.modalBanco = false;
        this.toast.success(this.editando ? 'Banco actualizado.' : 'Banco creado.');
        this.editando = null;
        this.cargar();
      },
      error: () => (this.guardando = false),
    });
  }

  // ---------- Cuentas ----------
  falloCuenta(campo: string): boolean {
    const c = this.formCuenta.get(campo);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  guardarCuenta(): void {
    if (this.formCuenta.invalid || !this.seleccionado) {
      this.formCuenta.markAllAsTouched();
      return;
    }
    const v = this.formCuenta.getRawValue();
    this.guardandoCuenta = true;
    this.service
      .crearCuenta(this.seleccionado.id, {
        numeroCuenta: v.numeroCuenta,
        cuentaContable: v.cuentaContable,
        activa: v.activa,
      })
      .subscribe({
        next: () => {
          this.guardandoCuenta = false;
          this.modalCuenta = false;
          this.toast.success('Cuenta creada.');
          this.formCuenta.reset({ numeroCuenta: '', cuentaContable: '', activa: true });
          this.seleccionar(this.seleccionado!);
        },
        error: () => (this.guardandoCuenta = false),
      });
  }

  eliminarCuenta(c: CuentaBanco): void {
    if (!this.seleccionado || !confirm(`¿Eliminar la cuenta ${c.numeroCuenta}?`)) return;
    this.service.eliminarCuenta(this.seleccionado.id, c.id).subscribe({
      next: () => {
        this.toast.success('Cuenta eliminada.');
        this.seleccionar(this.seleccionado!);
      },
      error: () => undefined,
    });
  }

  /** Imprime el listado de bancos / cuentas (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta los bancos y sus cuentas a CSV. */
  exportar(): void {
    const encabezados = ['Banco', 'Tipo', 'Saldo', 'Nº Cuenta', 'Cuenta contable', 'Saldo cuenta', 'Débitos', 'Créditos'];
    const filas: (string | number)[][] = [];
    for (const b of this.bancos) {
      filas.push([b.nombre, this.esNacional(b) ? 'Nacional' : 'Internacional', Number(b.saldo || 0), '', '', '', '', '']);
      for (const cta of (b.cuentas ?? [])) {
        filas.push(['', '', '', cta.numeroCuenta, cta.cuentaContable || '', Number(cta.saldo || 0), Number(cta.debitos || 0), Number(cta.creditos || 0)]);
      }
    }
    exportarCsv('bancos_cuentas', encabezados, filas);
  }
}
