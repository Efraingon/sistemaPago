import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import { Custodio, CustodioMovimiento } from '../models';

/** Respuesta cruda de movimientos del backend (VFP COMPENSACION). */
interface MovimientosCrudos {
  items?: Array<{
    id?: string;
    fecha?: string;
    descripcion?: string | null;
    monedaId?: string;
    abonos?: number;
    retiros?: number;
  }>;
  total?: number;
  disponible?: number;
}

@Injectable({ providedIn: 'root' })
export class CustodioService extends ApiService {
  /** Saldos de custodia por cliente y moneda (VFP COMPENSACION agregado). */
  listar(params?: QueryParams): Observable<Custodio[]> {
    return this.get<Custodio[]>('/api/custodios/saldos', params);
  }

  /**
   * Movimientos de custodia de un cliente, normalizados para la UI:
   *  - `monto`  = abonos − retiros (+entrada / −salida)
   *  - `tipo`   = 'entrada' | 'salida'
   *  - `saldo`  = saldo disponible DESPUÉS del movimiento, recalculado hacia
   *    atrás desde el saldo actual (`disponible`) porque el backend los
   *    devuelve en orden descendente.
   */
  movimientos(clienteId: string): Observable<CustodioMovimiento[]> {
    return this.get<MovimientosCrudos>(`/api/custodios/${clienteId}/movimientos`).pipe(
      map((r) => this.normalizar(r)),
    );
  }

  recibir(body: { clienteId: string; monedaId: string; monto: number; descripcion?: string }): Observable<unknown> {
    return this.post<unknown>('/api/custodios/recibir', body);
  }

  entregar(body: { clienteId: string; monedaId: string; monto: number; descripcion?: string }): Observable<unknown> {
    return this.post<unknown>('/api/custodios/entregar', body);
  }

  private normalizar(r: MovimientosCrudos): CustodioMovimiento[] {
    const items = r?.items ?? [];
    let saldo = r?.disponible ?? 0;
    const out: CustodioMovimiento[] = [];
    for (const it of items) {
      const monto = (it.abonos ?? 0) - (it.retiros ?? 0);
      out.push({
        id: it.id ?? '',
        fecha: it.fecha ?? '',
        descripcion: it.descripcion ?? undefined,
        monedaId: it.monedaId,
        monto,
        tipo: monto >= 0 ? 'entrada' : 'salida',
        saldo,
      });
      // el más reciente ya lleva el saldo actual; retrocedemos para el siguiente (más antiguo)
      saldo -= monto;
    }
    return out;
  }
}
