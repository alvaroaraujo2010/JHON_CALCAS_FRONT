import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { LegalParameter } from '../models';

@Injectable({ providedIn: 'root' })
export class LegalParameterService {
  private api = inject(ApiService);

  list(): Observable<LegalParameter[]> {
    return this.api.get<LegalParameter[]>('legal-parameters');
  }

  getByYear(year: number): Observable<LegalParameter> {
    return this.api.get<LegalParameter>(`legal-parameters/${year}`);
  }

  getCurrent(): Observable<LegalParameter> {
    return this.api.get<LegalParameter>('legal-parameters/current');
  }

  upsert(year: number, param: LegalParameter): Observable<LegalParameter> {
    return this.api.put<LegalParameter>(`legal-parameters/${year}`, param);
  }
}
