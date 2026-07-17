import { Routes } from '@angular/router';
import { authGuard, publicGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { PublicLayoutComponent } from './layout/public-layout/public-layout.component';
import { HomeComponent } from './pages/public/home/home.component';
import { GalleryComponent } from './pages/public/gallery/gallery.component';
import { CheckoutComponent } from './pages/public/checkout/checkout.component';
import { OrderConfirmationComponent } from './pages/public/order-confirmation/order-confirmation.component';
import { LoginComponent } from './pages/public/login/login.component';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';
import { DashboardComponent } from './pages/admin/dashboard/dashboard.component';
import { ProductsComponent } from './pages/admin/products/products.component';
import { CategoriesComponent } from './pages/admin/categories/categories.component';
import { InventoryComponent } from './pages/admin/inventory/inventory.component';
import { SuppliersComponent } from './pages/admin/suppliers/suppliers.component';
import { CustomersComponent } from './pages/admin/customers/customers.component';
import { PurchasesComponent } from './pages/admin/purchases/purchases.component';
import { SalesComponent } from './pages/admin/sales/sales.component';
import { AccountingComponent } from './pages/admin/accounting/accounting.component';
import { UsersComponent } from './pages/admin/users/users.component';
import { CompanyComponent } from './pages/admin/company/company.component';
import { PayrollComponent } from './pages/admin/payroll/payroll.component';
import { PayrollDetailsComponent } from './pages/admin/payroll/payroll-details/payroll-details.component';
import { EmployeesComponent } from './pages/admin/payroll/employees/employees.component';
import { DeductionsComponent } from './pages/admin/payroll/deductions/deductions.component';
import { SocialSecurityComponent } from './pages/admin/payroll/social-security/social-security.component';
import { PaymentRecordsComponent } from './pages/admin/payroll/payment-records/payment-records.component';
import { LegalParametersComponent } from './pages/admin/payroll/legal-parameters/legal-parameters.component';
import { SettlementsComponent } from './pages/admin/payroll/settlements/settlements.component';
import { ProvisionsComponent } from './pages/admin/payroll/provisions/provisions.component';
import { RolesComponent } from './pages/admin/roles/roles.component';
import { GalleryAdminComponent } from './pages/admin/gallery/gallery-admin.component';
import { OrdersAdminComponent } from './pages/admin/orders/orders-admin.component';
import { PaymentSettingsComponent } from './pages/admin/payment-settings/payment-settings.component';

export const routes: Routes = [
  {
    path: '', component: PublicLayoutComponent, children: [
      { path: '', component: HomeComponent },
      { path: 'categoria/:slug', component: GalleryComponent },
      { path: 'galeria', component: GalleryComponent },
      { path: 'checkout', component: CheckoutComponent },
      { path: 'pedido/:id', component: OrderConfirmationComponent },
    ]
  },
  { path: 'login', component: LoginComponent, canActivate: [publicGuard] },
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'productos',         component: ProductsComponent,        canActivate: [permissionGuard], data: { permission: 'products.view' } },
      { path: 'categorias',        component: CategoriesComponent,      canActivate: [permissionGuard], data: { permission: 'categories.view' } },
      { path: 'inventario',        component: InventoryComponent,       canActivate: [permissionGuard], data: { permission: 'inventory.view' } },
      { path: 'proveedores',       component: SuppliersComponent,       canActivate: [permissionGuard], data: { permission: 'suppliers.view' } },
      { path: 'clientes',          component: CustomersComponent,       canActivate: [permissionGuard], data: { permission: 'customers.view' } },
      { path: 'compras',           component: PurchasesComponent,       canActivate: [permissionGuard], data: { permission: 'purchases.view' } },
      { path: 'ventas',            component: SalesComponent,           canActivate: [permissionGuard], data: { permission: 'sales.view' } },
      { path: 'contabilidad',      component: AccountingComponent,      canActivate: [permissionGuard], data: { permission: 'accounting.view' } },
      { path: 'galeria',           component: GalleryAdminComponent,    canActivate: [permissionGuard], data: { permission: ['catalog.manage', 'products.edit'] } },
      { path: 'pedidos',           component: OrdersAdminComponent,     canActivate: [permissionGuard], data: { permission: 'orders.view' } },
      { path: 'pasarela-pago',     component: PaymentSettingsComponent, canActivate: [permissionGuard], data: { permission: 'payments.manage' } },
      { path: 'usuarios',          component: UsersComponent,           canActivate: [permissionGuard], data: { permission: 'users.view' } },
      { path: 'roles',             component: RolesComponent,           canActivate: [permissionGuard], data: { permission: 'users.permissions' } },
      { path: 'empresa',           component: CompanyComponent,         canActivate: [permissionGuard], data: { permission: 'company.view' } },
      // Módulo de Nómina
      { path: 'nomina',                       component: PayrollComponent,          canActivate: [permissionGuard], data: { permission: 'payroll.view' } },
      { path: 'nomina/:id',                   component: PayrollDetailsComponent },
      { path: 'nomina-empleados',             component: EmployeesComponent,         canActivate: [permissionGuard], data: { permission: 'payroll.manage' } },
      { path: 'nomina-deducciones',           component: DeductionsComponent,       canActivate: [permissionGuard], data: { permission: 'payroll.manage' } },
      { path: 'nomina-seguridad-social',      component: SocialSecurityComponent,   canActivate: [permissionGuard], data: { permission: 'social_security.view' } },
      { path: 'nomina-pagos',                 component: PaymentRecordsComponent,   canActivate: [permissionGuard], data: { permission: 'payroll.view' } },
      { path: 'nomina-parametros',            component: LegalParametersComponent,  canActivate: [permissionGuard], data: { permission: 'legal_params.manage' } },
      { path: 'nomina-provisiones',           component: ProvisionsComponent,       canActivate: [permissionGuard], data: { permission: 'payroll.view' } },
      { path: 'nomina-liquidaciones',         component: SettlementsComponent,      canActivate: [permissionGuard], data: { permission: 'payroll.settlement' } }
    ]
  },
  { path: '**', redirectTo: '' }
];
