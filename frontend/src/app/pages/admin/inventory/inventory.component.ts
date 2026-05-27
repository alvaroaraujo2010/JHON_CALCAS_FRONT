import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { InventoryMovement, Product } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, ModuleHeaderComponent],
  templateUrl: './inventory.component.html'
})
export class InventoryComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  products = signal<Product[]>([]);
  movements = signal<InventoryMovement[]>([]);

  form = this.fb.group({
    productId: [0, Validators.min(1)],
    quantity: [1, Validators.min(1)],
    type: ['Entrada', Validators.required],
    notes: ['']
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<Product[]>('products', d => this.products.set(d)).subscribe();
    this.api.loadList<InventoryMovement[]>('inventory/movements', d => this.movements.set(d)).subscribe();
  }

  adjust() {
    if (this.form.invalid) return;
    this.api
      .run(this.api.post('inventory/adjust', this.form.getRawValue()), {
        success: 'Movimiento de inventario registrado',
        error: 'No se pudo registrar el movimiento'
      })
      .subscribe({
        next: () => {
          this.form.reset({ type: 'Entrada', quantity: 1, productId: 0 });
          this.reload();
        }
      });
  }
}
