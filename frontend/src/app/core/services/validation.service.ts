import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface NitValidationResult {
  nit: string;
  dv: string;
  formatted: string;
  valid: boolean;
}

export interface DocumentType {
  code: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class ValidationService {
  private http = inject(HttpClient);

  /** Pesos oficiales DIAN (módulo 11) — Art. 1.6.1.1.5 DUR 1625/2016. */
  private static readonly Weights = [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

  /**
   * Calcula el dígito de verificación (DV) del NIT colombiano
   * con el mismo algoritmo que el backend.
   */
  static calculateDv(nit: string | null | undefined): string {
    const n = this.normalize(nit);
    if (!n) return '';
    if (!/^\d{6,15}$/.test(n)) return '';
    const digits = n.split('').map(Number);
    let weightIndex = 0;
    let sum = 0;
    for (let i = digits.length - 1; i >= 0; i--) {
      sum += digits[i] * this.Weights[weightIndex % this.Weights.length];
      weightIndex++;
    }
    const mod = sum % 11;
    return (mod < 2 ? mod : 11 - mod).toString();
  }

  static normalize(nit: string | null | undefined): string {
    return (nit ?? '').replace(/[^0-9]/g, '');
  }

  static format(nit: string | null | undefined, dv?: string | null): string {
    const n = this.normalize(nit);
    if (!n) return '';
    if (n.length <= 3) return n + (dv ? `-${dv}` : '');
    let withDots = '';
    let count = 0;
    for (let i = n.length - 1; i >= 0; i--) {
      withDots = n[i] + withDots;
      count++;
      if (count === 3 && i > 0) {
        withDots = '.' + withDots;
        count = 0;
      }
    }
    return withDots + (dv ? `-${dv}` : '');
  }

  // ─── Endpoints backend (para consistencia en producción) ─────────
  calculateDvRemote(nit: string): Observable<NitValidationResult> {
    return this.http.get<NitValidationResult>('api/validation/nit/dv', { params: { nit } });
  }
  verifyNit(nit: string, dv: string): Observable<NitValidationResult> {
    return this.http.get<NitValidationResult>('api/validation/nit/verify', { params: { nit, dv } });
  }
  getDocumentTypes(): Observable<DocumentType[]> {
    return this.http.get<DocumentType[]>('api/validation/document-types');
  }
}
