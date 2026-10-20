import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InformeService } from '../../core/services/informe.service';
import { InformeDef, InformeFamilia } from '../../core/models/informe';
import { IconComponent } from '../../shared/icon/icon.component';

/** Hub del Centro de Reportes: lista los informes del catálogo (familias del
 *  VFP) y permite abrir cada uno con sus parámetros, imprimible y exportable. */
@Component({
  selector: 'app-centro-reportes',
  standalone: true,
  imports: [FormsModule, RouterLink, IconComponent],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Reportes</p>
        <h1 class="page-title">Centro de Reportes</h1>
        <p class="page-sub">
          Todos los informes del sistema, con la estructura de los reportes originales de Visual FoxPro.
          Cada uno se puede ejecutar, imprimir y exportar a CSV.
        </p>
      </div>
      <span class="badge badge-gold">{{ total }} informes</span>
    </div>

    <div class="toolbar">
      <div class="form-group" style="flex: 1 1 260px">
        <label class="form-label" for="buscar">Buscar informe</label>
        <div class="buscador">
          <app-icon name="search" [size]="16" />
          <input
            id="buscar"
            class="input"
            type="search"
            placeholder="Ej.: cuadre, extracto, custodios, lista…"
            [(ngModel)]="busqueda"
            (ngModelChange)="aplicarFiltro()"
            autocomplete="off"
          />
        </div>
      </div>
    </div>

    @if (cargando) {
      <div class="card"><div class="loading-panel"><span class="spinner"></span> Cargando catálogo de informes…</div></div>
    } @else if (error) {
      <div class="empty-state">
        <span class="empty-ico"><app-icon name="alert" [size]="32" /></span>
        <strong>No se pudo cargar el catálogo</strong>
        <span class="small">{{ error }}</span>
      </div>
    } @else {
      @for (fam of familias; track fam.codigo) {
        @if (fam.visible) {
          <section class="familia">
            @if (fam.informesVisibles.length) {
              <header class="familia-head">
                <h2>{{ fam.nombre }}</h2>
                <p class="small">{{ fam.descripcion }}</p>
              </header>
            }
            <div class="grid-3">
              @if (!fam.informesVisibles.length) {
                <article class="card informe-card nota-card">
                  <div class="informe-head">
                    <span class="informe-ico"><app-icon name="alert" [size]=\"18\" /></span>
                    <h3>{{ fam.nombre }}</h3>
                  </div>
                  <p class="small informe-desc">{{ fam.descripcion }}</p>
                </article>
              }
              @for (inf of fam.informesVisibles; track inf.id) {
                <article class="card informe-card">
                  <div class="informe-head">
                    <span class="informe-ico"><app-icon name="file" [size]="18" /></span>
                    <h3>{{ inf.nombre }}</h3>
                  </div>
                  @if (inf.descripcion) {
                    <p class="small informe-desc">{{ inf.descripcion }}</p>
                  }
                  @if (inf.origenVfp) {
                    <span class="chip chip-out origen" title="Reporte original de Visual FoxPro">{{ inf.origenVfp }}</span>
                  }
                  <div class="informe-acciones">
                    @if (inf.rutaPantalla) {
                      <a class="btn btn-secondary btn-sm" [routerLink]="inf.rutaPantalla">
                        <app-icon name="chart" [size]="14" /> Abrir pantalla
                      </a>
                    } @else {
                      <a class="btn btn-primary btn-sm" [routerLink]="['/reportes/informe', inf.id]">
                        <app-icon name="print" [size]="14" /> Ejecutar
                      </a>
                    }
                    @if (inf.parametros?.length) {
                      <span class="param-count">{{ inf.parametros.length }} parám.</span>
                    }
                  </div>
                </article>
              }
            </div>
          </section>
        }
      }

      @if (totalVisible === 0) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="search" [size]="32" /></span>
          <strong>Sin resultados</strong>
          <span class="small">Ningún informe coincide con «{{ busqueda }}».</span>
        </div>
      }
    }
  `,
  styles: [
    `
      .buscador {
        display: flex;
        align-items: center;
        gap: 8px;
        padding-inline-start: 12px;
        background: var(--surface-2, #0b1220);
        border: 1px solid var(--line, #1e293b);
        border-radius: 10px;
      }
      .buscador .input {
        border: 0;
        background: transparent;
        padding-inline-start: 0;
      }
      .buscador app-icon {
        color: var(--muted, #64748b);
      }
      .familia {
        margin-bottom: 34px;
      }
      .familia-head {
        display: flex;
        align-items: baseline;
        gap: 14px;
        flex-wrap: wrap;
        margin-bottom: 14px;
        padding-inline-start: 12px;
        border-inline-start: 3px solid var(--gold, #f59e0b);
      }
      .familia-head h2 {
        font-size: 15px;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        margin: 0;
      }
      .familia-head p {
        margin: 0;
      }
      .informe-card {
        display: flex;
        flex-direction: column;
        gap: 10px;
        transition: border-color 0.15s ease, transform 0.15s ease;
      }
      .informe-card:hover {
        border-color: var(--gold, #f59e0b);
        transform: translateY(-2px);
      }
      .informe-head {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .informe-head h3 {
        font-size: 15px;
        margin: 0;
      }
      .informe-ico {
        display: grid;
        place-items: center;
        width: 32px;
        height: 32px;
        border-radius: 9px;
        background: rgba(245, 158, 11, 0.12);
        color: var(--gold, #f59e0b);
        flex: 0 0 auto;
      }
      .informe-desc {
        margin: 0;
        min-height: 34px;
      }
      .origen {
        align-self: flex-start;
        font-size: 11px;
      }
      .informe-acciones {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        margin-top: auto;
        padding-top: 6px;
      }
      .param-count {
        font-size: 11px;
        color: var(--muted, #64748b);
      }
      .nota-card {
        border-style: dashed;
        background: transparent;
      }
      .nota-card:hover {
        transform: none;
      }
      @media (max-width: 640px) {
        .informe-desc {
          min-height: 0;
        }
      }
    `,
  ],
})
export class CentroReportesComponent implements OnInit {
  busqueda = '';
  cargando = true;
  error = '';
  familias: (InformeFamilia & { visible: boolean; informesVisibles: InformeDef[] })[] = [];
  total = 0;
  totalVisible = 0;

  private readonly service = inject(InformeService);

  ngOnInit(): void {
    this.service.catalogo().subscribe({
      next: (cat) => {
        this.familias = (cat?.familias ?? []).map((f) => ({
          ...f,
          visible: true,
          informesVisibles: f.informes ?? [],
        }));
        this.total = this.familias.reduce((n, f) => n + f.informes.length, 0);
        this.aplicarFiltro();
        this.cargando = false;
      },
      error: (e) => {
        this.error = e?.error?.title || e?.error?.detail || 'Error al consultar /api/informes.';
        this.cargando = false;
      },
    });
  }

  protected aplicarFiltro(): void {
    const t = this.busqueda.trim().toLowerCase();
    this.totalVisible = 0;
    for (const fam of this.familias) {
      const visibles = !t
        ? fam.informes
        : fam.informes.filter((i) =>
            [i.nombre, i.descripcion, i.origenVfp, fam.nombre].filter(Boolean).join(' ').toLowerCase().includes(t),
          );
      fam.informesVisibles = visibles;
      fam.visible = !t || visibles.length > 0;
      this.totalVisible += visibles.length;
    }
  }
}
