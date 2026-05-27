import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { JHON_CALCAS_NAV_MENU } from '../../core/jhon-calcas-menu.data';
import { JHON_CALCAS_IMAGES } from '../../core/jhon-calcas-assets';
import { JhonCalcasMenuNode, JhonCalcasNavItem } from '../../core/jhon-calcas-menu.types';

@Component({
  selector: 'app-jhon-calcas-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './jhon-calcas-header.component.html',
  styleUrl: './jhon-calcas-header.component.scss'
})
export class JhonCalcasHeaderComponent implements AfterViewInit, OnDestroy {
  @ViewChild('headerEl') headerEl?: ElementRef<HTMLElement>;

  readonly images = JHON_CALCAS_IMAGES;
  readonly menu = JHON_CALCAS_NAV_MENU;

  mobileOpen = false;
  openMenu: string | null = null;
  openBrand: string | null = null;

  /** Cabecera fija tras bajar un poco (no al cargar). */
  headerPinned = false;
  headerVisible = false;
  placeholderHeight = 0;

  private readonly scrollThreshold = 160;
  private readonly pinDelayMs = 220;
  private pinTimer?: ReturnType<typeof setTimeout>;

  ngAfterViewInit() {
    this.syncPinState();
  }

  ngOnDestroy() {
    this.clearPinTimer();
  }

  @HostListener('window:scroll')
  onWindowScroll() {
    this.syncPinState();
  }

  @HostListener('window:resize')
  onWindowResize() {
    if (this.headerPinned) {
      this.placeholderHeight = this.measureHeaderHeight();
    }
  }

  private syncPinState() {
    const scrolled = window.scrollY > this.scrollThreshold;

    if (scrolled) {
      if (!this.headerPinned && this.pinTimer === undefined) {
        this.pinTimer = setTimeout(() => {
          this.pinTimer = undefined;
          if (window.scrollY <= this.scrollThreshold) return;

          this.placeholderHeight = this.measureHeaderHeight();
          this.headerPinned = true;
          requestAnimationFrame(() => {
            this.headerVisible = true;
          });
        }, this.pinDelayMs);
      }
      return;
    }

    this.clearPinTimer();
    this.headerVisible = false;
    this.headerPinned = false;
    this.placeholderHeight = 0;
  }

  private clearPinTimer() {
    if (this.pinTimer !== undefined) {
      clearTimeout(this.pinTimer);
      this.pinTimer = undefined;
    }
  }

  private measureHeaderHeight(): number {
    return this.headerEl?.nativeElement.offsetHeight ?? 0;
  }

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
}
