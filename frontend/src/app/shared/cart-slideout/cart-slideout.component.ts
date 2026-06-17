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
                <p class="jc-cart-item__price">\${{ item.product.price.toLocaleString('es-CO') }}</p>
                <div class="jc-cart-item__qty">
                  <button (click)="cart.updateQuantity(item.product.id, item.quantity - 1)">-</button>
                  <span>{{ item.quantity }}</span>
                  <button (click)="cart.updateQuantity(item.product.id, item.quantity + 1)">+</button>
                </div>
              </div>
              <button class="jc-cart-item__remove" (click)="cart.remove(item.product.id)" aria-label="Eliminar">&times;</button>
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
    .jc-cart-btn { position: relative; background: none; border: none; cursor: pointer; color: inherit; padding: 0.5rem; }
    .jc-cart-badge { position: absolute; top: 0; right: 0; background: #e53935; color: #fff; font-size: 0.65rem; width: 18px; height: 18px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: 700; }
    .jc-cart-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 999; }
    .jc-cart-drawer { position: fixed; top: 0; right: 0; bottom: 0; width: 380px; max-width: 100vw; background: #fff; z-index: 1000; display: flex; flex-direction: column; box-shadow: -2px 0 12px rgba(0,0,0,0.15); }
    .jc-cart-drawer__header { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.25rem; border-bottom: 1px solid #eee; }
    .jc-cart-drawer__header h2 { margin: 0; font-size: 1.1rem; }
    .jc-cart-drawer__header button { background: none; border: none; font-size: 1.5rem; cursor: pointer; padding: 0 0.25rem; }
    .jc-cart-drawer__body { flex: 1; overflow-y: auto; padding: 1rem; }
    .jc-cart-empty { text-align: center; color: #999; padding: 2rem; }
    .jc-cart-item { display: flex; gap: 0.75rem; padding: 0.75rem 0; border-bottom: 1px solid #f5f5f5; }
    .jc-cart-item__image { width: 60px; height: 60px; border-radius: 4px; overflow: hidden; flex-shrink: 0; }
    .jc-cart-item__image img { width: 100%; height: 100%; object-fit: cover; }
    .jc-cart-item__info { flex: 1; min-width: 0; }
    .jc-cart-item__title { font-size: 0.85rem; margin: 0 0 0.25rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .jc-cart-item__price { font-size: 0.9rem; font-weight: 600; margin: 0 0 0.5rem; }
    .jc-cart-item__qty { display: flex; align-items: center; gap: 0.5rem; }
    .jc-cart-item__qty button { width: 26px; height: 26px; border: 1px solid #ddd; border-radius: 4px; background: #fff; cursor: pointer; font-size: 1rem; line-height: 1; }
    .jc-cart-item__qty span { min-width: 1.5rem; text-align: center; font-size: 0.9rem; }
    .jc-cart-item__remove { background: none; border: none; font-size: 1.25rem; color: #999; cursor: pointer; padding: 0.25rem; align-self: flex-start; }
    .jc-cart-drawer__footer { padding: 1rem 1.25rem; border-top: 1px solid #eee; }
    .jc-cart-total { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; font-size: 1rem; }
    .jc-cart-total strong { font-size: 1.2rem; }
    .jc-cart-checkout { display: block; text-align: center; width: 100%; }
  `]
})
export class CartSlideoutComponent {
  cart = inject(CartService);
  readonly isOpen = signal(false);

  open() { this.isOpen.set(true); }
  close() { this.isOpen.set(false); }
}
