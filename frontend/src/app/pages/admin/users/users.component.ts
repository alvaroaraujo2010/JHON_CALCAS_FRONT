import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { User } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, ModuleHeaderComponent],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<User[]>([]);
  readonly searchTerm = signal('');
  readonly filteredItems = computed(() => {
    const t = this.searchTerm().toLowerCase();
    if (!t) return this.items();
    return this.items().filter(u => u.fullName?.toLowerCase().includes(t) || u.email?.toLowerCase().includes(t) || u.role?.toLowerCase().includes(t));
  });
  showForm = false;
  editingUser: User | null = null;
  resetPasswordFor: User | null = null;
  resetPwd = '';

  form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    role: ['Vendedor', Validators.required]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<User[]>('users', d => this.items.set(d)).subscribe();
  }

  openNew() {
    this.editingUser = null;
    this.form.reset({ role: 'Vendedor' });
    this.form.get('password')?.setValidators([Validators.required]);
    this.form.get('password')?.updateValueAndValidity();
    this.showForm = true;
  }

  openEdit(u: User) {
    this.editingUser = u;
    this.form.patchValue({ fullName: u.fullName, email: u.email, role: u.role });
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.showForm = true;
  }

  cancelForm() {
    this.showForm = false;
    this.editingUser = null;
  }

  save() {
    if (this.form.invalid) return;
    if (this.editingUser) {
      const body = {
        ...this.form.getRawValue(),
        isActive: this.editingUser.isActive
      };
      this.api.run(
        this.api.put(`users/${this.editingUser.id}`, body),
        { success: 'Usuario actualizado', error: 'No se pudo actualizar' }
      ).subscribe({ next: () => { this.cancelForm(); this.reload(); } });
    } else {
      this.api.run(
        this.api.post('users', this.form.getRawValue()),
        { success: 'Usuario creado', error: 'No se pudo crear' }
      ).subscribe({ next: () => { this.cancelForm(); this.reload(); } });
    }
  }

  toggleActive(u: User) {
    const label = u.isActive ? 'desactivar' : 'activar';
    this.api.run(
      this.api.put(`users/${u.id}/toggle-active`, {}),
      { success: `Usuario ${label}do`, error: `No se pudo ${label}` }
    ).subscribe({ next: () => this.reload() });
  }

  openResetPwd(u: User) {
    this.resetPasswordFor = u;
    this.resetPwd = '';
  }

  confirmResetPwd() {
    if (!this.resetPasswordFor || this.resetPwd.length < 6) return;
    this.api.run(
      this.api.put(`users/${this.resetPasswordFor.id}/reset-password`, { newPassword: this.resetPwd }),
      { success: 'Contraseña restablecida', error: 'No se pudo restablecer' }
    ).subscribe({ next: () => { this.resetPasswordFor = null; this.resetPwd = ''; } });
  }
}
