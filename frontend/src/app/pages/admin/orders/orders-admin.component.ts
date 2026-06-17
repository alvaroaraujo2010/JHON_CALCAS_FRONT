import { Component, computed, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { Order, OrderItemDetail } from '../../../core/models';
import { ToastService } from '../../../core/services/toast.service';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-orders-admin',
  standalone: true,
  imports: [DatePipe, ModuleHeaderComponent],
  template: `
    <app-module-header title="Pedidos" subtitle="Gestiona los pedidos del e-commerce" />

    <div class="jc-filter-bar">
      <select (change)="filterStatus($event)">
        <option value="">Todos</option>
        <option value="pending">Pendientes</option>
        <option value="paid">Pagados</option>
        <option value="completed">Completados</option>
        <option value="cancelled">Cancelados</option>
      </select>
      <input type="search" placeholder="Buscar por #, cliente o email..." (input)="searchTerm.set($any($event.target).value)"
             style="padding:0.4rem 0.75rem;border:1px solid #ccc;border-radius:4px;font-size:0.9rem;margin-left:0.5rem;flex:1;" />
    </div>

    @if (selectedOrder(); as o) {
      <div class="jc-order-detail">
        <div class="jc-order-detail__head">
          <h3>Pedido #{{ o.orderNumber }}</h3>
          <span class="jc-badge" [class.jc-badge--paid]="o.status === 'paid' || o.status === 'completed'"
                [class.jc-badge--pending]="o.status === 'pending'"
                [class.jc-badge--cancelled]="o.status === 'cancelled'">{{ o.status }}</span>
          <button class="jc-btn-sm" (click)="selectedOrder.set(null)" style="margin-left:auto;">Cerrar</button>
        </div>
        <div class="jc-order-detail__body">
          <p><strong>Cliente:</strong> {{ o.customerName }}</p>
          <p><strong>Email:</strong> {{ o.customerEmail }}</p>
          <p><strong>Teléfono:</strong> {{ o.customerPhone }}</p>
          <p><strong>Dirección:</strong> {{ o.customerAddress }}{{ o.city ? ', ' + o.city : '' }}</p>
          @if (o.notes) { <p><strong>Notas:</strong> {{ o.notes }}</p> }
          <p><strong>Fecha:</strong> {{ o.createdAt | date:'short' }}</p>
          @if (o.mpPaymentId) { <p><strong>Pago MP:</strong> {{ o.mpPaymentId }} ({{ o.mpPaymentStatus }})</p> }

          <h4>Productos</h4>
          <table class="jc-table">
            <thead><tr><th>Producto</th><th>Cant.</th><th>Precio</th><th>Subtotal</th></tr></thead>
            <tbody>
              @for (item of o.items; track $index) {
                <tr>
                  <td>{{ item.productTitle }}</td>
                  <td>{{ item.quantity }}</td>
                  <td>\${{ item.unitPrice.toLocaleString('es-CO') }}</td>
                  <td>\${{ item.lineTotal.toLocaleString('es-CO') }}</td>
                </tr>
              }
            </tbody>
          </table>

          <div class="jc-order-detail__total">
            <strong>Total: \${{ o.total.toLocaleString('es-CO') }}</strong>
          </div>

          @if (o.stockDeductedAt) {
            <p><strong>Inventario:</strong> descontado el {{ o.stockDeductedAt | date:'short' }}</p>
          }

          @if (o.status === 'pending' || o.status === 'pending_error' || o.status === 'processing') {
            <button class="jc-btn jc-btn--primary" (click)="updateStatus(o.id, 'paid')">Marcar como pagado</button>
          }
          @if (o.status === 'paid') {
            <button class="jc-btn jc-btn--primary" (click)="updateStatus(o.id, 'completed')">Marcar completado</button>
          }
          @if (o.status === 'pending' || o.status === 'paid') {
            <button class="jc-btn jc-btn--danger" (click)="updateStatus(o.id, 'cancelled')" style="margin-left:0.5rem;">Cancelar</button>
          }
        </div>
      </div>
    }

    <table class="jc-table">
      <thead>
        <tr><th>#</th><th>Cliente</th><th>Email</th><th>Total</th><th>Estado</th><th>Fecha</th><th></th></tr>
      </thead>
      <tbody>
        @for (o of filteredOrders(); track o.id) {
          <tr>
            <td>{{ o.orderNumber }}</td>
            <td>{{ o.customerName }}</td>
            <td>{{ o.customerEmail }}</td>
            <td>\${{ o.total.toLocaleString('es-CO') }}</td>
            <td><span class="jc-badge" [class.jc-badge--paid]="o.status === 'paid' || o.status === 'completed'"
                  [class.jc-badge--pending]="o.status === 'pending'"
                  [class.jc-badge--cancelled]="o.status === 'cancelled'">{{ o.status }}</span></td>
            <td>{{ o.createdAt | date:'short' }}</td>
            <td><button class="jc-btn-sm" (click)="selectedOrder.set(o)">Ver</button></td>
          </tr>
        } @empty {
          <tr><td colspan="7" style="text-align:center;color:#999;">No hay pedidos</td></tr>
        }
      </tbody>
    </table>
  `,
  styles: [`
    .jc-filter-bar { margin-bottom: 1rem; display: flex; gap: 0.5rem; }
    .jc-filter-bar select { padding: 0.4rem 0.75rem; border: 1px solid #ccc; border-radius: 4px; }
    .jc-order-detail { background: #fff; border: 1px solid #eee; border-radius: 8px; padding: 1.5rem; margin-bottom: 1.5rem; }
    .jc-order-detail__head { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1rem; }
    .jc-order-detail__head h3 { margin: 0; }
    .jc-order-detail__body p { margin: 0.3rem 0; font-size: 0.9rem; }
    .jc-order-detail__body h4 { margin: 1rem 0 0.5rem; }
    .jc-order-detail__total { text-align: right; padding: 1rem 0; font-size: 1.1rem; }
    .jc-badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 12px; font-size: 0.75rem; font-weight: 600; background: #eee; }
    .jc-badge--paid { background: #c8e6c9; color: #2e7d32; }
    .jc-badge--pending { background: #fff3e0; color: #e65100; }
    .jc-badge--cancelled { background: #ffcdd2; color: #c62828; }
    .jc-btn--danger { background: #d32f2f; color: #fff; border-color: #d32f2f; }
  `]
})
export class OrdersAdminComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);

  readonly allOrders = signal<Order[]>([]);
  readonly selectedOrder = signal<Order | null>(null);
  readonly statusFilter = signal('');
  readonly searchTerm = signal('');
  readonly filteredOrders = computed(() => {
    let list = this.allOrders();
    const sf = this.statusFilter();
    if (sf) list = list.filter(o => o.status === sf);
    const t = this.searchTerm().toLowerCase();
    if (t) list = list.filter(o => o.orderNumber?.toLowerCase().includes(t) || o.customerName?.toLowerCase().includes(t) || o.customerEmail?.toLowerCase().includes(t));
    return list;
  });

  ngOnInit() {
    this.loadOrders();
  }

  private loadOrders() {
    this.api.get<Order[]>('orders').subscribe({
      next: (data) => {
        this.allOrders.set(data);
      },
      error: () => this.toast.error('No se pudieron cargar los pedidos')
    });
  }

  filterStatus(event: Event) {
    this.statusFilter.set((event.target as HTMLSelectElement).value);
  }

  updateStatus(orderId: number, status: string) {
    this.api.put<Order | { order: Order; stockWarnings?: string[]; stockDeducted?: string[]; stockProcessed?: boolean }>(`orders/${orderId}/status`, { status }).subscribe({
      next: (res) => {
        const payload = 'order' in res ? res : null;
        const order = payload?.order ?? (res as Order);
        const warnings = payload?.stockWarnings ?? [];
        const deducted = payload?.stockDeducted ?? [];

        if (warnings.length) {
          this.toast.error(warnings.join(' · '));
        } else if (deducted.length) {
          this.toast.success(`Estado actualizado. Stock descontado: ${deducted.join(', ')}`);
        } else {
          this.toast.success('Estado actualizado');
        }

        if (order) this.selectedOrder.set(order);
        this.loadOrders();
      },
      error: () => this.toast.error('Error al actualizar estado')
    });
  }
}
