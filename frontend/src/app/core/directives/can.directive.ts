import { Directive, inject, Input, OnDestroy, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { PermissionService } from '../services/permission.service';

/**
 * Directiva estructural: *can="'sales.create'" — muestra el elemento solo si el
 * usuario tiene el permiso. Acepta también *can="['sales.create','sales.edit']"
 * (modo OR) para cualquiera de los permisos de la lista.
 */
@Directive({ selector: '[can]', standalone: true })
export class CanDirective implements OnInit, OnDestroy {
  private tpl = inject(TemplateRef);
  private vcr = inject(ViewContainerRef);
  private perms = inject(PermissionService);
  private keys: string[] = [];
  private rendered = false;

  @Input() set can(value: string | string[]) {
    this.keys = Array.isArray(value) ? value : [value];
    this.update();
  }

  ngOnInit() { this.update(); }
  ngOnDestroy() { this.vcr.clear(); }

  private update() {
    const allowed = this.perms.canAny(this.keys);
    if (allowed && !this.rendered) {
      this.vcr.createEmbeddedView(this.tpl);
      this.rendered = true;
    } else if (!allowed && this.rendered) {
      this.vcr.clear();
      this.rendered = false;
    }
  }
}
