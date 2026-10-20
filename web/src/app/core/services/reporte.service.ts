import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import {
  ConsolidadoItem,
  CuadreCaja,
  EstadoCuenta,
  EstadoGeneralItem,
  ReporteCaja,
} from '../models';

@Injectable({ providedIn: 'root' })
export class ReporteService extends ApiService {
  consolidado(fecha: string): Observable<{ items?: ConsolidadoItem[] } | ConsolidadoItem[]> {
    return this.get<any>('/api/reportes/consolidado', { fecha });
  }

  estadoGeneral(params: { fechaCorte?: string; clienteId?: string | null }): Observable<EstadoGeneralItem[]> {
    return this.get<any>('/api/reportes/estado-general', params as QueryParams);
  }

  cuadreCaja(desde: string, hasta: string): Observable<CuadreCaja> {
    return this.get<any>('/api/reportes/cuadre-caja', { desde, hasta }).pipe(
      map((r) => this.normalizarCuadre(r)),
    );
  }

  /** La API expone `entradasBs/salidasBs/saldoBs` y `monedas[]`;
   *  la UI consume `entradas/salidas/saldo` y `porMoneda`. */
  normalizarCuadre(r: any): CuadreCaja {
    if (!r) return { entradas: 0, salidas: 0, saldo: 0, porMoneda: [] };
    const monedas = Array.isArray(r.monedas) ? r.monedas : Array.isArray(r.porMoneda) ? r.porMoneda : [];
    return {
      entradas: Number(r.entradasBs ?? r.entradas ?? 0),
      salidas: Number(r.salidasBs ?? r.salidas ?? 0),
      saldo: Number(r.saldoBs ?? r.saldo ?? 0),
      porMoneda: monedas.map((m: any) => ({
        moneda: m.moneda ?? m.descripcion,
        descripcion: m.descripcion,
        monto: Number(m.saldoFinal ?? m.monto ?? 0),
        total: Number(m.saldoFinal ?? m.total ?? 0),
        nacional: !!m.nacional,
        saldoAnterior: Number(m.saldoAnterior ?? 0),
        entradas: Number(m.entradas ?? 0),
        salidas: Number(m.salidas ?? 0),
        saldoFinal: Number(m.saldoFinal ?? 0),
        saldoFinalBs: Number(m.saldoFinalBs ?? 0),
      })),
    };
  }

  caja(desde: string, hasta: string, tipo: 'ingreso' | 'egreso' | 'todos' = 'todos'): Observable<ReporteCaja> {
    return this.get<ReporteCaja>('/api/reportes/caja', { desde, hasta, tipo });
  }

  estadoCuenta(clienteId: string, desde?: string, hasta?: string): Observable<EstadoCuenta> {
    return this.get<any>(`/api/reportes/estado-cuenta/${clienteId}`, { desde, hasta });
  }

  /** Normaliza respuestas que puedan venir envueltas ({items:[…]}) o planas.
   *  `estado-general` devuelve { fechaCorte, tasaActiva, clientes:[…] }. */
  normalizarLista<T>(respuesta: any): T[] {
    if (Array.isArray(respuesta)) return respuesta as T[];
    if (respuesta && Array.isArray(respuesta.items)) return respuesta.items as T[];
    if (respuesta && Array.isArray(respuesta.clientes)) return respuesta.clientes as T[];
    if (respuesta && Array.isArray(respuesta.lineas)) return respuesta.lineas as T[];
    if (respuesta && Array.isArray(respuesta.movimientos)) return respuesta.movimientos as T[];
    return [];
  }
}
