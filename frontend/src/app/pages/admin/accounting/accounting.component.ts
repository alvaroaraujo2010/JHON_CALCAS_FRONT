import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Account, JournalEntry, TrialBalance } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-accounting',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, ModuleHeaderComponent],
  templateUrl: './accounting.component.html'
})
export class AccountingComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  tab: 'accounts' | 'journal' | 'trial' = 'accounts';
  accounts = signal<Account[]>([]);
  journal = signal<JournalEntry[]>([]);
  trial = signal<TrialBalance | null>(null);
  showAccountForm = false;
  showJournalForm = false;

  accountForm = this.fb.group({
    code: ['', Validators.required], name: ['', Validators.required],
    type: ['Activo', Validators.required], parentId: [null as number | null], isActive: [true]
  });
  journalForm = this.fb.group({ description: ['', Validators.required], reference: [''] });
  journalLines: { accountId: number; debit: number; credit: number; description: string }[] = [];
  lineForm = this.fb.group({ accountId: [0], debit: [0], credit: [0], description: [''] });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.loadAccounts();
    if (this.tab === 'journal') this.loadJournal();
    if (this.tab === 'trial') this.loadTrial();
  }

  setTab(t: 'accounts' | 'journal' | 'trial') {
    this.tab = t;
    if (t === 'journal') this.loadJournal();
    if (t === 'trial') this.loadTrial();
  }

  loadAccounts() {
    this.api.loadList<Account[]>('accounting/accounts', d => this.accounts.set(d)).subscribe();
  }

  loadJournal() {
    this.api.loadList<JournalEntry[]>('accounting/journal', d => this.journal.set(d)).subscribe();
  }

  loadTrial() {
    this.api.loadList<TrialBalance>('accounting/trial-balance', d => this.trial.set(d)).subscribe();
  }

  saveAccount() {
    if (this.accountForm.invalid) return;
    this.api
      .run(this.api.post('accounting/accounts', this.accountForm.getRawValue()), {
        success: 'Cuenta contable creada',
        error: 'No se pudo crear la cuenta'
      })
      .subscribe({
        next: () => {
          this.showAccountForm = false;
          this.accountForm.reset({ type: 'Activo', isActive: true });
          this.loadAccounts();
        }
      });
  }

  addJournalLine() {
    const v = this.lineForm.getRawValue();
    if (!v.accountId) return;
    this.journalLines.push({ accountId: Number(v.accountId), debit: v.debit || 0, credit: v.credit || 0, description: v.description || '' });
  }

  saveJournal() {
    if (this.journalForm.invalid || !this.journalLines.length) return;
    this.api
      .run(
        this.api.post('accounting/journal', {
          description: this.journalForm.value.description,
          reference: this.journalForm.value.reference,
          lines: this.journalLines
        }),
        {
          success: 'Asiento contable registrado',
          error: 'No se pudo registrar el asiento'
        }
      )
      .subscribe({
        next: () => {
          this.showJournalForm = false;
          this.journalLines = [];
          this.journalForm.reset();
          this.loadJournal();
        }
      });
  }
}
