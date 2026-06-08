import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private toast = inject(ToastService);
  private base = environment.apiUrl;

  get<T>(path: string): Observable<T> {
    return this.http.get<T>(`${this.base}/${path}`);
  }

  getBlob(path: string): Observable<Blob> {
    return this.http.get(`${this.base}/${path}`, { responseType: 'blob' });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(`${this.base}/${path}`, body);
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(`${this.base}/${path}`, body);
  }

  delete(path: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${path}`);
  }

  getPublic<T>(path: string): Observable<T> {
    return this.http.get<T>(`${this.base}/${path}`);
  }

  postPublic<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(`${this.base}/${path}`, body);
  }

  /** Ejecuta peticion y muestra toast de exito o error. */
  run<T>(
    request: Observable<T>,
    messages?: { success?: string; error?: string }
  ): Observable<T> {
    return request.pipe(
      tap({
        next: () => {
          if (messages?.success) {
            this.toast.success(messages.success);
          }
        },
        error: () => {
          this.toast.error(messages?.error ?? 'No se pudo completar la operacion.');
        }
      })
    );
  }

  /** Carga lista y ejecuta callback al recibir datos (siempre refresca la UI). */
  loadList<T>(path: string, onData: (data: T) => void, onError?: (msg: string) => void) {
    const msg = 'No se pudo cargar la informacion. Verifique que el servidor este activo.';
    return this.get<T>(path).pipe(
      tap({
        next: onData,
        error: () => {
          onError?.(msg);
          this.toast.error(msg);
        }
      })
    );
  }
}
