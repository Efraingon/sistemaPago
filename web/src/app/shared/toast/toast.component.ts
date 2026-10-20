import { Component } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Toast, ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [AsyncPipe],
  template: `
    @if (toasts.toasts$ | async; as lista) {
      <div class="toast-host" role="status" aria-live="polite">
        @for (t of lista; track t.id) {
          <div class="toast toast-{{ t.tipo }}">
            <span class="toast-ico" aria-hidden="true">
              @if (t.tipo === 'success') {
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
              } @else if (t.tipo === 'error') {
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18M6 6l12 12" /></svg>
              } @else {
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16v-4M12 8h.01M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z" /></svg>
              }
            </span>
            <span class="toast-msg">{{ t.mensaje }}</span>
            <button type="button" class="toast-x" (click)="toasts.cerrar(t.id)" aria-label="Cerrar notificación">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M18 6 6 18M6 6l12 12" /></svg>
            </button>
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      .toast-host {
        position: fixed;
        right: 20px;
        bottom: 20px;
        z-index: 2000;
        display: flex;
        flex-direction: column;
        gap: 10px;
        max-width: min(380px, calc(100vw - 40px));
      }
      .toast {
        display: flex;
        align-items: flex-start;
        gap: 10px;
        background: var(--color-card);
        border: 1px solid var(--color-border);
        border-left: 3px solid var(--color-primary);
        border-radius: 10px;
        padding: 12px 14px;
        box-shadow: var(--shadow-lg);
        animation: toast-in 220ms ease;
        cursor: pointer;
      }
      .toast-success {
        border-left-color: #22c55e;
      }
      .toast-error {
        border-left-color: var(--color-destructive);
      }
      .toast-info {
        border-left-color: var(--color-accent);
      }
      .toast-ico {
        color: var(--color-primary);
        display: inline-flex;
        margin-top: 2px;
      }
      .toast-success .toast-ico {
        color: #22c55e;
      }
      .toast-error .toast-ico {
        color: var(--color-destructive);
      }
      .toast-msg {
        flex: 1;
        font-size: 13.5px;
        line-height: 1.45;
        color: var(--color-foreground);
      }
      .toast-x {
        background: transparent;
        border: 0;
        color: var(--color-muted-foreground);
        cursor: pointer;
        padding: 2px;
        border-radius: 6px;
        display: inline-flex;
        transition: color 200ms ease;
      }
      .toast-x:hover {
        color: var(--color-foreground);
      }
      .toast-x:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
      @keyframes toast-in {
        from {
          opacity: 0;
          transform: translateY(8px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
      @media (prefers-reduced-motion: reduce) {
        .toast {
          animation: none;
        }
      }
    `,
  ],
})
export class ToastComponent {
  constructor(public toasts: ToastService) {}
}
