import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Product, Purchase, Supplier } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-purchases',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, ModuleHeaderComponent],
  templateUrl: './purchases.component.html'
})
export class PurchasesComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Purchase[]>([]);
  suppliers = signal<Supplier[]>([]);
  products = signal<Product[]>([]);
  showForm = false;
  lines: { productId: number; quantity: number; unitCost: number; name: string }[] = [];

  form = this.fb.group({
    supplierId: [0, Validators.min(1)],
    taxRate: [0.19],
    notes: [''],
    productId: [0],
    quantity: [1],
    unitCost: [0]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<Purchase[]>('purchases', d => this.items.set(d)).subscribe();
    this.api.loadList<Supplier[]>('suppliers', d => this.suppliers.set(d)).subscribe();
    this.api.loadList<Product[]>('products', d => this.products.set(d)).subscribe();
  }

  addLine() {
    const v = this.form.getRawValue();
    const prod = this.products().find(p => p.id === Number(v.productId));
    if (!prod || !v.quantity) return;
    this.lines.push({ productId: prod.id, quantity: v.quantity!, unitCost: v.unitCost || prod.unitCost, name: prod.name });
  }

  save() {
    const v = this.form.getRawValue();
    if (!v.supplierId || !this.lines.length) return;
    const req = this.api.post('purchases', {
      supplierId: Number(v.supplierId),
      taxRate: v.taxRate,
      notes: v.notes,
      details: this.lines.map(l => ({ productId: l.productId, quantity: l.quantity, unitCost: l.unitCost }))
    });
    this.api
      .run(req, {
        success: 'Compra registrada correctamente',
        error: 'No se pudo registrar la compra'
      })
      .subscribe({
        next: () => {
          this.showForm = false;
          this.lines = [];
          this.reload();
        }
      });
  }
}
