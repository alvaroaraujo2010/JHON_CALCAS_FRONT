import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Category } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './categories.component.html'
})
export class CategoriesComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<Category[]>([]);
  showForm = false;
  editingId: number | null = null;

  form = this.fb.group({ name: ['', Validators.required], description: [''], isActive: [true] });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<Category[]>('categories', d => this.items.set(d)).subscribe();
  }

  edit(c: Category) {
    this.editingId = c.id; this.showForm = true;
    this.form.patchValue({ name: c.name, description: c.description || '', isActive: c.isActive });
  }

  save() {
    if (this.form.invalid) return;
    const body = this.form.getRawValue();
    const req = this.editingId
      ? this.api.put(`categories/${this.editingId}`, body)
      : this.api.post('categories', body);
    this.api
      .run(req, {
        success: this.editingId ? 'Categoria actualizada' : 'Categoria creada',
        error: 'No se pudo guardar la categoria'
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
