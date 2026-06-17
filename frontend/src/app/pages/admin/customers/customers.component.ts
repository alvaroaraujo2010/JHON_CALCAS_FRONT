import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Customer } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './customers.component.html'
})
export class CustomersComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Customer[]>([]);
  readonly searchTerm = signal('');
  readonly filteredItems = computed(() => {
    const t = this.searchTerm().toLowerCase();
    if (!t) return this.items();
    return this.items().filter(c => c.name?.toLowerCase().includes(t) || c.taxId?.toLowerCase().includes(t) || c.contactName?.toLowerCase().includes(t) || c.email?.toLowerCase().includes(t));
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
    this.api.loadList<Customer[]>('customers', d => this.items.set(d)).subscribe();
  }

  edit(c: Customer) {
    this.editingId = c.id; this.showForm = true;
    this.form.patchValue({ name: c.name, taxId: c.taxId || '', contactName: c.contactName || '', phone: c.phone || '', email: c.email || '', address: c.address || '', isActive: c.isActive });
  }

  save() {
    if (this.form.invalid) return;
    const body = this.form.getRawValue();
    const req = this.editingId ? this.api.put(`customers/${this.editingId}`, body) : this.api.post('customers', body);
    this.api
      .run(req, {
        success: this.editingId ? 'Cliente actualizado' : 'Cliente creado',
        error: 'No se pudo guardar el cliente'
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
