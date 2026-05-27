import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ElectronicInvoicePdfService } from '../../../core/services/electronic-invoice-pdf.service';
import { ToastService } from '../../../core/services/toast.service';
import { Customer, ElectronicInvoice, Product, Sale } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-sales',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, PercentPipe, ModuleHeaderComponent],
  templateUrl: './sales.component.html',
  styleUrl: './sales.component.scss'
})
export class SalesComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private invoicePdf = inject(ElectronicInvoicePdfService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Sale[]>([]);
  customers = signal<Customer[]>([]);
  products = signal<Product[]>([]);
  showForm = false;
  invoice = signal<ElectronicInvoice | null>(null);
  invoiceLoading = false;
  lines: { productId: number; quantity: number; unitPrice: number; name: string }[] = [];

  form = this.fb.group({
    customerId: [0],
    taxRate: [0.19],
    paymentMethod: ['Efectivo'],
    notes: [''],
    productId: [0],
    quantity: [1],
    unitPrice: [0]
  });

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

  addLine() {
    const v = this.form.getRawValue();
    const prod = this.products().find(p => p.id === Number(v.productId));
    if (!prod || !v.quantity) return;
    this.lines.push({ productId: prod.id, quantity: v.quantity!, unitPrice: v.unitPrice || prod.unitPrice, name: prod.name });
  }

  save() {
    const v = this.form.getRawValue();
    if (!this.lines.length) return;
    const req = this.api.post('sales', {
      customerId: Number(v.customerId) || null,
      taxRate: v.taxRate,
      paymentMethod: v.paymentMethod,
      notes: v.notes,
      details: this.lines.map(l => ({ productId: l.productId, quantity: l.quantity, unitPrice: l.unitPrice }))
    });
    this.api
      .run(req, {
        success: 'Venta registrada. Puede emitir la factura electronica.',
        error: 'No se pudo registrar la venta'
      })
      .subscribe({
        next: () => {
          this.showForm = false;
          this.lines = [];
          this.reload();
        }
      });
  }

  openInvoice(sale: Sale) {
    this.invoiceLoading = true;
    this.api.get<ElectronicInvoice>(`sales/${sale.id}/electronic-invoice`).subscribe({
      next: inv => {
        this.invoice.set(inv);
        this.invoiceLoading = false;
      },
      error: () => {
        this.invoiceLoading = false;
      }
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
      .subscribe({
        next: updated => {
          this.invoice.set(updated);
          this.reload();
        }
      });
  }

  closeInvoice() {
    this.invoice.set(null);
  }

  printPdf() {
    const inv = this.invoice();
    if (!inv) return;
    this.invoicePdf.print(inv);
    this.toast.info('Se abrio la factura en PDF para imprimir');
  }

  downloadPdf() {
    const inv = this.invoice();
    if (!inv) return;
    this.invoicePdf.download(inv);
    this.toast.success(`PDF guardado: ${inv.electronicInvoiceNumber || inv.documentNumber}.pdf`);
  }
}
