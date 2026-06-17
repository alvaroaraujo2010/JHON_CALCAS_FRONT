import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { Order } from '../../../core/models';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="jc-confirm page-width">
      @if (loading()) {
        <p>Verificando pago...</p>
      } @else if (error()) {
        <div class="jc-confirm__error">
          <h2>Hubo un problema</h2>
          <p>{{ error() }}</p>
          <a routerLink="/" class="jc-btn">Volver al inicio</a>
        </div>
      } @else if (order(); as o) {
        @if (o.status === 'paid' || o.status === 'completed') {
          <div class="jc-confirm__success">
            <h2>¡Pago confirmado!</h2>
            <p>Gracias por tu compra, {{ o.customerName }}.</p>
            <p>Pedido #{{ o.orderNumber }}</p>
            <p>Te enviaremos la confirmación a {{ o.customerEmail }}</p>
            <a routerLink="/" class="jc-btn">Seguir comprando</a>
          </div>
        } @else if (o.status === 'pending') {
          <div class="jc-confirm__pending">
            <h2>Pago pendiente</h2>
            <p>Tu pedido #{{ o.orderNumber }} está pendiente de pago.</p>
            <p>Te notificaremos cuando se confirme.</p>
            <a routerLink="/" class="jc-btn">Volver al inicio</a>
          </div>
        } @else {
          <div class="jc-confirm__error">
            <h2>Estado: {{ o.status }}</h2>
            <p>Contacta con nosotros si tienes dudas.</p>
            <a routerLink="/" class="jc-btn">Volver al inicio</a>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .jc-confirm { padding: 4rem 1.5rem; text-align: center; max-width: 600px; }
    .jc-confirm h2 { font-size: 1.5rem; margin-bottom: 1rem; }
    .jc-confirm__success h2 { color: #2e7d32; }
    .jc-confirm__pending h2 { color: #f57f17; }
    .jc-confirm__error h2 { color: #c62828; }
    .jc-btn { margin-top: 1.5rem; display: inline-block; }
  `]
})
export class OrderConfirmationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);

  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit() {
    const orderId = this.route.snapshot.paramMap.get('id');
    const mpPaymentId = this.route.snapshot.queryParamMap.get('payment_id');
    const status = this.route.snapshot.queryParamMap.get('status');

    if (orderId) {
      this.api.getPublic<Order>(`orders/${orderId}`).subscribe({
        next: (o) => { this.order.set(o); this.loading.set(false); },
        error: () => { this.error.set('No se pudo verificar el pedido.'); this.loading.set(false); }
      });
    } else {
      this.error.set('No se encontró información del pedido.');
      this.loading.set(false);
    }
  }
}
