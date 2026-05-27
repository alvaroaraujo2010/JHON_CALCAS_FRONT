import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { User } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  items = signal<User[]>([]);
  showForm = false;

  form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    role: ['Vendedor', Validators.required]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.api.loadList<User[]>('users', d => this.items.set(d)).subscribe();
  }

  save() {
    if (this.form.invalid) return;
    this.api
      .run(this.api.post('users', this.form.getRawValue()), {
        success: 'Usuario creado correctamente',
        error: 'No se pudo crear el usuario'
      })
      .subscribe({
        next: () => {
          this.showForm = false;
          this.form.reset({ role: 'Vendedor' });
          this.reload();
        }
      });
  }
}
