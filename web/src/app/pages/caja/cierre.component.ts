import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CajaService } from '../../core/services/caja.service';
import { ReporteService } from '../../core/services/reporte.service';
import { ToastService } from '../../shared/toast/toast.service';
import { CierreEstado, CuadreCaja } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';

@Component({
  selector: 'app-cierre',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, FechaPipe, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Control diario</p>
        <h1 class="page-title">Cierre de caja</h1>
        <p class="page-sub">Estado del día, cuadre y cierre formal de la caja.</p>
      </div>
      <div class="row gap-2 wrap">
        <a class="btn btn-ghost" routerLink="/caja">
          <app-icon name="wallet" [size]="16" /> Movimientos
        </a>
        <a class="btn btn-ghost" routerLink="/reportes/cuadre">
          <app-icon name="chart" [size]="16" /> Reporte de cuadre
        </a>
      </div>
    </div>

    <div class="toolbar">
      <div class="form-group w-auto">
        <label class="form-label" for="fecha">Fecha de cierre</label>
        <input id="fecha" class="input" type="date" [(ngModel)]="fecha" (ngModelChange)="cargar()" />
      </div>
      <button class="btn btn-ghost" type="button" (click)="cargar()">
        <app-icon name="refresh" [size]="15" /> Actualizar
      </button>
    </div>

    @if (cargando) {
      <div class="card"><div class="loading-panel"><span class="spinner"></span> Consultando estado del cierre…</div></div>
    } @else {
      <!-- Estado -->
      <div class="grid-3 mb-4">
        <article class="kpi">
          <span class="kpi-label"><app-icon name="lock" [size]="14" /> Estado</span>
          <span class="kpi-value" style="font-size: 21px" [class.text-gold]="estado?.cerrado" [class.text-success]="!estado?.cerrado">
            {{ estado?.cerrado ? 'Día cerrado' : 'Día abierto' }}
          </span>
          <span class="kpi-sub">Corte {{ estado?.fecha || fecha | fecha }}</span>
        </article>
        <article class="kpi">
          <span class="kpi-label"><app-icon name="wallet" [size]="14" /> Total del día</span>
          <span class="kpi-value">{{ (estado?.total ?? 0) | monto }}</span>
          <span class="kpi-sub">Bs segun caja</span>
        </article>
        <article class="kpi">
          <span class="kpi-label"><app-icon name="banknote" [size]="14" /> Saldo cuadre</span>
          <span class="kpi-value">{{ (cuadre?.saldo ?? 0) | monto }}</span>
          <span class="kpi-sub">Según reporte de cuadre</span>
        </article>
      </div>

      <!-- Detalle cuadre -->
      <div class="grid-2 mb-4">
        <section class="card">
          <h2 class="card-title">Entradas y salidas</h2>
          <div class="linea">
            <span><app-icon name="arrowDown" [size]="15" /> Entradas</span>
            <strong class="mono text-success">{{ (cuadre?.entradas ?? 0) | monto }}</strong>
          </div>
          <div class="linea">
            <span><app-icon name="arrowUp" [size]="15" /> Salidas</span>
            <strong class="mono text-danger">{{ (cuadre?.salidas ?? 0) | monto }}</strong>
          </div>
          <div class="linea total">
            <span>Saldo</span>
            <strong class="mono">{{ (cuadre?.saldo ?? 0) | monto }} Bs</strong>
          </div>
        </section>

        <section class="card">
          <h2 class="card-title">Por moneda</h2>
          @if (cuadre?.porMoneda?.length) {
            @for (m of cuadre!.porMoneda; track m.moneda || m.descripcion) {
              <div class="linea">
                <span>{{ m.moneda || m.descripcion }}</span>
                <strong class="mono">{{ (m.monto ?? m.total ?? 0) | monto }}</strong>
              </div>
            }
          } @else {
            <div class="empty-state" style="padding: 24px">
              <span class="empty-ico"><app-icon name="coin" [size]="28" /></span>
              <span class="small">Sin desglose por moneda para esta fecha.</span>
            </div>
          }
        </section>
      </div>

      <!-- Acción de cierre -->
      <section class="card cierre-card">
        <div>
          <h2 class="cierre-titulo">
            @if (estado?.cerrado) {
              <app-icon name="check" [size]="18" />
            } @else {
              <app-icon name="lock" [size]="18" />
            }
            {{ estado?.cerrado ? 'La caja de este día está cerrada' : 'Cierre formal de caja' }}
          </h2>
          <p class="small text-muted mt-1">
            El cierre congela los movimientos del día y deja constancia del total. No se puede deshacer
            desde la aplicación.
          </p>
        </div>
        <button
          class="btn btn-primary"
          type="button"
          [disabled]="estado?.cerrado || cerrando"
          (click)="cerrar()"
        >
          @if (cerrando) {
            <span class="spinner" style="width:16px;height:16px"></span> Cerrando…
          } @else if (estado?.cerrado) {
            <app-icon name="check" [size]="16" /> Cerrado
          } @else {
            <app-icon name="lock" [size]="16" /> Cerrar caja del día
          }
        </button>
      </section>
    }
  `,
  styles: [
    `
      .linea {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 14px;
        padding: 11px 0;
        border-bottom: 1px dashed rgba(51, 65, 85, 0.7);
        font-size: 14px;
        color: var(--color-muted-foreground);
      }
      .linea:last-child {
        border-bottom: 0;
      }
      .linea span {
        display: inline-flex;
        align-items: center;
        gap: 8px;
      }
      .linea strong {
        font-size: 16px;
        color: var(--color-foreground);
      }
      .linea.total {
        border-top: 1px solid var(--hairline);
        border-bottom: 0;
        margin-top: 4px;
        padding-top: 13px;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        font-size: 12.5px;
      }
      .linea.total strong {
        font-size: 19px;
        color: var(--color-primary);
      }
      .cierre-card {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 22px;
        flex-wrap: wrap;
        border-color: rgba(245, 158, 11, 0.35);
      }
      .cierre-titulo {
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: 17px;
        font-weight: 600;
        color: var(--color-foreground);
      }
      .cierre-titulo app-icon {
        color: var(--color-primary);
      }
      a {
        cursor: pointer;
      }
    `,
  ],
})
export class CierreComponent implements OnInit {
  fecha = '';
  estado: CierreEstado | null = null;
  cuadre: CuadreCaja | null = null;
  cargando = false;
  cerrando = false;

  private readonly service = inject(CajaService);
  private readonly reporteService = inject(ReporteService);
  private readonly toast = inject(ToastService);

  ngOnInit(): void {
    this.fecha = this.hoyIso();
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.service.estadoCierre(this.fecha).subscribe({
      next: (estado) => {
        this.estado = {
          cerrado: !!estado?.cerrado,
          fecha: estado?.fecha ?? this.fecha,
          total: Number(estado?.total ?? 0),
        };
        this.cargando = false;
      },
      error: () => {
        this.estado = null;
        this.cargando = false;
      },
    });

    this.reporteService.cuadreCaja(this.fecha, this.fecha).subscribe({
      next: (c) =>
        (this.cuadre = {
          entradas: Number(c?.entradas ?? 0),
          salidas: Number(c?.salidas ?? 0),
          saldo: Number(c?.saldo ?? 0),
          porMoneda: c?.porMoneda ?? [],
        }),
      error: () => (this.cuadre = null),
    });
  }

  cerrar(): void {
    if (!confirm(`¿Cerrar la caja del ${this.fecha}? Esta acción no se puede deshacer.`)) return;
    this.cerrando = true;
    this.service.cerrar(this.fecha).subscribe({
      next: (r) => {
        this.cerrando = false;
        this.toast.success('Caja cerrada correctamente.');
        this.estado = {
          cerrado: r?.cerrado ?? true,
          fecha: r?.fecha ?? this.fecha,
          total: Number(r?.total ?? 0),
        };
      },
      error: () => (this.cerrando = false),
    });
  }

  private hoyIso(): string {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
