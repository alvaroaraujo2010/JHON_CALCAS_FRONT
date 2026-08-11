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
    <div class="module-page">
      <app-module-header title="Pedidos" subtitle="Gestiona los pedidos del e-commerce" />

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

      <section class="data-card">
        <div class="data-card__head">
          <h2>Listado de pedidos</h2>
          <div class="data-card__actions">
            <span>{{ filteredOrders().length }} de {{ allOrders().length }} registro(s)</span>
            <button class="btn sm" type="button" (click)="loadOrders()">Recargar</button>
          </div>
        </div>
        @if (loadError()) {
          <div class="orders-error">{{ loadError() }}</div>
        }
        <div class="orders-toolbar">
          <select class="orders-toolbar__status" (change)="filterStatus($event)">
            <option value="">Todos</option>
            <option value="pending">Pendientes</option>
            <option value="paid">Pagados</option>
            <option value="completed">Completados</option>
            <option value="cancelled">Cancelados</option>
          </select>
          <input type="search" class="data-card__search" placeholder="Buscar por #, cliente o email..." (input)="searchTerm.set($any($event.target).value)" />
        </div>
        <table class="data">
          <thead>
            <tr><th>#</th><th>Cliente</th><th>Email</th><th>Total</th><th>Estado</th><th>Fecha</th><th></th></tr>
          </thead>
          <tbody>
            @for (o of filteredOrders(); track o.id) {
              <tr>
                <td><strong>{{ o.orderNumber }}</strong></td>
                <td>{{ o.customerName }}</td>
                <td>{{ o.customerEmail }}</td>
                <td>\${{ o.total.toLocaleString('es-CO') }}</td>
                <td><span class="jc-badge" [class.jc-badge--paid]="o.status === 'paid' || o.status === 'completed'"
                      [class.jc-badge--pending]="o.status === 'pending'"
                      [class.jc-badge--cancelled]="o.status === 'cancelled'">{{ o.status }}</span></td>
                <td>{{ o.createdAt | date:'short' }}</td>
                <td><button class="btn sm" type="button" (click)="selectedOrder.set(o)">Ver</button></td>
              </tr>
            } @empty {
              <tr><td colspan="7"><div class="empty-state"><strong>Sin pedidos</strong><p>{{ emptyMessage() }}</p></div></td></tr>
            }
          </tbody>
        </table>
      </section>
    </div>
  `,
  styles: [`
    .data-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 24px rgba(15, 23, 42, 0.07); }
    .data-card__head { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: 1rem 1.5rem; border-bottom: 1px solid #e2e8f0; background: #f8fafc; }
    .data-card__head h2 { margin: 0; color: #475569; font-size: 0.85rem; font-weight: 700; letter-spacing: 0.05em; text-transform: uppercase; }
    .data-card__head span { color: #64748b; font-size: 0.85rem; }
    .data-card__actions { display: flex; align-items: center; gap: 0.75rem; }
    .data-card__search { width: 100%; min-height: 42px; padding: 0.65rem 1rem; border: 1px solid #d7dce5; border-radius: 12px; color: #182033; background: #fff; font-family: inherit; font-size: 0.9rem; outline: none; }
    .data-card__search::placeholder { color: #94a3b8; }
    .data-card__search:focus { background: #f8fafc; border-color: #de0404; box-shadow: 0 0 0 3px rgba(222, 4, 4, 0.12); }
    .orders-toolbar { display: grid; grid-template-columns: 160px minmax(0, 1fr); gap: 0.75rem; margin-bottom: 1rem; }
    .orders-error { margin: 1rem 1.5rem 0; padding: 0.75rem 1rem; border: 1px solid #fecaca; border-radius: 10px; background: #fef2f2; color: #991b1b; font-size: 0.9rem; }
    .orders-toolbar__status { min-height: 42px; padding: 0.6rem 0.75rem; border: 1px solid #d7dce5; border-radius: 12px; background: #fff; color: #182033; font-family: inherit; font-size: 0.9rem; font-weight: 400; line-height: 1.4; outline: none; }
    .orders-toolbar__status:focus { background: #f8fafc; border-color: #de0404; box-shadow: 0 0 0 3px rgba(222, 4, 4, 0.12); }
    table.data { width: 100%; border-collapse: collapse; table-layout: auto; }
    table.data th, table.data td { padding: 0.85rem 1.25rem; border-bottom: 1px solid #f1f5f9; color: #0f172a; font-size: 0.9rem; text-align: left; vertical-align: middle; white-space: nowrap; }
    table.data th { background: #fff; color: #64748b; font-size: 0.78rem; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; }
    table.data tbody tr:hover { background: #fafafa; }
    table.data tbody tr:last-child td { border-bottom: none; }
    .empty-state { padding: 3rem 1.5rem; color: #64748b; text-align: center; }
    .empty-state strong { display: block; margin-bottom: 0.25rem; color: #334155; font-size: 1rem; }
    .empty-state p { margin: 0.35rem 0 0; font-size: 0.9rem; }
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
    @media (max-width: 860px) { table.data { display: block; overflow-x: auto; } }
    @media (max-width: 720px) { .orders-toolbar { grid-template-columns: 1fr; } }
  `]
})
export class OrdersAdminComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);

  readonly allOrders = signal<Order[]>([]);
  readonly selectedOrder = signal<Order | null>(null);
  readonly statusFilter = signal('');
  readonly searchTerm = signal('');
  readonly loadError = signal('');
  readonly filteredOrders = computed(() => {
    let list = this.allOrders();
    const sf = this.statusFilter();
    if (sf) list = list.filter(o => o.status === sf);
    const t = this.searchTerm().toLowerCase();
    if (t) list = list.filter(o => o.orderNumber?.toLowerCase().includes(t) || o.customerName?.toLowerCase().includes(t) || o.customerEmail?.toLowerCase().includes(t));
    return list;
  });
  readonly emptyMessage = computed(() => {
    if (this.loadError()) return 'No se pudo cargar la lista. Revise el mensaje superior.';
    if (this.allOrders().length > 0) return 'Hay pedidos cargados, pero los filtros actuales no tienen coincidencias.';
    return 'Los pedidos del e-commerce apareceran aqui.';
  });

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.loadError.set('');
    this.api.get<Order[]>(`orders?t=${Date.now()}`).subscribe({
      next: (data) => {
        if (!Array.isArray(data)) {
          this.allOrders.set([]);
          this.loadError.set('La API de pedidos respondio con un formato inesperado.');
          return;
        }
        this.allOrders.set(data);
      },
      error: (err) => {
        const status = err?.status ? ` (${err.status})` : '';
        const message = `No se pudieron cargar los pedidos${status}. Verifique que la API configurada sea la correcta.`;
        this.allOrders.set([]);
        this.loadError.set(message);
        this.toast.error(message);
      }
    });
  }

  filterStatus(event: Event) {
    this.statusFilter.set((event.target as HTMLSelectElement).value);
  }

  updateStatus(orderId: number, status: string) {
    this.api.put<Order | {
      order: Order;
      stockWarnings?: string[];
      stockDeducted?: string[];
      stockProcessed?: boolean;
      saleCreated?: boolean;
      saleDocument?: string;
      saleWarnings?: string[];
    }>(`orders/${orderId}/status`, { status }).subscribe({
      next: (res) => {
        const payload = 'order' in res ? res : null;
        const order = payload?.order ?? (res as Order);
        const warnings = [
          ...(payload?.stockWarnings ?? []),
          ...(payload?.saleWarnings ?? [])
        ];
        const deducted = payload?.stockDeducted ?? [];

        if (warnings.length) {
          this.toast.error(warnings.join(' · '));
        } else if (payload?.saleCreated && payload.saleDocument) {
          this.toast.success(`Pedido pagado → venta ${payload.saleDocument}`);
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
