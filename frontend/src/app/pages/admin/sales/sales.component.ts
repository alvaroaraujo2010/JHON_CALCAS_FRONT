import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ElectronicInvoicePdfService } from '../../../core/services/electronic-invoice-pdf.service';
import { ToastService } from '../../../core/services/toast.service';
import { Customer, ElectronicInvoice, Product, Sale } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';
import { FilterPipe } from '../../../shared/filter.pipe';

interface SaleLine {
  productId: number;
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe, PercentPipe, ModuleHeaderComponent, FilterPipe],
  templateUrl: './sales.component.html',
  styleUrl: './sales.component.scss'
})
export class SalesComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private invoicePdf = inject(ElectronicInvoicePdfService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Sale[]>([]);
  readonly searchTerm = signal('');
  readonly filteredItems = computed(() => {
    const t = this.searchTerm().toLowerCase();
    if (!t) return this.items();
    return this.items().filter(s => s.documentNumber?.toLowerCase().includes(t) || s.customerName?.toLowerCase().includes(t));
  });
  customers = signal<Customer[]>([]);
  products = signal<Product[]>([]);
  // Form always visible (POS mode)
  invoice = signal<ElectronicInvoice | null>(null);
  invoiceLoading = false;

  // ── POS state ──
  customerQ = signal('');
  customerSelected = signal<Customer | null>(null);
  productQ = signal('');
  productResults = signal<Product[]>([]);
  lines = signal<SaleLine[]>([]);
  paymentMethod = signal('Efectivo');
  taxRate = signal(0.19);
  wantInvoice = signal(false);
  wantElectronic = signal(false);
  showQtyModal = false;
  qtyProduct: Product | null = null;
  qtyValue = 1;
  qtyPrice = 0;

  totalConIva = computed(() => this.lines().reduce((s, l) => s + l.lineTotal, 0));
  subtotal = computed(() => Math.round(this.totalConIva() / (1 + this.taxRate()) * 100) / 100);
  tax = computed(() => Math.round((this.totalConIva() - this.subtotal()) * 100) / 100);
  total = computed(() => this.totalConIva());

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<Sale[]>('sales', d => this.items.set(d)).subscribe();
    this.api.loadList<Customer[]>('customers', d => this.customers.set(d)).subscribe();
    this.api.loadList<Product[]>('products', d => this.products.set(d)).subscribe();
  }

  onCustomerSearch(q: string) {
    this.customerQ.set(q);
    if (!q) { this.customerSelected.set(null); }
  }

  selectCustomer(c: Customer) {
    this.customerSelected.set(c);
    this.customerQ.set(c.name);
  }

  clearCustomer() {
    this.customerSelected.set(null);
    this.customerQ.set('');
  }

  onProductSearch(q: string) {
    this.productQ.set(q);
    const t = q.toLowerCase();
    this.productResults.set(
      t ? this.products().filter(p => p.name.toLowerCase().includes(t) || p.sku.toLowerCase().includes(t)) : []
    );
  }

  selectProduct(p: Product) {
    this.qtyProduct = p;
    this.qtyValue = 1;
    this.qtyPrice = p.unitPrice;
    this.showQtyModal = true;
    this.productQ.set('');
    this.productResults.set([]);
  }

  confirmQty() {
    if (!this.qtyProduct || this.qtyValue < 1 || this.qtyPrice <= 0) return;
    const lineTotal = this.qtyValue * this.qtyPrice;
    this.lines.update(ls => [...ls, {
      productId: this.qtyProduct!.id,
      name: this.qtyProduct!.name,
      quantity: this.qtyValue,
      unitPrice: this.qtyPrice,
      lineTotal
    }]);
    this.showQtyModal = false;
    this.qtyProduct = null;
  }

  cancelQty() {
    this.showQtyModal = false;
    this.qtyProduct = null;
  }

  removeLine(i: number) {
    this.lines.update(ls => ls.filter((_, idx) => idx !== i));
  }

  setPayment(method: string) {
    this.paymentMethod.set(method);
  }

  save() {
    if (!this.lines().length) {
      this.toast.error('Agregue al menos un producto');
      return;
    }

    const body = {
      customerId: this.customerSelected()?.id || null,
      taxRate: this.taxRate(),
      paymentMethod: this.paymentMethod(),
      notes: this.wantElectronic() ? 'Solicita factura electronica' : '',
      details: this.lines().map(l => ({ productId: l.productId, quantity: l.quantity, unitPrice: l.unitPrice }))
    };

    this.api.run(
      this.api.post<any>('sales', body),
      { success: 'Venta registrada correctamente', error: 'No se pudo registrar la venta' }
    ).subscribe({
      next: sale => {
        this.resetForm();
        this.reload();

        if (this.wantElectronic() && sale?.id) {
          this.api.post<ElectronicInvoice>(`sales/${sale.id}/electronic-invoice/emit`, {}).subscribe({
            next: inv => {
              this.invoice.set(inv);
              if (this.wantInvoice()) {
                setTimeout(() => this.invoicePdf.print(inv), 500);
              }
            },
            error: () => this.toast.error('No se pudo emitir la factura electrónica')
          });
        } else if (this.wantInvoice() && sale?.id) {
          this.api.get<ElectronicInvoice>(`sales/${sale.id}/electronic-invoice`).subscribe({
            next: inv => {
              this.invoice.set(inv);
              setTimeout(() => this.invoicePdf.print(inv), 500);
            },
            error: () => this.toast.error('No se pudo cargar la factura')
          });
        }
      },
      error: () => {}
    });
  }

  private resetForm() {
    this.lines.set([]);
    this.customerSelected.set(null);
    this.customerQ.set('');
    this.paymentMethod.set('Efectivo');
    this.taxRate.set(0.19);
    this.wantInvoice.set(false);
    this.wantElectronic.set(false);
  }

  private delay(ms: number) { return new Promise(r => setTimeout(r, ms)); }

  // ── Invoice modal (existing) ──
  openInvoice(sale: Sale) {
    this.invoiceLoading = true;
    this.api.get<ElectronicInvoice>(`sales/${sale.id}/electronic-invoice`).subscribe({
      next: inv => { this.invoice.set(inv); this.invoiceLoading = false; },
      error: () => { this.invoiceLoading = false; }
    });
  }

  emitInvoice() {
    const inv = this.invoice();
    if (!inv || inv.electronicInvoiceStatus === 'Emitida') return;
    this.api
      .run(this.api.post<ElectronicInvoice>(`sales/${inv.saleId}/electronic-invoice/emit`, {}), {
        success: 'Factura electronica emitida y asiento contable generado',
        error: 'No se pudo emitir la factura'
      })
      .subscribe({ next: updated => { this.invoice.set(updated); this.reload(); } });
  }

  closeInvoice() { this.invoice.set(null); }
  printPdf() { const inv = this.invoice(); if (inv) { this.invoicePdf.print(inv); this.toast.info('Se abrio la factura en PDF para imprimir'); } }
  downloadPdf() { const inv = this.invoice(); if (inv) { this.invoicePdf.download(inv); this.toast.success(`PDF guardado: ${inv.electronicInvoiceNumber || inv.documentNumber}.pdf`); } }
}
