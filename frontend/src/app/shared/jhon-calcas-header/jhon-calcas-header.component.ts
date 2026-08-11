import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { JHON_CALCAS_NAV_MENU } from '../../core/jhon-calcas-menu.data';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { parseMenuHref } from '../../core/jhon-calcas-routing';
import { JhonCalcasMenuNode, JhonCalcasNavItem } from '../../core/jhon-calcas-menu.types';
import { ThemeService } from '../../core/services/theme.service';
import { CartSlideoutComponent } from '../cart-slideout/cart-slideout.component';

@Component({
  selector: 'app-jhon-calcas-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CartSlideoutComponent, FormsModule],
  templateUrl: './jhon-calcas-header.component.html',
  styleUrl: './jhon-calcas-header.component.scss'
})
export class JhonCalcasHeaderComponent {
  readonly images = JHON_CALCAS_IMAGES;
  readonly menu = JHON_CALCAS_NAV_MENU;
  readonly theme = inject(ThemeService);
  private router = inject(Router);
  private searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  mobileOpen = signal(false);
  searchOpen = signal(false);
  searchTerm = signal('');
  openMenu = signal<string | null>(null);
  openBrand = signal<string | null>(null);

  toggleSearch() {
    const next = !this.searchOpen();
    this.searchOpen.set(next);
    if (next) {
      this.closeMenus();
      setTimeout(() => this.searchInput()?.nativeElement?.focus(), 30);
    }
  }

  submitSearch() {
    const term = this.searchTerm().trim();
    if (!term) {
      this.searchOpen.set(true);
      setTimeout(() => this.searchInput()?.nativeElement?.focus(), 0);
      return;
    }
    this.searchOpen.set(false);
    this.mobileOpen.set(false);
    this.router.navigate(['/galeria'], { queryParams: { search: term } });
  }

  toggleMenu(label: string) {
    this.openMenu.set(this.openMenu() === label ? null : label);
    this.openBrand.set(null);
  }

  toggleBrand(label: string) {
    this.openBrand.set(this.openBrand() === label ? null : label);
  }

  closeMenus() {
    this.openMenu.set(null);
    this.openBrand.set(null);
    this.mobileOpen.set(false);
  }

  onNavEnter(item: JhonCalcasNavItem) {
    if (!item.children.length) return;
    this.openMenu.set(item.label);
  }

  onNavLeave() {
    this.openMenu.set(null);
    this.openBrand.set(null);
  }

  onBrandEnter(brand: JhonCalcasMenuNode) {
    this.openBrand.set(brand.label);
  }

  brandHref(brand: JhonCalcasMenuNode): string | null {
    if (brand.href) return brand.href;
    return null;
  }

  isExternal(url: string): boolean {
    return url.startsWith('http://') || url.startsWith('https://') || url.startsWith('//');
  }

  menuRoute(href: string | undefined) {
    return href ? parseMenuHref(href) : null;
  }
}
