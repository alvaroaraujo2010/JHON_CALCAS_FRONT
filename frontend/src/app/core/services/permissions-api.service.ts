import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import { Permission } from '../models';

@Injectable({ providedIn: 'root' })
export class PermissionsApiService {
  private api = inject(ApiService);

  getCatalog() {
    return this.api.get<Permission[]>('permissions/catalog');
  }

  getRoles() {
    return this.api.get<string[]>('permissions/roles');
  }

  getMatrix() {
    return this.api.get<Record<string, string[]>>('permissions/matrix');
  }

  updateMatrix(role: string, keys: string[]) {
    return this.api.put<{ message: string }>('permissions/matrix', { role, keys });
  }
}
