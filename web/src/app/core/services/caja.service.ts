import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import {
  Arqueo,
  CajaMovimiento,
  CajaMovimientoPage,
  CierreEstado,
  Codificaciones,
  NuevoMovimientoCaja,
} from '../models';

@Injectable({ providedIn: 'root' })
export class CajaService extends ApiService {
  /** Codificaciones de ingresos/egresos para el libro de caja. */
  codificaciones(): Observable<Codificaciones> {
    return this.get<Codificaciones>('/api/configuracion/codificaciones');
  }

  listar(params?: QueryParams): Observable<CajaMovimientoPage> {
    return this.get<any>('/api/caja', params).pipe(
      map((p) => ({
        ...p,
        items: ((p?.items ?? []) as any[]).map((m) => ({
          ...m,
          // La API expone `estado: ACTIVO|ANULADO`; la UI consume el flag booleano.
          anulado: String(m.estado ?? '').toUpperCase() === 'ANULADO',
          cerrado: !!m.cierreFecha,
        })),
      })),
    );
  }

  /** El controller expone POST /caja/ingresos y /caja/egresos: la ruta decide el sentido. */
  crear(body: NuevoMovimientoCaja): Observable<CajaMovimiento> {
    const ruta = String(body.origen).toUpperCase() === 'IN' ? '/api/caja/ingresos' : '/api/caja/egresos';
    return this.post<CajaMovimiento>(ruta, body);
  }

  /** El controller expone DELETE /api/caja/{ejercicio}/{id} con body `{ motivo }`. */
  anular(ejercicio: number | string | undefined, id: number | string, motivo: string): Observable<CajaMovimiento> {
    return this.del<CajaMovimiento>(`/api/caja/${ejercicio ?? '0'}/${id}`, { motivo });
  }

  estadoCierre(fecha: string): Observable<CierreEstado> {
    return this.get<CierreEstado>('/api/caja/cierre/estado', { fecha });
  }

  cerrar(fecha: string): Observable<CierreEstado> {
    return this.post<CierreEstado>('/api/caja/cierre', { fecha });
  }

  arqueo(fecha: string): Observable<Arqueo> {
    return this.get<Arqueo>('/api/caja/arqueo', { fecha });
  }
}
