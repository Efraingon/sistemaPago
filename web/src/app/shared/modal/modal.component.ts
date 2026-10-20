import { Component, ElementRef, EventEmitter, HostListener, Input, Output } from '@angular/core';

/** Modal accesible (backdrop, ESC, cierre con X). Uso:
 *  <app-modal [visible]="abierto" title="Nuevo" (closed)="abierto=false"> ... </app-modal> */
@Component({
  selector: 'app-modal',
  standalone: true,
  template: `
    @if (visible) {
      <div class="modal-overlay" (mousedown)="siBackdrop($event)">
        <div
          class="modal"
          [class.modal-lg]="ancho === 'lg'"
          [class.modal-xl]="ancho === 'xl'"
          role="dialog"
          aria-modal="true"
          [attr.aria-label]="title || 'Diálogo'"
        >
          <div class="modal-header">
            <h3>{{ title }}</h3>
            <button type="button" class="icon-btn" (click)="cerrar()" aria-label="Cerrar">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12" /></svg>
            </button>
          </div>
          <div class="modal-body"><ng-content /></div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .modal-overlay {
        position: fixed;
        inset: 0;
        background: rgba(4, 8, 18, 0.72);
        backdrop-filter: blur(4px);
        z-index: 900;
        display: flex;
        align-items: flex-start;
        justify-content: center;
        padding: 48px 16px;
        overflow-y: auto;
        animation: overlay-in 180ms ease;
      }
      .modal {
        background: var(--color-card);
        border: 1px solid var(--color-border);
        border-radius: 14px;
        width: min(560px, 100%);
        box-shadow: var(--shadow-xl);
        animation: modal-in 200ms ease;
      }
      .modal-lg {
        width: min(840px, 100%);
      }
      .modal-xl {
        width: min(1080px, 100%);
      }
      .modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        padding: 18px 22px;
        border-bottom: 1px solid var(--color-border);
      }
      .modal-header h3 {
        margin: 0;
        font-size: 17px;
        font-weight: 600;
        letter-spacing: -0.01em;
        color: var(--color-foreground);
      }
      .modal-body {
        padding: 22px;
        max-height: calc(100vh - 160px);
        overflow-y: auto;
      }
      .icon-btn {
        background: transparent;
        border: 1px solid transparent;
        border-radius: 8px;
        color: var(--color-muted-foreground);
        cursor: pointer;
        padding: 6px;
        display: inline-flex;
        transition: color 200ms ease, background 200ms ease, border-color 200ms ease;
      }
      .icon-btn:hover {
        color: var(--color-foreground);
        background: var(--color-muted);
        border-color: var(--color-border);
      }
      .icon-btn:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
      @keyframes overlay-in {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }
      @keyframes modal-in {
        from {
          opacity: 0;
          transform: translateY(10px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }
      @media (prefers-reduced-motion: reduce) {
        .modal-overlay,
        .modal {
          animation: none;
        }
      }
    `,
  ],
})
export class ModalComponent {
  @Input() visible = false;
  @Input() title = '';
  /** 'md' (default) | 'lg' | 'xl' */
  @Input() ancho: 'md' | 'lg' | 'xl' = 'md';
  @Output() closed = new EventEmitter<void>();

  constructor(private host: ElementRef) {}

  cerrar(): void {
    this.closed.emit();
  }

  siBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.cerrar();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.visible) this.cerrar();
  }
}
