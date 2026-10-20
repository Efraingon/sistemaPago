import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import { NuevaTransferencia, Transferencia } from '../models';

/** Resultado paginado del libro de transferencias. */
export interface TransferenciaPagina {
  items: Transferencia[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class TransferenciaService extends ApiService {
  /**
   * Libro de transferencias paginado (VFP CTASTRAN).
   * El backend pagina por defecto (tamano=20); normaliza `estado` → `anulado`.
   */
  listar(params?: QueryParams): Observable<TransferenciaPagina> {
    return this.get<{ items?: Transferencia[]; total?: number }>('/api/transferencias', params).pipe(
      map((r) => {
        const items = (r?.items ?? []).map((t) => ({
          ...t,
          anulado: String((t as any).estado ?? '').toUpperCase() === 'ANULADO',
        }));
        return { items, total: r?.total ?? items.length };
      }),
    );
  }

  crear(body: NuevaTransferencia): Observable<Transferencia> {
    return this.post<Transferencia>('/api/transferencias', body);
  }

  /** Anulación soft (DELETE /api/transferencias/{id} con motivo en el body). */
  anular(id: number, motivo: string): Observable<Transferencia> {
    return this.del<Transferencia>(`/api/transferencias/${id}`, { motivo });
  }
}
