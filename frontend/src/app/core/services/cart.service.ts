import { Injectable, signal, computed, effect } from '@angular/core';
import { CartItem, CatalogProduct } from '../models';

const STORAGE_KEY = 'contanexo_cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  readonly items = signal<CartItem[]>(this.load());

  readonly count = computed(() => this.items().reduce((s, i) => s + i.quantity, 0));
  readonly total = computed(() => this.items().reduce((s, i) => s + i.product.price * i.quantity, 0));

  constructor() {
    effect(() => {
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(this.items())); }
      catch { /* quota exceeded */ }
    });
  }

  add(product: CatalogProduct, quantity = 1) {
    this.items.update(list => {
      const existing = list.find(i => i.product.id === product.id);
      if (existing) {
        existing.quantity += quantity;
      } else {
        list = [...list, { product, quantity }];
      }
      return list;
    });
  }

  remove(productId: number) {
    this.items.update(list => list.filter(i => i.product.id !== productId));
  }

  updateQuantity(productId: number, quantity: number) {
    if (quantity <= 0) { this.remove(productId); return; }
    this.items.update(list =>
      list.map(i => i.product.id === productId ? { ...i, quantity } : i)
    );
  }

  clear() {
    this.items.set([]);
  }

  private load(): CartItem[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }
}
