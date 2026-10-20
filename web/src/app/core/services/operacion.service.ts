import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import {
  Cuota,
  NuevaOperacion,
  Operacion,
  OperacionDetalle,
  OperacionPage,
  TipoOperacion,
} from '../models';

/** Compras y ventas comparten contrato (ventas = espejo).
 *  `tipo` decide el prefijo: /api/compras o /api/ventas. */
@Injectable({ providedIn: 'root' })
export class OperacionService extends ApiService {
  listar(tipo: TipoOperacion, params?: QueryParams): Observable<OperacionPage> {
    return this.get<OperacionPage>(`/api/${tipo}s`, params);
  }

  obtener(tipo: TipoOperacion, id: number): Observable<OperacionDetalle> {
    return this.get<OperacionDetalle>(`/api/${tipo}s/${id}`);
  }

  crear(tipo: TipoOperacion, body: NuevaOperacion): Observable<Operacion> {
    return this.post<Operacion>(`/api/${tipo}s`, body);
  }

  actualizar(tipo: TipoOperacion, id: number, body: NuevaOperacion): Observable<Operacion> {
    return this.put<Operacion>(`/api/${tipo}s/${id}`, body);
  }

  anular(tipo: TipoOperacion, id: number, motivo: string): Observable<Operacion> {
    return this.post<Operacion>(`/api/${tipo}s/${id}/anular`, { motivo });
  }

  recepcion(
    tipo: TipoOperacion,
    id: number,
    body: { fecha: string; cantidad: number; deCaja: boolean; bancoId?: number | null }
  ): Observable<Operacion> {
    return this.post<Operacion>(`/api/${tipo}s/${id}/recepciones`, body);
  }

  pago(
    tipo: TipoOperacion,
    id: number,
    body: {
      fecha: string;
      monto: number;
      bancoDebitoId?: number | null;
      bancoCreditoId?: number | null;
      tipoPago?: string;
    }
  ): Observable<Operacion> {
    return this.post<Operacion>(`/api/${tipo}s/${id}/pagos`, body);
  }

  /** Extrae las cuotas de la respuesta sea cual sea el formato.
   *  La API real devuelve `recepciones` y `pagos` por separado: se fusionan
   *  y ordenan por fecha para mostrar la línea de tiempo completa. */
  cuotasDe(detalle: OperacionDetalle | null): Cuota[] {
    if (!detalle) return [];
    if (detalle.cuotas?.length) return detalle.cuotas;
    // La API no envía `tipo`: se sintetiza aquí (capa de normalización).
    const receps = (detalle.recepciones ?? []).map((r) => ({
      ...r,
      tipo: r.tipo ?? 'RECEPCION',
    }));
    const pagos = (detalle.pagos ?? []).map((p) => ({
      ...p,
      tipo: p.tipo ?? 'PAGO',
    }));
    return [...receps, ...pagos].sort(
      (a, b) => (a.fecha ?? '').localeCompare(b.fecha ?? '') || (a.id ?? 0) - (b.id ?? 0)
    );
  }
}
