import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, QueryParams } from '../api.service';
import { AuditoriaItem, Configuracion, Permiso, Rol, Usuario } from '../models';

@Injectable({ providedIn: 'root' })
export class UsuarioService extends ApiService {
  listar(params?: QueryParams): Observable<Usuario[]> {
    return this.get<Usuario[]>('/api/usuarios', params);
  }

  crear(body: Partial<Usuario>): Observable<Usuario> {
    return this.post<Usuario>('/api/usuarios', body);
  }

  actualizar(id: number, body: Partial<Usuario>): Observable<Usuario> {
    return this.put<Usuario>(`/api/usuarios/${id}`, body);
  }

  asignarRoles(id: number, roleIds: number[]): Observable<Usuario> {
    return this.put<Usuario>(`/api/usuarios/${id}/roles`, roleIds);
  }

  roles(): Observable<Rol[]> {
    return this.get<Rol[]>('/api/roles');
  }
}

@Injectable({ providedIn: 'root' })
export class ConfiguracionService extends ApiService {
  obtener(): Observable<Configuracion> {
    return this.get<Configuracion>('/api/configuracion');
  }

  guardar(body: Configuracion): Observable<Configuracion> {
    return this.put<Configuracion>('/api/configuracion', body);
  }

  auditoria(params?: QueryParams): Observable<AuditoriaItem[]> {
    return this.get<AuditoriaItem[]>('/api/auditoria', params);
  }
}
