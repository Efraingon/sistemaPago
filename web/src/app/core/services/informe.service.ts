import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import { Informe, InformeCatalogo } from '../models/informe';

/** Centro de Reportes: catálogo + ejecución del motor genérico de informes. */
@Injectable({ providedIn: 'root' })
export class InformeService extends ApiService {
  catalogo(): Observable<InformeCatalogo> {
    return this.get<InformeCatalogo>('/api/informes');
  }

  ejecutar(id: string, params: QueryParams): Observable<Informe> {
    return this.get<Informe>(`/api/informes/${id}`, params);
  }
}
