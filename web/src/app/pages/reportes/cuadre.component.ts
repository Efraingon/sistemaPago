import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReporteService } from '../../core/services/reporte.service';
import { ComprobanteService } from '../../core/services/comprobante.service';
import { CuadreCaja, Configuracion } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';
import { exportarCsv, imprimir } from '../../shared/export';

@Component({
  selector: 'app-cuadre',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent, FechaPipe, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Reportes</p>
        <h1 class="page-title">Cuadre de caja</h1>
        <p class="page-sub">Entradas, salidas y saldo consolidado por moneda en un rango de fechas.</p>
      </div>
      <div class="row gap-2 wrap">
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/estado-general">Estado general</a>
        <a class="btn btn-ghost btn-sm" routerLink="/reportes/estado-cuenta">Estado de cuenta</a>
      </div>
      <div class="row gap-2 wrap">
        <button class="btn btn-ghost btn-sm" type="button" (click)="hoy()">
          <app-icon name="calendar" [size]="14" /> Hoy
        </button>
        <button class="btn btn-secondary btn-sm" type="button" (click)="exportar()">
          <app-icon name="arrowDown" [size]="14" /> CSV
        </button>
        <button class="btn btn-primary btn-sm" type="button" (click)="imprimir()">
          <app-icon name="print" [size]="14" /> Imprimir
        </button>
        <button class="btn btn-secondary btn-sm" type="button" (click)="cargar()">
          <app-icon name="refresh" [size]="14" /> Calcular
        </button>
      </div>
    </div>

    <div class="toolbar">
      <div class="form-group w-auto">
        <label class="form-label" for="desde">Desde</label>
        <input id="desde" class="input" type="date" [(ngModel)]="desde" (ngModelChange)="cargar()" />
      </div>
      <div class="form-group w-auto">
        <label class="form-label" for="hasta">Hasta</label>
        <input id="hasta" class="input" type="date" [(ngModel)]="hasta" (ngModelChange)="cargar()" />
      </div>
    </div>

    @if (cargando) {
      <div class="card"><div class="loading-panel"><span class="spinner"></span> Calculando cuadre…</div></div>
    } @else {
      <div class="print-area">
      @if (!empresa) {
        <div class="loading-panel"><span class="spinner"></span> Cargando encabezado…</div>
      } @else {
        <header class="papel-head">
          <div class="papel-empresa">
            <strong>{{ empresa.nombreEmpresa || 'Monrys Capital' }}</strong>
            @if (empresa.rif) { <span>RIF: {{ empresa.rif }}</span> }
            @if (empresa.direccion) { <span>{{ empresa.direccion }}</span> }
            @if (empresa.telefono) { <span>Tel: {{ empresa.telefono }}</span> }
          </div>
          <div class="papel-titulo">
            <h1>Cuadre de Caja</h1>
            <p class="papel-periodo">{{ desde | fecha }} → {{ hasta | fecha }}</p>
          </div>
        </header>
      }
      <div class="grid-3 mb-4">
        <article class="kpi">
          <span class="kpi-label"><app-icon name="arrowDown" [size]="14" /> Entradas</span>
          <span class="kpi-value text-success">{{ cuadre.entradas | monto }}</span>
          <span class="kpi-sub">Bs acumulados en el rango</span>
        </article>
        <article class="kpi">
          <span class="kpi-label"><app-icon name="arrowUp" [size]="14" /> Salidas</span>
          <span class="kpi-value text-danger">{{ cuadre.salidas | monto }}</span>
          <span class="kpi-sub">Bs acumulados en el rango</span>
        </article>
        <article class="kpi">
          <span class="kpi-label"><app-icon name="wallet" [size]="14" /> Saldo</span>
          <span class="kpi-value">{{ cuadre.saldo | monto }}</span>
          <span class="kpi-sub">Bs · {{ desde | fecha }} → {{ hasta | fecha }}</span>
        </article>
      </div>

      <section class="card" style="padding: 0">
        <h2 class="card-title" style="padding: 18px 20px 0; margin-bottom: 10px">Desglose por moneda</h2>
        @if (!cuadre.porMoneda.length) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="coin" [size]="32" /></span>
            <strong>Sin desglose por moneda</strong>
            <span class="small">El backend no devolvió montos por moneda para este rango.</span>
          </div>
        } @else {
          <div class="table-wrap" style="border: 0; border-radius: 0">
            <table class="table">
              <thead>
                <tr>
                  <th>Moneda</th>
                  <th class="num">Saldo anterior</th>
                  <th class="num">Entradas</th>
                  <th class="num">Salidas</th>
                  <th class="num">Saldo final</th>
                  <th class="num">≈ Bs</th>
                </tr>
              </thead>
              <tbody>
                @for (m of cuadre.porMoneda; track m.moneda || m.descripcion) {
                  <tr>
                    <td>{{ m.moneda || m.descripcion || '—' }}</td>
                    <td class="num">{{ (m.saldoAnterior ?? 0) | monto }}</td>
                    <td class="num text-success">{{ (m.entradas ?? 0) | monto }}</td>
                    <td class="num text-danger">{{ (m.salidas ?? 0) | monto }}</td>
                    <td class="num">{{ (m.saldoFinal ?? m.monto ?? 0) | monto }}</td>
                    <td class="num">{{ (m.saldoFinalBs ?? 0) | monto }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
      </div>
    }
  `,
  styles: [
    `
      a,
      button {
        cursor: pointer;
      }
      .page-header > div:last-child {
        margin-inline-start: auto;
      }

      /* ------------------------------------------------ encabezado de papel */
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
      .papel-periodo {
        margin: 6px 0 0;
        font-size: 12px;
        color: #475569;
        font-family: var(--font-mono, 'IBM Plex Mono', monospace);
      }

      @media print {
        .papel-head {
          border-bottom: 3px double #000 !important;
          padding-bottom: 14px;
          margin-bottom: 20px;
        }
        .papel-empresa strong,
        .papel-titulo h1,
        .papel-periodo {
          color: #000 !important;
        }
      }
    `,
  ],
})
export class CuadreComponent implements OnInit {
  desde = '';
  hasta = '';
  cargando = false;
  cuadre: CuadreCaja = { entradas: 0, salidas: 0, saldo: 0, porMoneda: [] };
  empresa: Configuracion | null = null;

  private readonly service = inject(ReporteService);
  private readonly configService = inject(ComprobanteService);

  ngOnInit(): void {
    this.hoy();
    this.configService.configuracion().subscribe({
      next: (c) => (this.empresa = c),
      error: () => (this.empresa = null),
    });
    this.cargar();
  }

  hoy(): void {
    const f = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    const iso = `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
    this.desde = iso;
    this.hasta = iso;
  }

  cargar(): void {
    if (!this.desde || !this.hasta) return;
    this.cargando = true;
    this.service.cuadreCaja(this.desde, this.hasta).subscribe({
      next: (c) => {
        this.cuadre = {
          entradas: Number(c?.entradas ?? 0),
          salidas: Number(c?.salidas ?? 0),
          saldo: Number(c?.saldo ?? 0),
          porMoneda: c?.porMoneda ?? [],
        };
        this.cargando = false;
      },
      error: () => {
        this.cuadre = { entradas: 0, salidas: 0, saldo: 0, porMoneda: [] };
        this.cargando = false;
      },
    });
  }

  /** Imprime el cuadre (zona .print-area). */
  imprimir(): void {
    imprimir();
  }

  /** Exporta el desglose por moneda a CSV compatible con Excel. */
  exportar(): void {
    const encabezados = ['Moneda', 'Saldo anterior', 'Entradas', 'Salidas', 'Saldo final', '≈ Bs'];
    const filas = this.cuadre.porMoneda.map((m) => [
      m.moneda || m.descripcion || '',
      Number(m.saldoAnterior ?? 0),
      Number(m.entradas ?? 0),
      Number(m.salidas ?? 0),
      Number(m.saldoFinal ?? m.monto ?? 0),
      Number(m.saldoFinalBs ?? 0),
    ]);
    filas.push(['TOTAL', 0, this.cuadre.entradas, this.cuadre.salidas, this.cuadre.saldo, 0]);
    exportarCsv(`cuadre_caja_${this.desde}_${this.hasta}`, encabezados, filas);
  }
}
