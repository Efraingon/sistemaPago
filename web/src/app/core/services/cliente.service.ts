import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import { Cliente, ClientePage, CuentaCliente } from '../models';

/** Contrato de la API: persona 1=natural, 2=jurídica. La UI trabaja con 'N'/'J'. */
const aLetra = (p: unknown): 'N' | 'J' => (Number(p) === 2 ? 'J' : 'N');
const aNumero = (p: unknown): number => (p === 'J' || Number(p) === 2 ? 2 : 1);

@Injectable({ providedIn: 'root' })
export class ClienteService extends ApiService {
  listar(params?: QueryParams): Observable<ClientePage> {
    return this.get<ClientePage>('/api/clientes', params).pipe(
      map((p) => ({ ...p, items: (p.items ?? []).map((c) => ({ ...c, persona: aLetra(c.persona) })) }))
    );
  }

  obtener(id: string): Observable<Cliente> {
    return this.get<Cliente>(`/api/clientes/${id}`).pipe(map((c) => ({ ...c, persona: aLetra(c.persona) })));
  }

  crear(body: Partial<Cliente>): Observable<Cliente> {
    return this.post<Cliente>('/api/clientes', this.enviar(body)).pipe(
      map((c) => ({ ...c, persona: aLetra(c.persona) }))
    );
  }

  actualizar(id: string, body: Partial<Cliente>): Observable<Cliente> {
    return this.put<Cliente>(`/api/clientes/${id}`, this.enviar(body)).pipe(
      map((c) => ({ ...c, persona: aLetra(c.persona) }))
    );
  }

  eliminar(id: string): Observable<void> {
    return this.del<void>(`/api/clientes/${id}`);
  }

  // Cuentas bancarias del cliente (CRUD)
  cuentas(id: string): Observable<CuentaCliente[]> {
    return this.get<CuentaCliente[]>(`/api/clientes/${id}/cuentas`);
  }

  crearCuenta(id: string, body: Partial<CuentaCliente>): Observable<CuentaCliente> {
    return this.post<CuentaCliente>(`/api/clientes/${id}/cuentas`, body);
  }

  actualizarCuenta(id: string, cuentaId: number, body: Partial<CuentaCliente>): Observable<CuentaCliente> {
    return this.put<CuentaCliente>(`/api/clientes/${id}/cuentas/${cuentaId}`, body);
  }

  eliminarCuenta(id: string, cuentaId: number): Observable<void> {
    return this.del<void>(`/api/clientes/${id}/cuentas/${cuentaId}`);
  }

  /** Normaliza el payload al contrato de la API (persona numérica 1/2). */
  private enviar(body: Partial<Cliente>): Record<string, unknown> {
    return { ...(body as Record<string, unknown>), persona: aNumero((body as Record<string, unknown>)['persona']) };
  }
}
