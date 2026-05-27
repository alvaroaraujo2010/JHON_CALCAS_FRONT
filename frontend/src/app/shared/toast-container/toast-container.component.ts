import { Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  template: `
    <div class="toast-stack" aria-live="polite" aria-atomic="true">
      @for (t of toast.toasts(); track t.id) {
        <div class="toast toast--{{ t.type }}" role="status">
          <span class="toast__msg">{{ t.message }}</span>
          <button type="button" class="toast__close" (click)="toast.dismiss(t.id)" aria-label="Cerrar">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .toast-stack {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 10000;
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      max-width: min(420px, calc(100vw - 2rem));
      pointer-events: none;
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.85rem 1rem;
      border-radius: 12px;
      box-shadow: 0 12px 32px rgba(15, 23, 42, 0.2);
      font-size: 0.9rem;
      font-weight: 500;
      line-height: 1.4;
      pointer-events: auto;
      animation: toast-in 0.25s ease-out;
    }

    @keyframes toast-in {
      from {
        opacity: 0;
        transform: translateX(1rem);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    .toast--success {
      background: #ecfdf5;
      color: #065f46;
      border: 1px solid #6ee7b7;
    }

    .toast--error {
      background: #fef2f2;
      color: #991b1b;
      border: 1px solid #fecaca;
    }

    .toast--info {
      background: #eff6ff;
      color: #1e40af;
      border: 1px solid #bfdbfe;
    }

    .toast__msg {
      flex: 1;
    }

    .toast__close {
      flex-shrink: 0;
      border: none;
      background: transparent;
      font-size: 1.25rem;
      line-height: 1;
      cursor: pointer;
      opacity: 0.65;
      padding: 0;
      color: inherit;

      &:hover {
        opacity: 1;
      }
    }
  `
})
export class ToastContainerComponent {
  readonly toast = inject(ToastService);
}
