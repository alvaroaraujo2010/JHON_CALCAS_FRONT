import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SidebarIconComponent } from './sidebar-icon.component';
import { BrandLogoComponent } from '../../shared/brand-logo/brand-logo.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, SidebarIconComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  readonly logoSrc = JHON_CALCAS_IMAGES.logo;

  ngOnInit() {
    document.body.classList.add('jc-admin-page');
  }

  ngOnDestroy() {
    document.body.classList.remove('jc-admin-page');
  }
  menu = [
    { path: '/admin', label: 'Dashboard', icon: 'dashboard' },
    { path: '/admin/productos', label: 'Productos', icon: 'productos' },
    { path: '/admin/categorias', label: 'Categorias', icon: 'categorias' },
    { path: '/admin/inventario', label: 'Inventario', icon: 'inventario' },
    { path: '/admin/proveedores', label: 'Proveedores', icon: 'proveedores' },
    { path: '/admin/clientes', label: 'Clientes', icon: 'clientes' },
    { path: '/admin/compras', label: 'Compras', icon: 'compras' },
    { path: '/admin/ventas', label: 'Ventas', icon: 'ventas' },
    { path: '/admin/contabilidad', label: 'Contabilidad', icon: 'contabilidad' },
    { path: '/admin/usuarios', label: 'Usuarios', icon: 'usuarios' },
    { path: '/admin/empresa', label: 'Empresa', icon: 'empresa' }
  ];
}
