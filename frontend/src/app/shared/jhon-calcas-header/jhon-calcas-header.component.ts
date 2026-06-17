import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { JHON_CALCAS_NAV_MENU } from '../../core/jhon-calcas-menu.data';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { parseMenuHref } from '../../core/jhon-calcas-routing';
import { JhonCalcasMenuNode, JhonCalcasNavItem } from '../../core/jhon-calcas-menu.types';
import { CartSlideoutComponent } from '../cart-slideout/cart-slideout.component';

@Component({
  selector: 'app-jhon-calcas-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CartSlideoutComponent],
  templateUrl: './jhon-calcas-header.component.html',
  styleUrl: './jhon-calcas-header.component.scss'
})
export class JhonCalcasHeaderComponent {
  readonly images = JHON_CALCAS_IMAGES;
  readonly menu = JHON_CALCAS_NAV_MENU;

  mobileOpen = false;
  openMenu: string | null = null;
  openBrand: string | null = null;

  toggleMenu(label: string) {
    this.openMenu = this.openMenu === label ? null : label;
    this.openBrand = null;
  }

  toggleBrand(label: string) {
    this.openBrand = this.openBrand === label ? null : label;
  }

  closeMenus() {
    this.openMenu = null;
    this.openBrand = null;
    this.mobileOpen = false;
  }

  onNavEnter(item: JhonCalcasNavItem) {
    if (!item.children.length) return;
    this.openMenu = item.label;
  }

  onNavLeave() {
    this.openMenu = null;
    this.openBrand = null;
  }

  onBrandEnter(brand: JhonCalcasMenuNode) {
    this.openBrand = brand.label;
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
