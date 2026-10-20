import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { Moneda, Tasa, TasaCalendarioItem } from '../models';

@Injectable({ providedIn: 'root' })
export class MonedaService extends ApiService {
  listar(): Observable<Moneda[]> {
    return this.get<Moneda[]>('/api/monedas');
  }
}

@Injectable({ providedIn: 'root' })
export class TasaService extends ApiService {
  actual(): Observable<Tasa> {
    return this.get<Tasa>('/api/tasas');
  }

  guardar(body: { tasaActiva: number; tasaCompra: number; tasaVenta: number }): Observable<Tasa> {
    return this.put<Tasa>('/api/tasas', body);
  }

  calendario(desde: string, hasta: string): Observable<TasaCalendarioItem[]> {
    return this.get<TasaCalendarioItem[]>('/api/tasas/calendario', { desde, hasta });
  }
}
