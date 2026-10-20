import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { Banco, CuentaBanco } from '../models';

/**
 * Bancos y cuentas propias de la casa.
 * Ids de banco: formato VFP "00001" (string); ids de cuenta: serial numérico.
 */
@Injectable({ providedIn: 'root' })
export class BancoService extends ApiService {
  listar(): Observable<Banco[]> {
    return this.get<Banco[]>('/api/bancos');
  }

  obtener(id: string): Observable<Banco> {
    return this.get<Banco>(`/api/bancos/${id}`);
  }

  crear(body: Partial<Banco>): Observable<Banco> {
    return this.post<Banco>('/api/bancos', body);
  }

  actualizar(id: string, body: Partial<Banco>): Observable<Banco> {
    return this.put<Banco>(`/api/bancos/${id}`, body);
  }

  eliminar(id: string): Observable<void> {
    return this.del<void>(`/api/bancos/${id}`);
  }

  // ---------------------------------------------------------------- cuentas
  /** Cuentas de un banco (GET /api/cuentas-banco?bancoId=). */
  cuentas(bancoId: string): Observable<CuentaBanco[]> {
    return this.get<{ items?: CuentaBanco[] } | CuentaBanco[]>('/api/cuentas-banco', { bancoId }).pipe(
      map((r) => (Array.isArray(r) ? r : (r?.items ?? []))),
    );
  }

  crearCuenta(bancoId: string, body: Partial<CuentaBanco>): Observable<CuentaBanco> {
    return this.post<CuentaBanco>(`/api/bancos/${bancoId}/cuentas`, body);
  }

  eliminarCuenta(bancoId: string, cuentaId: number): Observable<void> {
    return this.del<void>(`/api/bancos/${bancoId}/cuentas/${cuentaId}`);
  }
}
