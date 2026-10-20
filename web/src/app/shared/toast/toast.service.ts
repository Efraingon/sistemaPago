import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type TipoToast = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  tipo: TipoToast;
  mensaje: string;
}

/** Toasts propios (sin librerías externas). */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private readonly subject = new BehaviorSubject<Toast[]>([]);
  readonly toasts$ = this.subject.asObservable();

  success(mensaje: string): void {
    this.push('success', mensaje);
  }

  error(mensaje: string): void {
    this.push('error', mensaje);
  }

  info(mensaje: string): void {
    this.push('info', mensaje);
  }

  cerrar(id: number): void {
    this.subject.next(this.subject.value.filter((t) => t.id !== id));
  }

  private push(tipo: TipoToast, mensaje: string): void {
    const toast: Toast = { id: ++this.seq, tipo, mensaje };
    this.subject.next([...this.subject.value, toast]);
    setTimeout(() => this.cerrar(toast.id), 5000);
  }
}
