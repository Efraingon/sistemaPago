import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ToastService } from '../shared/toast/toast.service';

export interface QueryParams {
  [clave: string]: string | number | boolean | null | undefined;
}

/** Cliente HTTP base: centraliza URL, params, errores y toasts.
 *  Todas las reglas de contrato viven en core/services/* — si la API real
 *  difiere, basta con ajustar aquí. */
@Injectable()
export class ApiService {
  protected readonly base = environment.apiUrl;
  protected readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  protected get<T>(path: string, params?: QueryParams): Observable<T> {
    return this.http
      .get<T>(`${this.base}${path}`, { params: this.buildParams(params) })
      .pipe(catchError((e) => this.error(e)));
  }

  protected post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(`${this.base}${path}`, body).pipe(catchError((e) => this.error(e)));
  }

  protected put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(`${this.base}${path}`, body).pipe(catchError((e) => this.error(e)));
  }

  protected del<T>(path: string, body?: unknown): Observable<T> {
    return this.http
      .delete<T>(`${this.base}${path}`, body !== undefined ? { body } : undefined)
      .pipe(catchError((e) => this.error(e)));
  }

  /** Construye HttpParams omitiendo vacíos/undefined/null. */
  protected buildParams(params?: QueryParams): HttpParams {
    let out = new HttpParams();
    if (!params) return out;
    for (const clave of Object.keys(params)) {
      const valor = params[clave];
      if (valor !== null && valor !== undefined && valor !== '') {
        out = out.set(clave, String(valor));
      }
    }
    return out;
  }

  private error(err: any): Observable<never> {
    let msg: string | null = null;
    const body = err?.error;
    if (typeof body === 'string' && body.trim()) msg = body;
    else if (body && typeof body === 'object') {
      msg = body.mensaje || body.message || body.title || body.error || null;
      if (!msg && Array.isArray(body.errores)) msg = body.errores.join(', ');
    }
    if (!msg) {
      if (err?.status === 0) msg = 'No hay conexión con el servidor.';
      else if (err?.status === 404) msg = 'Recurso no encontrado.';
      else if (err?.status === 403) msg = 'No tiene permisos para esta operación.';
      else msg = `Error del servidor${err?.status ? ` (${err.status})` : ''}.`;
    }
    this.toast.error(msg);
    return throwError(() => err);
  }
}
