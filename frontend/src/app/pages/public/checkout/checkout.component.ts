import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CartService } from '../../../core/services/cart.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';

interface OrderResponse {
  id: number;
  orderNumber: string;
  customerName: string;
  status: string;
  mpPreferenceId?: string;
  mpInitPoint?: string;
  total: number;
  createdAt: string;
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <div class="jc-checkout page-width">
      <h1>Checkout</h1>

      @if (cart.items().length === 0 && !orderDone()) {
        <div class="jc-checkout__empty">
          <p>Tu carrito está vacío</p>
          <a routerLink="/" class="jc-btn">Ir al catálogo</a>
        </div>
      }

      @if (!orderDone() && cart.items().length > 0) {
        <div class="jc-checkout__grid">
          <div class="jc-checkout__form">
            <h2>Datos de envío</h2>
            <form [formGroup]="form" (ngSubmit)="submitOrder()">
              <div class="jc-field">
                <label>Nombre completo *</label>
                <input type="text" formControlName="customerName" required />
              </div>
              <div class="jc-field">
                <label>Email *</label>
                <input type="email" formControlName="customerEmail" required />
              </div>
              <div class="jc-field">
                <label>Teléfono *</label>
                <input type="tel" formControlName="customerPhone" required />
              </div>
              <div class="jc-field">
                <label>Dirección *</label>
                <input type="text" formControlName="customerAddress" required />
              </div>
              <div class="jc-field">
                <label>Ciudad</label>
                <input type="text" formControlName="city" />
              </div>
              <div class="jc-field">
                <label>Notas del pedido</label>
                <textarea formControlName="notes" rows="3"></textarea>
              </div>

              <button type="submit" class="jc-btn jc-btn--primary jc-btn--lg" [disabled]="submitting()">
                @if (submitting()) {
                  Procesando...
                } @else {
                  Ir a pagar — \${{ cart.total().toLocaleString('es-CO') }}
                }
              </button>
            </form>
          </div>

          <div class="jc-checkout__summary">
            <h2>Resumen del pedido</h2>
            @for (item of cart.items(); track item.product.id) {
              <div class="jc-checkout__item">
                <span class="jc-checkout__item-qty">{{ item.quantity }}x</span>
                <span class="jc-checkout__item-title">{{ item.product.title }}</span>
                <span class="jc-checkout__item-price">\${{ (item.product.price * item.quantity).toLocaleString('es-CO') }}</span>
              </div>
            }
            <div class="jc-checkout__total">
              <strong>Total</strong>
              <strong>\${{ cart.total().toLocaleString('es-CO') }}</strong>
            </div>
          </div>
        </div>
      }

      @if (orderDone()) {
        <div class="jc-checkout__done">
          <h2>¡Pedido creado con éxito!</h2>
          <p>Serás redirigido a Mercado Pago para completar el pago.</p>
          <p class="jc-checkout__num">Pedido #{{ orderNumber() }}</p>
          <p class="jc-checkout__alt">
            <a [href]="mpInitPoint()" target="_blank">Haz clic aquí para pagar</a>
          </p>
        </div>
      }
    </div>
  `,
  styles: [`
    .jc-checkout { padding: 2rem 1.5rem 4rem; max-width: 900px; margin: 0 auto; }
    .jc-checkout h1 { font-size: 1.75rem; margin-bottom: 2rem; }
    .jc-checkout__empty { text-align: center; padding: 4rem 0; }
    .jc-checkout__grid { display: grid; grid-template-columns: 1fr 320px; gap: 2rem; }
    @media (max-width: 768px) { .jc-checkout__grid { grid-template-columns: 1fr; } }
    .jc-checkout__form h2, .jc-checkout__summary h2 { font-size: 1.1rem; margin-bottom: 1rem; }
    .jc-field { margin-bottom: 1rem; }
    .jc-field label { display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.3rem; }
    .jc-field input, .jc-field textarea { width: 100%; padding: 0.6rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.95rem; }
    .jc-field textarea { resize: vertical; }
    .jc-btn--lg { width: 100%; padding: 0.8rem; font-size: 1.05rem; margin-top: 0.5rem; }
    .jc-checkout__summary { background: #f9f9f9; padding: 1.25rem; border-radius: 8px; align-self: start; position: sticky; top: 1rem; }
    .jc-checkout__item { display: flex; gap: 0.5rem; padding: 0.5rem 0; border-bottom: 1px solid #eee; font-size: 0.9rem; }
    .jc-checkout__item-qty { color: #666; min-width: 2rem; }
    .jc-checkout__item-title { flex: 1; }
    .jc-checkout__item-price { font-weight: 500; white-space: nowrap; }
    .jc-checkout__total { display: flex; justify-content: space-between; padding: 1rem 0 0; font-size: 1.1rem; }
    .jc-checkout__done { text-align: center; padding: 3rem 0; }
    .jc-checkout__num { font-size: 1.3rem; font-weight: 700; margin: 1rem 0; }
    .jc-checkout__alt { font-size: 0.85rem; color: #666; margin-top: 1rem; }
  `]
})
export class CheckoutComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private toast = inject(ToastService);
  cart = inject(CartService);

  readonly submitting = signal(false);
  readonly orderDone = signal(false);
  readonly orderNumber = signal('');
  readonly mpInitPoint = signal('');

  readonly form = this.fb.group({
    customerName: ['', Validators.required],
    customerEmail: ['', [Validators.required, Validators.email]],
    customerPhone: ['', Validators.required],
    customerAddress: ['', Validators.required],
    city: [''],
    notes: ['']
  });

  submitOrder() {
    if (this.form.invalid) {
      this.toast.error('Completa todos los campos obligatorios');
      return;
    }

    this.submitting.set(true);
    const fv = this.form.value;
    const body = {
      customerName: fv.customerName,
      customerEmail: fv.customerEmail,
      customerPhone: fv.customerPhone,
      customerAddress: fv.customerAddress,
      city: fv.city || '',
      notes: fv.notes || '',
      items: this.cart.items().map(i => ({
        catalogProductId: i.product.id,
        quantity: i.quantity
      }))
    };

    this.api.postPublic<OrderResponse>('orders', body).subscribe({
      next: (res) => {
        this.orderNumber.set(res.orderNumber);
        this.mpInitPoint.set(res.mpInitPoint ?? '');
        this.orderDone.set(true);
        this.cart.clear();
        if (res.mpInitPoint) {
          window.location.href = res.mpInitPoint;
        }
      },
      error: () => {
        this.toast.error('Error al crear el pedido. Intenta de nuevo.');
        this.submitting.set(false);
      }
    });
  }
}
