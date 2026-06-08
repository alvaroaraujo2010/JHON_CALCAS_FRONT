import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from '../services/permission.service';

/**
 * Guard de rutas basado en permisos. Se activa declarando la ruta con
 *   { path: 'ventas', component: ..., canActivate: [permissionGuard], data: { permission: 'sales.view' } }
 * Si la ruta no requiere permiso, dejar sin `data.permission`.
 */
export const permissionGuard: CanActivateFn = (route) => {
  const perms = inject(PermissionService);
  const router = inject(Router);
  const required = route.data?.['permission'] as string | string[] | undefined;
  if (!required) return true;
  const ok = Array.isArray(required) ? perms.canAny(required) : perms.can(required);
  return ok ? true : router.createUrlTree(['/admin']);
};
