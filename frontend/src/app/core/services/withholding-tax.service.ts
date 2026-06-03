import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { WithholdingTaxBracket } from '../models';

@Injectable({ providedIn: 'root' })
export class WithholdingTaxService {
  private api = inject(ApiService);

  getBrackets(year: number): Observable<WithholdingTaxBracket[]> {
    return this.api.get<WithholdingTaxBracket[]>(`withholding-tax/brackets?year=${year}`);
  }

  calculate(year: number, grossIncome: number, opts?: {
    nonTaxableIncome?: number;
    mandatoryContributions?: number;
    procedure?: '1' | '2';
  }): Observable<any> {
    return this.api.post('withholding-tax/calculate', {
      year,
      grossIncome,
      nonTaxableIncome: opts?.nonTaxableIncome ?? 0,
      mandatoryContributions: opts?.mandatoryContributions ?? 0,
      procedure: opts?.procedure ?? '1'
    });
  }
}
