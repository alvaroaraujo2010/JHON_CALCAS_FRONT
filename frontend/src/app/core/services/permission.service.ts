import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'contanexo_permissions';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly _permissions = signal<Set<string>>(new Set(this.load()));

  /** Set de permisos activos del usuario actual. */
  permissions = computed(() => this._permissions());

  /** Cantidad de permisos (útil para debug). */
  count = computed(() => this._permissions().size);

  /** Reemplaza los permisos activos (al login o refresh). */
  set(permissions: string[] | null | undefined) {
    const set = new Set(permissions ?? []);
    this._permissions.set(set);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(Array.from(set))); } catch {}
  }

  /** Limpia los permisos (al logout). */
  clear() {
    this._permissions.set(new Set());
    try { localStorage.removeItem(STORAGE_KEY); } catch {}
  }

  /** ¿Tiene el permiso exacto? */
  can(key: string): boolean {
    return this._permissions().has(key);
  }

  /** ¿Tiene al menos uno de los permisos? */
  canAny(keys: string[]): boolean {
    const set = this._permissions();
    return keys.some(k => set.has(k));
  }

  /** ¿Tiene todos los permisos? */
  canAll(keys: string[]): boolean {
    const set = this._permissions();
    return keys.every(k => set.has(k));
  }

  private load(): string[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch { return []; }
  }
}
