import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PermissionService } from '../../../core/services/permission.service';
import { PermissionsApiService } from '../../../core/services/permissions-api.service';
import { Permission } from '../../../core/models';
import { ToastService } from '../../../core/services/toast.service';

interface ModuleGroup {
  module: string;
  label: string;
  permissions: Permission[];
}

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss'
})
export class RolesComponent implements OnInit {
  private api = inject(PermissionsApiService);
  private toast = inject(ToastService);
  perms = inject(PermissionService);

  readonly modules: { key: string; label: string }[] = [
    { key: 'users',           label: 'Usuarios y seguridad' },
    { key: 'company',         label: 'Empresa' },
    { key: 'products',        label: 'Productos' },
    { key: 'categories',      label: 'Categorías' },
    { key: 'inventory',       label: 'Inventario' },
    { key: 'suppliers',       label: 'Proveedores' },
    { key: 'customers',       label: 'Clientes' },
    { key: 'purchases',       label: 'Compras' },
    { key: 'sales',           label: 'Ventas y FE' },
    { key: 'accounting',      label: 'Contabilidad' },
    { key: 'reports',         label: 'Reportes' },
    { key: 'payroll',         label: 'Nómina' },
    { key: 'social_security', label: 'Seguridad social' },
    { key: 'legal_params',    label: 'Parámetros legales' }
  ];

  roles = signal<string[]>([]);
  selectedRole = signal<string>('Administrador');
  catalog = signal<Permission[]>([]);
  matrix = signal<Record<string, string[]>>({});
  saving = signal(false);

  grouped = computed<ModuleGroup[]>(() => {
    const list = this.catalog();
    return this.modules
      .map(m => ({
        module: m.key,
        label: m.label,
        permissions: list.filter(p => p.module === m.key)
      }))
      .filter(g => g.permissions.length > 0);
  });

  ngOnInit() {
    if (!this.perms.can('users.permissions')) {
      this.toast.error('No tiene permiso para administrar la matriz de roles');
      return;
    }
    this.load();
  }

  async load() {
    try {
      const [roles, catalog, matrix] = await Promise.all([
        firstValueFrom(this.api.getRoles()),
        firstValueFrom(this.api.getCatalog()),
        firstValueFrom(this.api.getMatrix())
      ]);
      this.roles.set(roles);
      this.catalog.set(catalog);
      this.matrix.set(matrix);
      if (!roles.includes(this.selectedRole()) && roles.length > 0) {
        this.selectedRole.set(roles[0]);
      }
    } catch (e: any) {
      this.toast.error('No se pudo cargar la matriz de permisos');
    }
  }

  isChecked(key: string): boolean {
    return (this.matrix()[this.selectedRole()] ?? []).includes(key);
  }

  toggle(key: string, checked: boolean) {
    const m = { ...this.matrix() };
    const set = new Set(m[this.selectedRole()] ?? []);
    if (checked) set.add(key); else set.delete(key);
    m[this.selectedRole()] = Array.from(set);
    this.matrix.set(m);
  }

  countForRole(role: string): number {
    return (this.matrix()[role] ?? []).length;
  }

  countTotal(): number {
    return this.catalog().length;
  }

  async save() {
    const role = this.selectedRole();
    const keys = this.matrix()[role] ?? [];
    this.saving.set(true);
    try {
      const res = await firstValueFrom(this.api.updateMatrix(role, keys));
      this.toast.success(res.message);
    } catch (e: any) {
      this.toast.error(e?.error?.message ?? 'No se pudo guardar la matriz');
    } finally {
      this.saving.set(false);
    }
  }
}
