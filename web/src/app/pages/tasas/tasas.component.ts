import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TasaService } from '../../core/services/tasas.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Tasa, TasaCalendarioItem } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';

@Component({
  selector: 'app-tasas',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, IconComponent, FechaPipe, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Mesa de dinero</p>
        <h1 class="page-title">Tasas</h1>
        <p class="page-sub">Tasa activa, compra y venta con vista previa de su efecto operativo.</p>
      </div>
    </div>

    <div class="grid-2">
      <!-- Formulario -->
      <section class="card">
        <h2 class="card-title">Tasas vigentes</h2>
        <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
          <div class="form-group mb-3">
            <label class="form-label" for="tasaActiva">Tasa activa (referencia) <span class="req">*</span></label>
            <input
              id="tasaActiva"
              class="input mono"
              type="number"
              step="0.01"
              min="0"
              formControlName="tasaActiva"
              [class.is-invalid]="fallo('tasaActiva')"
            />
            @if (fallo('tasaActiva')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Valor requerido</span>
            }
          </div>

          <div class="form-row-2 form-row mb-4">
            <div class="form-group">
              <label class="form-label" for="tasaCompra">Tasa de compra <span class="req">*</span></label>
              <input
                id="tasaCompra"
                class="input mono"
                type="number"
                step="0.01"
                min="0"
                formControlName="tasaCompra"
                [class.is-invalid]="fallo('tasaCompra')"
              />
              @if (fallo('tasaCompra')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> Requerida</span>
              }
            </div>
            <div class="form-group">
              <label class="form-label" for="tasaVenta">Tasa de venta <span class="req">*</span></label>
              <input
                id="tasaVenta"
                class="input mono"
                type="number"
                step="0.01"
                min="0"
                formControlName="tasaVenta"
                [class.is-invalid]="fallo('tasaVenta')"
              />
              @if (fallo('tasaVenta')) {
                <span class="form-error"><app-icon name="alert" [size]="12" /> {{ errorVenta() }}</span>
              }
            </div>
          </div>

          <!-- Preview de efecto -->
          <div class="preview">
            <span class="preview-titulo">Efecto por 1.000 USD</span>
            <div class="preview-grid">
              <div>
                <span class="dt">Compra (cliente vende)</span>
                <strong class="mono">{{ previewCompra | monto }} Bs</strong>
              </div>
              <div>
                <span class="dt">Venta (cliente compra)</span>
                <strong class="mono text-gold">{{ previewVenta | monto }} Bs</strong>
              </div>
              <div>
                <span class="dt">Margen mesa</span>
                <strong class="mono">{{ previewMargen | monto }} Bs</strong>
              </div>
            </div>
            @if (form.invalid && enviado) {
              <p class="form-error mt-2"><app-icon name="alert" [size]="13" /> Complete las tres tasas para guardar.</p>
            }
          </div>

          <div class="row gap-2 mt-4" style="justify-content: flex-end">
            <button class="btn btn-ghost" type="button" (click)="restaurar()">Restaurar</button>
            <button class="btn btn-primary" type="submit" [disabled]="guardando">
              @if (guardando) {
                <span class="spinner" style="width:16px;height:16px"></span> Guardando…
              } @else {
                Guardar tasas
              }
            </button>
          </div>
        </form>
      </section>

      <!-- Calendario -->
      <section class="card" style="padding: 0">
        <div class="row-between" style="padding: 18px 20px 8px">
          <h2 class="card-title" style="margin: 0">Calendario histórico</h2>
          <button class="btn btn-ghost btn-sm" type="button" (click)="cargarCalendario()">
            <app-icon name="refresh" [size]="14" /> Actualizar
          </button>
        </div>
        <div class="row gap-2 wrap" style="padding: 0 20px 12px">
          <div class="form-group">
            <label class="form-label" for="desde">Desde</label>
            <input id="desde" class="input" type="date" [(ngModel)]="desde" (ngModelChange)="cargarCalendario()" />
          </div>
          <div class="form-group">
            <label class="form-label" for="hasta">Hasta</label>
            <input id="hasta" class="input" type="date" [(ngModel)]="hasta" (ngModelChange)="cargarCalendario()" />
          </div>
        </div>

        @if (cargandoCal) {
          <div class="loading-panel"><span class="spinner"></span> Cargando calendario…</div>
        } @else if (!calendario.length) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="calendar" [size]="32" /></span>
            <strong>Sin tasas en el rango</strong>
            <span class="small">No hay registros históricos para estas fechas.</span>
          </div>
        } @else {
          <div class="table-wrap" style="border: 0; border-radius: 0">
            <table class="table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th class="num">Activa</th>
                  <th class="num">Compra</th>
                  <th class="num">Venta</th>
                </tr>
              </thead>
              <tbody>
                @for (t of calendario; track t.id ?? t.fecha) {
                  <tr>
                    <td class="mono">{{ t.fecha | fecha }}</td>
                    <td class="num">{{ t.tasa | monto }}</td>
                    <td class="num">{{ t.tasaCompra | monto }}</td>
                    <td class="num text-gold">{{ t.tasaVenta | monto }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    </div>
  `,
  styles: [
    `
      .preview {
        background: rgba(245, 158, 11, 0.06);
        border: 1px solid rgba(245, 158, 11, 0.3);
        border-radius: 10px;
        padding: 16px 18px;
      }
      .preview-titulo {
        display: block;
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        color: var(--color-muted-foreground);
        margin-bottom: 12px;
      }
      .preview-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 16px;
      }
      .dt {
        display: block;
        font-size: 11.5px;
        color: var(--color-muted-foreground);
        margin-bottom: 4px;
      }
      .preview-grid strong {
        font-size: 19px;
        font-weight: 600;
        color: var(--color-foreground);
        letter-spacing: -0.01em;
      }
      .form-group {
        flex: 0 0 auto;
        min-width: 150px;
      }
    `,
  ],
})
export class TasasComponent implements OnInit {
  cargando = false;
  guardando = false;
  enviado = false;
  original: Tasa | null = null;

  cargandoCal = false;
  calendario: TasaCalendarioItem[] = [];
  desde = '';
  hasta = '';

  private readonly service = inject(TasaService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group(
    {
      tasaActiva: [0, [Validators.required, Validators.min(0.01)]],
      tasaCompra: [0, [Validators.required, Validators.min(0.01)]],
      tasaVenta: [0, [Validators.required, Validators.min(0.01)]],
    },
    {
      // regla de negocio: la venta no puede ser menor que la compra (spread >= 0)
      validators: (g) => {
        const compra = Number(g.get('tasaCompra')?.value);
        const venta = Number(g.get('tasaVenta')?.value);
        return compra > 0 && venta > 0 && venta < compra ? { ventaMenorQueCompra: true } : null;
      },
    },
  );

  get previewCompra(): number {
    return (Number(this.form.controls.tasaCompra.value) || 0) * 1000;
  }

  get previewVenta(): number {
    return (Number(this.form.controls.tasaVenta.value) || 0) * 1000;
  }

  get previewMargen(): number {
    return this.previewVenta - this.previewCompra;
  }

  ngOnInit(): void {
    const hoy = new Date();
    const hace30 = new Date();
    hace30.setDate(hoy.getDate() - 30);
    this.hasta = this.iso(hoy);
    this.desde = this.iso(hace30);

    this.cargando = true;
    this.service.actual().subscribe({
      next: (t) => {
        this.cargando = false;
        if (t) {
          this.original = t;
          this.form.reset({
            tasaActiva: Number(t.tasaActiva) || 0,
            tasaCompra: Number(t.tasaCompra) || 0,
            tasaVenta: Number(t.tasaVenta) || 0,
          });
        }
      },
      error: () => (this.cargando = false),
    });

    this.cargarCalendario();
  }

  cargarCalendario(): void {
    this.cargandoCal = true;
    this.service.calendario(this.desde, this.hasta).subscribe({
      next: (lista) => {
        this.calendario = Array.isArray(lista) ? lista : ((lista as any)?.items ?? []);
        this.cargandoCal = false;
      },
      error: () => {
        this.calendario = [];
        this.cargandoCal = false;
      },
    });
  }

  fallo(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && (c.dirty || c.touched || this.enviado);
  }

  /** Mensaje específico para tasa de venta (regla: venta >= compra). */
  errorVenta(): string {
    const v = this.form.getRawValue();
    return Number(v.tasaVenta) < Number(v.tasaCompra) ? 'Debe ser ≥ la compra' : 'Requerida';
  }

  restaurar(): void {
    if (this.original) {
      this.form.reset({
        tasaActiva: Number(this.original.tasaActiva) || 0,
        tasaCompra: Number(this.original.tasaCompra) || 0,
        tasaVenta: Number(this.original.tasaVenta) || 0,
      });
    } else {
      this.form.reset({ tasaActiva: 0, tasaCompra: 0, tasaVenta: 0 });
    }
  }

  guardar(): void {
    this.enviado = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.guardando = true;
    this.service
      .guardar({
        tasaActiva: Number(v.tasaActiva),
        tasaCompra: Number(v.tasaCompra),
        tasaVenta: Number(v.tasaVenta),
      })
      .subscribe({
        next: () => {
          this.guardando = false;
          this.enviado = false;
          this.toast.success('Tasas actualizadas.');
          this.cargarCalendario();
        },
        error: () => (this.guardando = false),
      });
  }

  private iso(f: Date): string {
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
