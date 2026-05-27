import { Routes } from '@angular/router';
import { authGuard, publicGuard } from './core/guards/auth.guard';
import { HomeComponent } from './pages/public/home/home.component';
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

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent, canActivate: [publicGuard] },
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'productos', component: ProductsComponent },
      { path: 'categorias', component: CategoriesComponent },
      { path: 'inventario', component: InventoryComponent },
      { path: 'proveedores', component: SuppliersComponent },
      { path: 'clientes', component: CustomersComponent },
      { path: 'compras', component: PurchasesComponent },
      { path: 'ventas', component: SalesComponent },
      { path: 'contabilidad', component: AccountingComponent },
      { path: 'usuarios', component: UsersComponent },
      { path: 'empresa', component: CompanyComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
