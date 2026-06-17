import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { Company } from '../../../core/models';
import { DEFAULT_PUBLIC_COMPANY } from '../../../core/company-defaults';
import {
  JHON_CALCAS_IMAGES,
  JHON_CALCAS_SECTIONS,
  JHON_CALCAS_WHATSAPP_URL
} from '../../../core/jhon-calcas-assets';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private api = inject(ApiService);

  readonly images = JHON_CALCAS_IMAGES;
  readonly sections = JHON_CALCAS_SECTIONS.map(s => ({
    ...s,
    slug: s.id === 'calcas-moto' ? 'calcas-motos' : s.id
  }));
  readonly whatsappUrl = JHON_CALCAS_WHATSAPP_URL;
  readonly company = signal<Company>(DEFAULT_PUBLIC_COMPANY);

  readonly paymentMethods = [
    { src: JHON_CALCAS_IMAGES.payments.mercadoPago, alt: 'Mercado Pago' },
    { src: JHON_CALCAS_IMAGES.payments.visa, alt: 'Visa' },
    { src: JHON_CALCAS_IMAGES.payments.mastercard, alt: 'Mastercard' },
    { src: JHON_CALCAS_IMAGES.payments.amex, alt: 'American Express' },
    { src: JHON_CALCAS_IMAGES.payments.diners, alt: 'Diners Club' }
  ];

  ngOnInit() {
    this.api.getPublic<Company>('company/public').subscribe({
      next: c => this.company.set(c),
      error: () => this.company.set(DEFAULT_PUBLIC_COMPANY)
    });
  }
}
