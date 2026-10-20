import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { Comprobante, Configuracion, TipoComprobante } from '../models';

/** /api/reportes/comprobante/{tipo}/{id} + datos de empresa para el encabezado. */
@Injectable({ providedIn: 'root' })
export class ComprobanteService extends ApiService {
  obtener(tipo: TipoComprobante, id: number): Observable<Comprobante> {
    return this.get<Comprobante>(`/api/reportes/comprobante/${tipo}/${id}`);
  }

  configuracion(): Observable<Configuracion> {
    return this.get<Configuracion>('/api/configuracion');
  }
}
