import { Directive, inject, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';

/** Recarga datos cada vez que se entra a la ruta del modulo. */
@Directive()
export abstract class AdminListPage implements OnInit, OnDestroy {
  protected router = inject(Router);
  private sub?: Subscription;
  loadError = '';

  abstract reload(): void;

  ngOnInit() {
    this.reload();
    this.sub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.reload());
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  protected clearError() {
    this.loadError = '';
  }
}
