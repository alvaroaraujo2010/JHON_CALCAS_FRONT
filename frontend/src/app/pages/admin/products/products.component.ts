import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Category, Product } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, ModuleHeaderComponent],
  templateUrl: './products.component.html'
})
export class ProductsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  showForm = false;
  editingId: number | null = null;
  saving = false;

  form = this.fb.group({
    sku: ['', Validators.required],
    name: ['', Validators.required],
    description: [''],
    categoryId: [0, Validators.min(1)],
    unitCost: [0],
    unitPrice: [0],
    minStock: [5],
    unit: ['UND'],
    isActive: [true]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.reload());
  }

  ngOnDestroy() {
    this.navSub?.unsubscribe();
  }

  reload() {
    this.api.loadList<Product[]>('products', data => this.items.set(data)).subscribe();

    this.api.loadList<Category[]>('categories', data => this.categories.set(data)).subscribe();
  }

  edit(p: Product) {
    this.editingId = p.id;
    this.showForm = true;
    this.form.patchValue({
      sku: p.sku,
      name: p.name,
      description: p.description || '',
      categoryId: p.categoryId,
      unitCost: p.unitCost,
      unitPrice: p.unitPrice,
      minStock: p.minStock,
      unit: p.unit,
      isActive: p.isActive
    });
  }

  save() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const body = this.form.getRawValue();
    const req = this.editingId
      ? this.api.put<Product>(`products/${this.editingId}`, body)
      : this.api.post<Product>('products', body);

    this.api
      .run(req, {
        success: this.editingId ? 'Producto actualizado correctamente' : 'Producto creado correctamente',
        error: 'No se pudo guardar el producto'
      })
      .subscribe({
        next: () => {
          this.showForm = false;
          this.editingId = null;
          this.form.reset({ minStock: 5, unit: 'UND', categoryId: 0, isActive: true });
          this.saving = false;
          this.reload();
        },
        error: () => {
          this.saving = false;
        }
      });
  }
}
