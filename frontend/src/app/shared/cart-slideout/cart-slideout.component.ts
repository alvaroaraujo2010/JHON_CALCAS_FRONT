import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart-slideout',
  standalone: true,
  imports: [RouterLink],
  template: `
    <button class="jc-cart-btn" (click)="open()" aria-label="Abrir carrito">
      <svg viewBox="0 0 18 19" aria-hidden="true" width="20" height="20">
        <path fill="currentColor" d="M0 1.5A.5.5 0 01.5 1H2a.5.5 0 01.485.379L2.89 3H14.5a.5.5 0 01.485.632l-1.5 6A.5.5 0 0113 10H4.5a.5.5 0 01-.485-.379L2.11 2H.5A.5.5 0 010 1.5zm3 8a1.5 1.5 0 100 3 1.5 1.5 0 000-3zm8 0a1.5 1.5 0 100 3 1.5 1.5 0 000-3z"/>
      </svg>
      @if (cart.count()) {
        <span class="jc-cart-badge">{{ cart.count() }}</span>
      }
    </button>

    @if (isOpen()) {
      <div class="jc-cart-overlay" (click)="close()"></div>
      <aside class="jc-cart-drawer" role="dialog" aria-label="Carrito de compras">
        <div class="jc-cart-drawer__header">
          <h2>Carrito ({{ cart.count() }})</h2>
          <button (click)="close()">&times;</button>
        </div>
        <div class="jc-cart-drawer__body">
          @for (item of cart.items(); track item.product.id) {
            <div class="jc-cart-item">
              <div class="jc-cart-item__image">
                @if (item.product.imageUrl) {
                  <img [src]="item.product.imageUrl" [alt]="item.product.title" />
                }
              </div>
              <div class="jc-cart-item__info">
                <p class="jc-cart-item__title">{{ item.product.title }}</p>
                <p class="jc-cart-item__meta">
                  {{ item.product.brand }}
                  @if (item.product.model) { <span>/ {{ item.product.model }}</span> }
                  @if (item.product.color) { <span>/ {{ item.product.color }}</span> }
                </p>
                <div class="jc-cart-item__price-row">
                  <span>Precio unitario</span>
                  <strong>\${{ item.product.price.toLocaleString('es-CO') }}</strong>
                </div>
                <div class="jc-cart-item__actions">
                  <div class="jc-cart-item__qty" aria-label="Cantidad">
                    <button type="button" (click)="cart.updateQuantity(item.product.id, item.quantity - 1)">-</button>
                    <span>{{ item.quantity }}</span>
                    <button type="button" (click)="cart.updateQuantity(item.product.id, item.quantity + 1)">+</button>
                  </div>
                  <div class="jc-cart-item__subtotal">
                    <span>Subtotal</span>
                    <strong>\${{ (item.product.price * item.quantity).toLocaleString('es-CO') }}</strong>
                  </div>
                </div>
              </div>
              <button class="jc-cart-item__remove" type="button" (click)="cart.remove(item.product.id)" aria-label="Eliminar">&times;</button>
            </div>
          } @empty {
            <p class="jc-cart-empty">El carrito está vacío</p>
          }
        </div>
        @if (cart.count()) {
          <div class="jc-cart-drawer__footer">
            <div class="jc-cart-total">
              <span>Total</span>
              <strong>\${{ cart.total().toLocaleString('es-CO') }}</strong>
            </div>
            <a routerLink="/checkout" class="jc-btn jc-btn--primary jc-cart-checkout" (click)="close()">Pagar</a>
          </div>
        }
      </aside>
    }
  `,
  styles: [`
    .jc-cart-btn { position: relative; background: none; border: none; cursor: pointer; color: var(--jc-icon, inherit); padding: 0.5rem; }
    .jc-cart-badge { position: absolute; top: 0; right: 0; background: #e53935; color: #fff; font-size: 0.65rem; width: 18px; height: 18px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 700; }
    .jc-cart-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 999; }
    .jc-cart-drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 430px; max-width: 100vw; background: #fff; z-index: 1000; display: flex; flex-direction: column; box-shadow: -2px 0 12px rgba(0,0,0,0.15); color: #121212; }
    .jc-cart-drawer__header { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.25rem; border-bottom: 1px solid #eee; }
    .jc-cart-drawer__header h2 { margin: 0; font-size: 1.1rem; }
    .jc-cart-drawer__header button { background: none; border: none; font-size: 1.5rem; cursor: pointer; padding: 0 0.25rem; }
    .jc-cart-drawer__body { flex: 1; overflow-y: auto; padding: 1rem; }
    .jc-cart-empty { text-align: center; color: #999; padding: 2rem; }
    .jc-cart-item { position: relative; display: grid; grid-template-columns: 76px minmax(0, 1fr); gap: 0.9rem; padding: 1rem 1.75rem 1rem 0; border-bottom: 1px solid #f0f0f0; }
    .jc-cart-item__image { width: 76px; height: 76px; border: 1px solid #eee; border-radius: 8px; overflow: hidden; flex-shrink: 0; background: #fafafa; }
    .jc-cart-item__image img { width: 100%; height: 100%; object-fit: contain; }
    .jc-cart-item__info { flex: 1; min-width: 0; }
    .jc-cart-item__title { color: #121212; font-size: 0.92rem; font-weight: 700; line-height: 1.25; margin: 0 0 0.25rem; white-space: normal; }
    .jc-cart-item__meta { color: #6b7280; font-size: 0.76rem; margin: 0 0 0.65rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .jc-cart-item__price-row, .jc-cart-item__subtotal { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; color: #6b7280; font-size: 0.78rem; }
    .jc-cart-item__price-row strong, .jc-cart-item__subtotal strong { color: #121212; font-size: 0.92rem; }
    .jc-cart-item__actions { display: grid; grid-template-columns: auto minmax(0, 1fr); align-items: center; gap: 0.75rem; margin-top: 0.75rem; }
    .jc-cart-item__qty { display: inline-flex; align-items: center; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }
    .jc-cart-item__qty button { width: 30px; height: 30px; border: none; background: #fff; cursor: pointer; color: #121212; font-size: 1rem; line-height: 1; }
    .jc-cart-item__qty span { min-width: 2rem; text-align: center; color: #121212; font-size: 0.9rem; font-weight: 600; }
    .jc-cart-item__remove { position: absolute; top: 0.8rem; right: 0; background: none; border: none; font-size: 1.25rem; color: #777; cursor: pointer; padding: 0.25rem; }
    .jc-cart-drawer__footer { padding: 1rem 1.25rem; border-top: 1px solid #eee; }
    .jc-cart-total { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; font-size: 1rem; }
    .jc-cart-total strong { font-size: 1.2rem; }
    .jc-cart-checkout { display: flex; align-items: center; justify-content: center; width: 100%; min-height: 48px; border-radius: 10px; background: #de0404; color: #fff; font-size: 0.95rem; font-weight: 800; letter-spacing: 0.04em; text-transform: uppercase; text-decoration: none; box-shadow: 0 10px 24px rgba(222, 4, 4, 0.28); }
    .jc-cart-checkout:hover { background: #c40303; color: #fff; transform: translateY(-1px); }
  `]
})
export class CartSlideoutComponent {
  cart = inject(CartService);
  readonly isOpen = signal(false);

  open() { this.isOpen.set(true); }
  close() { this.isOpen.set(false); }
}
