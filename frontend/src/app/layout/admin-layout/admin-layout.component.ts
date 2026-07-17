import { Component, inject, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { PermissionService } from '../../core/services/permission.service';
import { SidebarIconComponent } from './sidebar-icon.component';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';

interface MenuItem {
  path: string;
  label: string;
  icon: string;
  permission?: string | string[];
  indent?: boolean;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule, SidebarIconComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  private api = inject(ApiService);
  private toast = inject(ToastService);
  perms = inject(PermissionService);
  readonly logoSrc = JHON_CALCAS_IMAGES.logo;
  showChangePwd = signal(false);
  changePwdCurrent = '';
  changePwdNew = '';
  changePwdSaving = false;

  readonly menuAll: MenuItem[] = [
    { path: '/admin',                              label: 'Dashboard',          icon: 'dashboard' },
    { path: '/admin/productos',                    label: 'Productos',          icon: 'productos',     permission: 'products.view' },
    { path: '/admin/categorias',                   label: 'Categorias',         icon: 'categorias',    permission: 'categories.view' },
    { path: '/admin/inventario',                   label: 'Inventario',         icon: 'inventario',    permission: 'inventory.view' },
    { path: '/admin/proveedores',                  label: 'Proveedores',        icon: 'proveedores',   permission: 'suppliers.view' },
    { path: '/admin/clientes',                     label: 'Clientes',           icon: 'clientes',      permission: 'customers.view' },
    { path: '/admin/galeria',                      label: 'Galería',            icon: 'inventario',    permission: ['catalog.manage', 'products.edit'] },
    { path: '/admin/pedidos',                      label: 'Pedidos Web',        icon: 'ventas',        permission: ['orders.view', 'sales.view'] },
    { path: '/admin/compras',                      label: 'Compras',            icon: 'compras',       permission: 'purchases.view' },
    { path: '/admin/ventas',                       label: 'Ventas',             icon: 'ventas',        permission: 'sales.view' },
    { path: '/admin/contabilidad',                 label: 'Contabilidad',       icon: 'contabilidad',  permission: 'accounting.view' },
    { path: '/admin/pasarela-pago',                 label: 'Pasarela de pago',   icon: 'pagos',         permission: 'payments.manage' },
    { path: '/admin/nomina',                       label: 'Nómina',             icon: 'nomina',        permission: 'payroll.view' },
    { path: '/admin/nomina-empleados',             label: 'Empleados',          icon: 'empleados',     permission: 'payroll.manage', indent: true },
    { path: '/admin/nomina-deducciones',           label: 'Deducciones',        icon: 'deducciones',   permission: 'payroll.manage', indent: true },
    { path: '/admin/nomina-seguridad-social',      label: 'Seguridad Social',   icon: 'seguridad',     permission: 'social_security.view', indent: true },
    { path: '/admin/nomina-pagos',                 label: 'Pagos Realizados',   icon: 'pagos',         permission: 'payroll.view', indent: true },
    { path: '/admin/nomina-provisiones',           label: 'Provisiones',        icon: 'provisiones',   permission: 'payroll.view', indent: true },
    { path: '/admin/nomina-liquidaciones',         label: 'Liquidaciones',      icon: 'liquidacion',   permission: 'payroll.settlement', indent: true },
    { path: '/admin/nomina-parametros',            label: 'Parámetros Legales', icon: 'parametros',    permission: 'legal_params.manage', indent: true },
    { path: '/admin/usuarios',                     label: 'Usuarios',           icon: 'usuarios',      permission: 'users.view' },
    { path: '/admin/roles',                        label: 'Permisos',           icon: 'usuarios',      permission: 'users.permissions' },
    { path: '/admin/empresa',                      label: 'Empresa',            icon: 'empresa',       permission: 'company.view' }
  ];

  menu = computed<MenuItem[]>(() => {
    const set = this.perms.permissions();
    return this.menuAll.filter((m) => {
      if (!m.permission) return true;
      const keys = Array.isArray(m.permission) ? m.permission : [m.permission];
      return keys.some((k) => set.has(k));
    });
  });

  subscriptionActive = signal(true);
  subscriptionChecking = signal(true);

  ngOnInit() {
    document.body.classList.add('jc-admin-page');
    this.refreshPermissions();
    this.checkSubscription();
  }

  private refreshPermissions() {
    if (!this.auth.isLoggedIn()) return;
    const role = this.auth.user()?.role;
    if (!role) return;

    this.api.get<Record<string, string[]>>('permissions/matrix').subscribe({
      next: (matrix) => {
        const permissions = matrix[role];
        if (permissions?.length) this.perms.set(permissions);
      },
      error: () => { /* conserva permisos del login en localStorage */ },
    });
  }

  private checkSubscription() {
    this.api.get<{ isActive: boolean }>('subscription').subscribe({
      next: s => {
        this.subscriptionActive.set(s.isActive);
        this.subscriptionChecking.set(false);
      },
      error: () => this.subscriptionChecking.set(false)
    });
  }

  reactivateSubscription() {
    this.api.run(
      this.api.put('subscription', { isActive: true, monthlyFee: 0, dueDate: null }),
      { success: 'Suscripción activada. La zona administrativa ya está disponible.', error: 'No se pudo reactivar' }
    ).subscribe({ next: () => this.subscriptionActive.set(true) });
  }

  ngOnDestroy() {
    document.body.classList.remove('jc-admin-page');
  }

  toggleChangePwd() {
    this.showChangePwd.update(v => !v);
    this.changePwdCurrent = '';
    this.changePwdNew = '';
  }

  changePassword() {
    if (this.changePwdNew.length < 6) return;
    this.changePwdSaving = true;
    this.api.run(
      this.api.post('auth/change-password', {
        currentPassword: this.changePwdCurrent,
        newPassword: this.changePwdNew
      }),
      { success: 'Contraseña actualizada', error: 'No se pudo cambiar la contraseña' }
    ).subscribe({ next: () => { this.toggleChangePwd(); this.changePwdSaving = false; }, error: () => this.changePwdSaving = false });
  }
}
