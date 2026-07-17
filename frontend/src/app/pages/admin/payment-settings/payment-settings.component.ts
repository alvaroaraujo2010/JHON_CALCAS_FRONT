import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

interface PaymentSettings {
  provider: string;
  isActive: boolean;
  useSandbox: boolean;
  publicKey: string;
  accessToken: string;
  baseUrl: string;
  webhookUrl: string;
  updatedAt: string;
}

@Component({
  selector: 'app-payment-settings',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  template: `
    <div class="module-page">
      <app-module-header title="Pasarela de pago" subtitle="Configura Mercado Pago para el checkout del e-commerce" badge="Pagos" />

      <form class="card-form" [formGroup]="form" (ngSubmit)="save()">
        <div class="card-form__head">
          <h3>Mercado Pago</h3>
          <p>Estos datos se usan al crear la preferencia de pago y al confirmar pagos por webhook.</p>
        </div>

        <div class="gateway-status">
          <label class="check-row">
            <input type="checkbox" formControlName="isActive" />
            <span>Pasarela activa</span>
          </label>
          <label class="check-row">
            <input type="checkbox" formControlName="useSandbox" />
            <span>Usar sandbox</span>
          </label>
        </div>

        <div class="form-grid">
          <label class="full">Public Key
            <input formControlName="publicKey" placeholder="APP_USR-..." />
          </label>
          <label class="full">Access Token
            <input formControlName="accessToken" placeholder="APP_USR-..." autocomplete="off" />
          </label>
          <label>URL base de la tienda
            <input formControlName="baseUrl" placeholder="https://app.jhoncalcas.com.co" />
          </label>
          <label>Webhook URL opcional
            <input formControlName="webhookUrl" placeholder="https://app.jhoncalcas.com.co/api/payment/webhook" />
          </label>
        </div>

        <div class="gateway-help">
          <strong>URL de webhook recomendada:</strong>
          <span>{{ recommendedWebhook() }}</span>
        </div>

        @if (updatedAt()) {
          <p class="last-update">Última actualización: {{ updatedAt() }}</p>
        }

        <div class="form-actions">
          <button class="btn primary" type="submit" [disabled]="saving() || form.invalid">
            {{ saving() ? 'Guardando...' : 'Guardar configuración' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .gateway-status { display: flex; flex-wrap: wrap; gap: 1rem; }
    .check-row { display: inline-flex; align-items: center; gap: 0.55rem; color: #334155; font-size: 0.9rem; font-weight: 600; }
    .check-row input { width: 18px; height: 18px; accent-color: #de0404; }
    .gateway-help { display: grid; gap: 0.35rem; padding: 0.85rem 1rem; border: 1px solid #e2e8f0; border-radius: 10px; background: #f8fafc; color: #475569; font-size: 0.9rem; }
    .gateway-help span { word-break: break-all; }
    .last-update { margin: 0; color: #64748b; font-size: 0.85rem; }
  `]
})
export class PaymentSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly updatedAt = signal('');

  readonly form = this.fb.group({
    isActive: [true],
    useSandbox: [false],
    publicKey: [''],
    accessToken: [''],
    baseUrl: ['', Validators.required],
    webhookUrl: ['']
  });

  ngOnInit() {
    this.api.get<PaymentSettings>('payment-settings/mercadopago').subscribe({
      next: (settings) => {
        this.form.patchValue(settings);
        this.updatedAt.set(new Date(settings.updatedAt).toLocaleString('es-CO'));
      }
    });
  }

  recommendedWebhook() {
    const baseUrl = (this.form.controls.baseUrl.value || '').trim().replace(/\/$/, '');
    return baseUrl ? `${baseUrl}/api/payment/webhook` : 'https://app.jhoncalcas.com.co/api/payment/webhook';
  }

  save() {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.run(this.api.put<PaymentSettings>('payment-settings/mercadopago', this.form.getRawValue()), {
      success: 'Configuración de Mercado Pago actualizada',
      error: 'No se pudo guardar Mercado Pago'
    }).subscribe({
      next: (settings) => {
        this.form.patchValue(settings);
        this.updatedAt.set(new Date(settings.updatedAt).toLocaleString('es-CO'));
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }
}
