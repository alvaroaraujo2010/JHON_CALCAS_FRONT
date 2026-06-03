import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { PayrollService } from '../../../../core/services/payroll.service';
import { SocialSecurityPayment } from '../../../../core/models';

@Component({
  selector: 'app-social-security',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './social-security.component.html',
  styleUrl: './social-security.component.scss'
})
export class SocialSecurityComponent implements OnInit, OnDestroy {
  private payrollService = inject(PayrollService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  payments = signal<SocialSecurityPayment[]>([]);
  showPaymentForm = false;
  saving = false;
  selectedPeriod = signal<string>('');

  form = this.fb.group({
    period: ['', Validators.required],
    paymentDate: ['', Validators.required]
  });

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
    this.payrollService.getSocialSecurityPayments().subscribe({
      next: (data) => this.payments.set(data),
      error: () => console.error('Error loading social security payments')
    });
  }

  loadPaymentsByPeriod(period: string) {
    this.selectedPeriod.set(period);
  }

  openPaymentForm() {
    const period = this.getPendingPeriods()[0] ?? '';
    if (!period) {
      alert('No hay aportes pendientes para pagar. Primero procese una nómina para generar seguridad social.');
      return;
    }

    this.showPaymentForm = true;
    this.form.reset({
      period,
      paymentDate: new Date().toISOString().split('T')[0]
    });
  }

  recordPayment() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;

    const period = this.form.get('period')?.value;
    if (!period) {
      this.saving = false;
      return;
    }

    const filteredPayments = this.payments().filter(p => p.period === period && p.status === 'pending');

    if (filteredPayments.length === 0) {
      alert('No hay pagos pendientes para este período');
      this.saving = false;
      return;
    }

    const updatedPayments = filteredPayments.map(p => ({
      ...p,
      paymentDate: this.form.get('paymentDate')?.value ?? undefined,
      status: 'paid' as const
    }));

    this.payrollService.paySocialSecurity(updatedPayments).subscribe({
      next: () => {
        alert('Pago de seguridad social registrado correctamente');
        this.showPaymentForm = false;
        this.form.reset();
        this.saving = false;
        this.reload();
      },
      error: () => {
        alert('Error al registrar el pago');
        this.saving = false;
      }
    });
  }

  getTotalPending(): number {
    return this.payments()
      .filter(p => p.status === 'pending')
      .reduce((sum, p) => sum + p.contributionAmount, 0);
  }

  getTotalPaid(): number {
    return this.payments()
      .filter(p => p.status === 'paid')
      .reduce((sum, p) => sum + p.contributionAmount, 0);
  }

  getUniquePeriods(): string[] {
    const periods = new Set(this.payments().map(p => p.period));
    return Array.from(periods).sort().reverse();
  }

  getPendingPeriods(): string[] {
    const periods = new Set(this.payments()
      .filter(p => p.status === 'pending')
      .map(p => p.period));
    return Array.from(periods).sort().reverse();
  }

  getVisiblePayments(): SocialSecurityPayment[] {
    const period = this.selectedPeriod();
    return period ? this.payments().filter(p => p.period === period) : this.payments();
  }

  getPendingTotalForSelectedPeriod(): number {
    const period = this.form.get('period')?.value;
    if (!period) return 0;
    return this.payments()
      .filter(p => p.period === period && p.status === 'pending')
      .reduce((sum, p) => sum + p.contributionAmount, 0);
  }
}
