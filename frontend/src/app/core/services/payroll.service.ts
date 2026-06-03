import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  Payroll,
  PayrollDetail,
  Deduction,
  Employee,
  SocialSecurityPayment,
  PaymentRecord,
  PayrollProvision,
  ProvisionSummary,
  PayrollSettlement,
  PayrollSimulation
} from '../models';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private api = inject(ApiService);

  // ─── Nóminas ────────────────────────────────────────────────
  getPayrolls(): Observable<Payroll[]> {
    return this.api.get<Payroll[]>('payrolls');
  }

  getPayroll(id: number): Observable<Payroll> {
    return this.api.get<Payroll>(`payrolls/${id}`);
  }

  createPayroll(payroll: Omit<Payroll, 'id'>): Observable<Payroll> {
    return this.api.post<Payroll>('payrolls', payroll);
  }

  updatePayroll(id: number, payroll: Partial<Payroll>): Observable<Payroll> {
    return this.api.put<Payroll>(`payrolls/${id}`, payroll);
  }

  deletePayroll(id: number): Observable<void> {
    return this.api.delete(`payrolls/${id}`);
  }

  processPayroll(id: number): Observable<Payroll> {
    return this.api.post<Payroll>(`payrolls/${id}/process`, {});
  }

  payPayroll(id: number, paymentData: { paymentDate: string; paymentMethod: string; reference?: string }): Observable<PaymentRecord> {
    return this.api.post<PaymentRecord>(`payrolls/${id}/pay`, paymentData);
  }

  simulate(employeeId: number, periodStart: string, periodEnd: string, customDeductions?: any[]): Observable<PayrollSimulation> {
    return this.api.post<PayrollSimulation>('payrolls/simulate', {
      employeeId, periodStart, periodEnd, customDeductions
    });
  }

  validatePeriod(periodStart: string, periodEnd: string): Observable<{ valid: boolean; message?: string }> {
    return this.api.post<{ valid: boolean; message?: string }>('payrolls/validate-period', {
      periodStart, periodEnd
    });
  }

  // ─── Empleados ───────────────────────────────────────────────
  getEmployees(): Observable<Employee[]> {
    return this.api.get<Employee[]>('employees');
  }

  getEmployee(id: number): Observable<Employee> {
    return this.api.get<Employee>(`employees/${id}`);
  }

  createEmployee(employee: Omit<Employee, 'id'>): Observable<Employee> {
    return this.api.post<Employee>('employees', employee);
  }

  updateEmployee(id: number, employee: Partial<Employee>): Observable<Employee> {
    return this.api.put<Employee>(`employees/${id}`, employee);
  }

  // ─── Deducciones ─────────────────────────────────────────────
  getDeductions(): Observable<Deduction[]> {
    return this.api.get<Deduction[]>('deductions');
  }

  createDeduction(deduction: Omit<Deduction, 'id'>): Observable<Deduction> {
    return this.api.post<Deduction>('deductions', deduction);
  }

  updateDeduction(id: number, deduction: Partial<Deduction>): Observable<Deduction> {
    return this.api.put<Deduction>(`deductions/${id}`, deduction);
  }

  deleteDeduction(id: number): Observable<void> {
    return this.api.delete(`deductions/${id}`);
  }

  // ─── Seguridad Social ────────────────────────────────────────
  getSocialSecurityPayments(): Observable<SocialSecurityPayment[]> {
    return this.api.get<SocialSecurityPayment[]>('social-security/payments');
  }

  getSocialSecurityPaymentsByPeriod(period: string): Observable<SocialSecurityPayment[]> {
    return this.api.get<SocialSecurityPayment[]>(`social-security/payments?period=${period}`);
  }

  paySocialSecurity(payments: Omit<SocialSecurityPayment, 'id'>[]): Observable<SocialSecurityPayment[]> {
    return this.api.post<SocialSecurityPayment[]>('social-security/pay', { payments });
  }

  // ─── Registros de Pago ───────────────────────────────────────
  getPaymentRecords(): Observable<PaymentRecord[]> {
    return this.api.get<PaymentRecord[]>('payment-records');
  }

  getPaymentRecordsByPayroll(payrollId: number): Observable<PaymentRecord[]> {
    return this.api.get<PaymentRecord[]>(`payment-records?payrollId=${payrollId}`);
  }

  // ─── Provisiones (NUEVO) ─────────────────────────────────────
  getProvisions(year?: number, employeeId?: number): Observable<PayrollProvision[]> {
    const params = new URLSearchParams();
    if (year) params.set('year', String(year));
    if (employeeId) params.set('employeeId', String(employeeId));
    return this.api.get<PayrollProvision[]>(`payroll-provisions?${params.toString()}`);
  }

  getProvisionSummary(year?: number): Observable<ProvisionSummary[]> {
    const params = new URLSearchParams();
    if (year) params.set('year', String(year));
    return this.api.get<ProvisionSummary[]>(`payroll-provisions/summary?${params.toString()}`);
  }

  // ─── Liquidaciones (NUEVO) ──────────────────────────────────
  getSettlements(): Observable<PayrollSettlement[]> {
    return this.api.get<PayrollSettlement[]>('payroll-settlements');
  }

  getSettlement(id: number): Observable<PayrollSettlement> {
    return this.api.get<PayrollSettlement>(`payroll-settlements/${id}`);
  }

  simulateSettlement(employeeId: number, settlementDate: string, opts?: { lastDayWorked?: string; variableAverage3Months?: number }): Observable<any> {
    return this.api.post('payroll-settlements/simulate', {
      employeeId, settlementDate, ...opts
    });
  }

  createSettlement(employeeId: number, settlementDate: string, opts?: { lastDayWorked?: string; variableAverage3Months?: number; notes?: string }): Observable<PayrollSettlement> {
    return this.api.post<PayrollSettlement>('payroll-settlements', {
      employeeId, settlementDate, ...opts
    });
  }

  paySettlement(id: number, data: { paymentDate: string; paymentMethod: string; reference?: string }): Observable<any> {
    return this.api.post(`payroll-settlements/${id}/pay`, data);
  }

  // ─── Utilidades ──────────────────────────────────────────────
  validateDeductionAmount(salary: number, totalDeductions: number): boolean {
    return totalDeductions <= salary;
  }

  validateBankAccount(account: string): boolean {
    return !!(account && account.length >= 10 && account.length <= 20 && /^\d+$/.test(account));
  }

  formatCurrency(value: number, currency = 'COP'): string {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value ?? 0);
  }
}
