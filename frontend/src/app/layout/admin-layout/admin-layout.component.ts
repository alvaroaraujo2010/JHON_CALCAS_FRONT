import { Component, inject, OnDestroy, OnInit, computed } from '@angular/core';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { PermissionService } from '../../core/services/permission.service';
import { SidebarIconComponent } from './sidebar-icon.component';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';

interface MenuItem {
  path: string;
  label: string;
  icon: string;
  permission?: string;
  indent?: boolean;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, SidebarIconComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  perms = inject(PermissionService);
  readonly logoSrc = JHON_CALCAS_IMAGES.logo;

  readonly menuAll: MenuItem[] = [
    { path: '/admin',                              label: 'Dashboard',          icon: 'dashboard' },
    { path: '/admin/productos',                    label: 'Productos',          icon: 'productos',     permission: 'products.view' },
    { path: '/admin/categorias',                   label: 'Categorias',         icon: 'categorias',    permission: 'categories.view' },
    { path: '/admin/inventario',                   label: 'Inventario',         icon: 'inventario',    permission: 'inventory.view' },
    { path: '/admin/proveedores',                  label: 'Proveedores',        icon: 'proveedores',   permission: 'suppliers.view' },
    { path: '/admin/clientes',                     label: 'Clientes',           icon: 'clientes',      permission: 'customers.view' },
    { path: '/admin/compras',                      label: 'Compras',            icon: 'compras',       permission: 'purchases.view' },
    { path: '/admin/ventas',                       label: 'Ventas',             icon: 'ventas',        permission: 'sales.view' },
    { path: '/admin/contabilidad',                 label: 'Contabilidad',       icon: 'contabilidad',  permission: 'accounting.view' },
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
    return this.menuAll.filter(m => !m.permission || set.has(m.permission));
  });

  ngOnInit() {
    document.body.classList.add('jc-admin-page');
  }

  ngOnDestroy() {
    document.body.classList.remove('jc-admin-page');
  }
}
