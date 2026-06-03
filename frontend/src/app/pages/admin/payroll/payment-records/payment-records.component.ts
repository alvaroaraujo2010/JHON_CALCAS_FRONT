import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { PayrollService } from '../../../../core/services/payroll.service';
import { PaymentRecord } from '../../../../core/models';

@Component({
  selector: 'app-payment-records',
  standalone: true,
  imports: [CommonModule, ModuleHeaderComponent],
  templateUrl: './payment-records.component.html',
  styleUrl: './payment-records.component.scss'
})
export class PaymentRecordsComponent implements OnInit, OnDestroy {
  private payrollService = inject(PayrollService);
  private router = inject(Router);
  private navSub?: Subscription;

  records = signal<PaymentRecord[]>([]);
  filterStatus = signal<string>('');

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.reload());
  }

  ngOnDestroy() {
    this.navSub?.unsubscribe();
  }

  reload() {
    this.payrollService.getPaymentRecords().subscribe({
      next: (data) => this.records.set(data),
      error: () => console.error('Error loading payment records')
    });
  }

  getTotalAmount(): number {
    return this.getFilteredRecords().reduce((sum, r) => sum + r.totalAmount, 0);
  }

  getFilteredRecords(): PaymentRecord[] {
    const status = this.filterStatus();
    if (!status) return this.records();
    return this.records().filter(r => r.status === status);
  }

  downloadReceipt(record: PaymentRecord) {
    // Implementación para descargar comprobante
    console.log('Descargando comprobante:', record.id);
  }
}
