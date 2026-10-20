import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ClienteService } from '../../core/services/cliente.service';
import { CajaService } from '../../core/services/caja.service';
import { ReporteService } from '../../core/services/reporte.service';
import { OperacionService } from '../../core/services/operacion.service';
import { TasaService } from '../../core/services/tasas.service';
import { CuadreCaja, EstadoGeneralItem, Operacion, Tasa } from '../../core/models';
import { IconComponent } from '../../shared/icon/icon.component';
import { FechaPipe, MontoPipe } from '../../shared/pipes/pipes';

interface Kpi {
  etiqueta: string;
  valor: string;
  sub: string;
  icono: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, IconComponent, FechaPipe, MontoPipe],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Sesión de operaciones</p>
        <h1 class="page-title">Dashboard</h1>
        <p class="page-sub">Resumen del día · {{ hoy | fecha }}</p>
      </div>
      <div class="row gap-2 wrap">
        <a class="btn btn-secondary btn-sm" routerLink="/caja/cierre">
          <app-icon name="lock" [size]="15" /> Cierre de caja
        </a>
        <a class="btn btn-primary btn-sm" routerLink="/operaciones/compras">
          <app-icon name="plus" [size]="15" /> Nueva operación
        </a>
      </div>
    </div>

    <!-- KPIs -->
    <div class="grid-4 mb-4">
      @for (k of kpis; track k.etiqueta) {
        <article class="kpi">
          <span class="kpi-label"><app-icon [name]="k.icono" [size]="14" /> {{ k.etiqueta }}</span>
          <span class="kpi-value">{{ k.valor }}</span>
          <span class="kpi-sub">{{ k.sub }}</span>
        </article>
      }
    </div>

    <div class="grid-2">
      <!-- Estado general -->
      <section class="card" style="padding: 0">
        <div class="row-between" style="padding: 18px 20px 14px">
          <h2 class="card-title" style="margin: 0">Estado general por cliente</h2>
          <a class="small" routerLink="/reportes/estado-general">Ver reporte</a>
        </div>
        @if (cargando) {
          <div class="loading-panel"><span class="spinner"></span> Cargando estado general…</div>
        } @else if (!estadoGeneral.length) {
          <div class="empty-state">
            <span class="empty-ico"><app-icon name="inbox" [size]="34" /></span>
            <strong>Sin movimientos registrados</strong>
            <span class="small">Aún no hay operaciones para la fecha de corte.</span>
          </div>
        } @else {
          <div class="table-wrap" style="border: 0; border-radius: 0">
            <table class="table">
              <thead>
                <tr>
                  <th>Cliente</th>
                  <th class="num">Compras Bs</th>
                  <th class="num">Ventas Bs</th>
                  <th class="num">Consolidado</th>
                </tr>
              </thead>
              <tbody>
                @for (f of estadoGeneral.slice(0, 8); track f.cliente) {
                  <tr>
                    <td>{{ f.cliente }}</td>
                    <td class="num">{{ f.comprasBs | monto }}</td>
                    <td class="num">{{ f.ventasBs | monto }}</td>
                    <td class="num" [class.text-success]="f.consolidado >= 0" [class.text-danger]="f.consolidado < 0">
                      {{ f.consolidado | monto }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>

      <!-- Cuadre del día -->
      <section class="card">
        <div class="row-between mb-3">
          <h2 class="card-title" style="margin: 0">Cuadre del día</h2>
          <a class="small" routerLink="/reportes/cuadre">Ver cuadre</a>
        </div>

        @if (cargando) {
          <div class="loading-panel"><span class="spinner"></span> Calculando cuadre…</div>
        } @else {
          <div class="cuadre">
            <div class="cuadre-line">
              <span><app-icon name="arrowDown" [size]="15" /> Entradas</span>
              <strong class="mono text-success">{{ cuadre.entradas | monto }}</strong>
            </div>
            <div class="cuadre-line">
              <span><app-icon name="arrowUp" [size]="15" /> Salidas</span>
              <strong class="mono text-danger">{{ cuadre.salidas | monto }}</strong>
            </div>
            <div class="cuadre-total">
              <span>Saldo del día</span>
              <strong class="mono">{{ cuadre.saldo | monto }} Bs</strong>
            </div>

            @if (cuadre.porMoneda?.length) {
              <div class="por-moneda">
                @for (m of cuadre.porMoneda; track m.moneda || m.descripcion) {
                  <div class="pm-item">
                    <span class="small text-muted">{{ m.moneda || m.descripcion }}</span>
                    <span class="mono">{{ (m.monto ?? m.total ?? 0) | monto }}</span>
                  </div>
                }
              </div>
            }
          </div>

          <div class="mt-3 row gap-2 wrap">
            <a class="btn btn-ghost btn-sm" routerLink="/caja">
              <app-icon name="wallet" [size]="15" /> Movimientos de caja
            </a>
            <a class="btn btn-ghost btn-sm" routerLink="/reportes/estado-cuenta">
              <app-icon name="file" [size]="15" /> Estados de cuenta
            </a>
          </div>
        }
      </section>
    </div>

    <!-- Operaciones recientes -->
    <section class="card mt-4" style="padding: 0">
      <div class="row-between" style="padding: 18px 20px 14px">
        <h2 class="card-title" style="margin: 0">Últimas operaciones</h2>
        <div class="row gap-2">
          <a class="small" routerLink="/operaciones/compras">Compras</a>
          <span class="text-muted">·</span>
          <a class="small" routerLink="/operaciones/ventas">Ventas</a>
        </div>
      </div>
      @if (!ultimas.length && !cargando) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="swap" [size]="34" /></span>
          <strong>Sin operaciones recientes</strong>
        </div>
      } @else if (ultimas.length) {
        <div class="table-wrap" style="border: 0; border-radius: 0">
          <table class="table">
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Fecha</th>
                <th>Cliente</th>
                <th class="num">USD</th>
                <th class="num">Tasa</th>
                <th class="num">Monto Bs</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              @for (op of ultimas; track op.id) {
                <tr>
                  <td>
                    <span class="badge" [class.badge-gold]="op.tipo === 'compra'" [class.badge-accent]="op.tipo === 'venta'">
                      {{ op.tipo === 'venta' ? 'Venta' : 'Compra' }}
                    </span>
                  </td>
                  <td class="mono">{{ op.fecha | fecha }}</td>
                  <td>{{ op.cliente || '—' }}</td>
                  <td class="num">{{ op.cantidad | monto }}</td>
                  <td class="num">{{ op.tasa | monto }}</td>
                  <td class="num">{{ (op.montoBs ?? 0) | monto }}</td>
                  <td>
                    <span class="badge" [class.badge-success]="op.estado === 'completada'" [class.badge-gold]="op.estado === 'pendiente'" [class.badge-danger]="op.estado === 'anulada'">
                      {{ op.estado || '—' }}
                    </span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .cuadre {
        display: flex;
        flex-direction: column;
        gap: 13px;
      }
      .cuadre-line {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        font-size: 14px;
        color: var(--color-muted-foreground);
      }
      .cuadre-line span {
        display: inline-flex;
        align-items: center;
        gap: 8px;
      }
      .cuadre-line strong {
        font-size: 16px;
        color: var(--color-foreground);
      }
      .cuadre-total {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding-top: 13px;
        border-top: 1px solid var(--hairline);
        font-size: 13.5px;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--color-muted-foreground);
      }
      .cuadre-total strong {
        font-size: 21px;
        color: var(--color-primary);
        font-weight: 600;
      }
      .por-moneda {
        display: flex;
        flex-direction: column;
        gap: 6px;
        padding-top: 12px;
        border-top: 1px dashed var(--color-border);
      }
      .pm-item {
        display: flex;
        justify-content: space-between;
        gap: 10px;
        font-size: 13px;
      }
      a {
        cursor: pointer;
      }
    `,
  ],
})
export class DashboardComponent implements OnInit {
  readonly hoy = new Date();
  cargando = true;

  kpis: Kpi[] = [];
  estadoGeneral: EstadoGeneralItem[] = [];
  cuadre: CuadreCaja = { entradas: 0, salidas: 0, saldo: 0, porMoneda: [] };
  cierreCerrado = false;
  ultimas: Operacion[] = [];

  private readonly cajaService = inject(CajaService);
  private readonly reporteService = inject(ReporteService);
  private readonly clienteService = inject(ClienteService);
  private readonly operacionService = inject(OperacionService);
  private readonly tasaService = inject(TasaService);

  ngOnInit(): void {
    const hoyStr = this.iso(new Date());
    const vacio = { items: [], total: 0 };

    forkJoin({
      cuadre: this.reporteService.cuadreCaja(hoyStr, hoyStr).pipe(catchError(() => of(null))),
      cierre: this.cajaService.estadoCierre(hoyStr).pipe(catchError(() => of(null))),
      estadoGeneral: this.reporteService.estadoGeneral({ fechaCorte: hoyStr }).pipe(
        catchError(() => of([]))
      ),
      clientes: this.clienteService.listar({ pagina: 1, tamano: 1 }).pipe(catchError(() => of(vacio))),
      compras: this.operacionService
        .listar('compra', { pagina: 1, tamano: 6, hasta: hoyStr })
        .pipe(catchError(() => of(vacio))),
      ventas: this.operacionService
        .listar('venta', { pagina: 1, tamano: 6, hasta: hoyStr })
        .pipe(catchError(() => of(vacio))),
      // Conteo real del día (los KPI "del día" no deben confundirse con el acumulado)
      comprasHoy: this.operacionService
        .listar('compra', { pagina: 1, tamano: 1, desde: hoyStr, hasta: hoyStr })
        .pipe(catchError(() => of(vacio))),
      ventasHoy: this.operacionService
        .listar('venta', { pagina: 1, tamano: 1, desde: hoyStr, hasta: hoyStr })
        .pipe(catchError(() => of(vacio))),
      tasa: this.tasaService.actual().pipe(catchError(() => of(null))),
    }).subscribe({
      next: (r: any) => {
        const cuadre = (r?.cuadre ?? null) as CuadreCaja | null;
        const cierre = (r?.cierre ?? null) as { cerrado?: boolean } | null;
        const estadoGeneral = this.reporteService.normalizarLista<EstadoGeneralItem>(r?.estadoGeneral);
        const clientes = (r?.clientes ?? vacio) as { total?: number };
        const compras = (r?.compras ?? vacio) as { items: Operacion[]; total?: number };
        const ventas = (r?.ventas ?? vacio) as { items: Operacion[]; total?: number };
        const comprasHoy = (r?.comprasHoy ?? vacio) as { total?: number };
        const ventasHoy = (r?.ventasHoy ?? vacio) as { total?: number };
        const tasa = (r?.tasa ?? null) as Tasa | null;

        this.cargando = false;
        this.cuadre = this.normalizarCuadre(cuadre);
        this.cierreCerrado = !!cierre?.cerrado;
        this.estadoGeneral = estadoGeneral;
        this.ultimas = [...(compras.items ?? []), ...(ventas.items ?? [])]
          .sort((a, b) => String(b.fecha).localeCompare(String(a.fecha)))
          .slice(0, 6);
        this.construirKpis(tasa, clientes.total ?? 0, compras.total ?? 0, ventas.total ?? 0,
          comprasHoy.total ?? 0, ventasHoy.total ?? 0);
      },
      error: () => {
        this.cargando = false;
        this.construirKpis(null, 0, 0, 0);
      },
    });
  }

  private construirKpis(tasa: Tasa | null, clientesActivos: number, compras: number, ventas: number,
    comprasHoy = 0, ventasHoy = 0): void {
    const saldoBs = this.cuadre.saldo || 0;
    const porUsd = this.cuadre.porMoneda?.find((m) => /usd|d[oó]lar/i.test(String(m.moneda ?? m.descripcion ?? '')));
    this.kpis = [
      {
        etiqueta: 'Saldo de caja',
        valor: `${this.fmt(saldoBs)} Bs`,
        sub: this.cierreCerrado ? 'Día cerrado' : 'Cuadre del día',
        icono: 'wallet',
      },
      {
        etiqueta: 'Caja en USD',
        valor: porUsd ? `$ ${this.fmt(porUsd.monto ?? porUsd.total ?? 0)}` : '—',
        sub: porUsd ? 'Disponible' : 'Sin registro hoy',
        icono: 'coin',
      },
      {
        etiqueta: 'Tasa activa',
        valor: tasa ? `${this.fmt(tasa.tasaActiva)} Bs` : '—',
        sub: tasa ? `Compra ${this.fmt(tasa.tasaCompra)} · Venta ${this.fmt(tasa.tasaVenta)} Bs` : 'Sin tasa configurada',
        icono: 'percent',
      },
      {
        etiqueta: 'Operaciones del día',
        valor: String(comprasHoy + ventasHoy),
        sub: `${compras} compras · ${ventas} ventas acumuladas · ${clientesActivos} clientes`,
        icono: 'trending',
      },
    ];
  }

  private normalizarCuadre(c: any): CuadreCaja {
    // Normalización centralizada en ReporteService (contrato API *Bs / monedas[]).
    return this.reporteService.normalizarCuadre(c);
  }

  private fmt(n: number): string {
    return (Number(n) || 0).toLocaleString('es-VE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private iso(f: Date): string {
    const p = (n: number) => String(n).padStart(2, '0');
    return `${f.getFullYear()}-${p(f.getMonth() + 1)}-${p(f.getDate())}`;
  }
}
