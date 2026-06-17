import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Supplier } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './suppliers.component.html'
})
export class SuppliersComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Supplier[]>([]);
  readonly searchTerm = signal('');
  readonly filteredItems = computed(() => {
    const t = this.searchTerm().toLowerCase();
    if (!t) return this.items();
    return this.items().filter(s => s.name?.toLowerCase().includes(t) || s.taxId?.toLowerCase().includes(t) || s.contactName?.toLowerCase().includes(t) || s.email?.toLowerCase().includes(t));
  });
  showForm = false;
  editingId: number | null = null;

  form = this.fb.group({
    name: ['', Validators.required], taxId: [''], contactName: [''],
    phone: [''], email: [''], address: [''], isActive: [true]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<Supplier[]>('suppliers', d => this.items.set(d)).subscribe();
  }

  edit(s: Supplier) {
    this.editingId = s.id; this.showForm = true;
    this.form.patchValue({ name: s.name, taxId: s.taxId || '', contactName: s.contactName || '', phone: s.phone || '', email: s.email || '', address: s.address || '', isActive: s.isActive });
  }

  save() {
    if (this.form.invalid) return;
    const body = this.form.getRawValue();
    const req = this.editingId ? this.api.put(`suppliers/${this.editingId}`, body) : this.api.post('suppliers', body);
    this.api
      .run(req, {
        success: this.editingId ? 'Proveedor actualizado' : 'Proveedor creado',
        error: 'No se pudo guardar el proveedor'
      })
      .subscribe({
        next: () => {
          this.showForm = false;
          this.editingId = null;
          this.form.reset({ isActive: true });
          this.reload();
        }
      });
  }
}
