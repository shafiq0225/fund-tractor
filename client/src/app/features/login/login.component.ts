import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService, LoginRequest } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  private authService = inject(AuthService);

  loginForm!: FormGroup;
  hidePassword = true;
  isLoading = false;
  returnUrl: string = '/';
  serverErrorMessage: string = '';
  hasServerError: boolean = false;

  ngOnInit(): void {
    // this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

    if (this.authService.isLoggedIn()) {
      this.router.navigate([this.returnUrl]);
    }

    this.initForm();
  }

  initForm(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]] // Added minLength validator
    });

    this.loginForm.valueChanges.subscribe(() => {
      this.clearServerErrors();
    });
  }

  clearServerErrors(): void {
    this.hasServerError = false;
    this.serverErrorMessage = '';
  }

  navigateToSignup(): void {
    this.router.navigate(['signup']);
  }

  // Add this method to check password validity on blur
  checkPasswordValidity(): void {
    const passwordControl = this.loginForm.get('password');
    if (passwordControl && passwordControl.value && passwordControl.value.length < 6) {
      passwordControl.markAsTouched();
      passwordControl.setErrors({ 'minlength': true });
    }
  }

  onSubmit(): void {
    this.clearServerErrors();

    if (this.loginForm.valid) {
      this.isLoading = true;
      this.handleLogin();
    } else {
      Object.keys(this.loginForm.controls).forEach(key => {
        this.loginForm.get(key)?.markAsTouched();
      });
    }
  }

  private handleLogin(): void {
    const loginData: LoginRequest = this.loginForm.value;

    this.authService.login(loginData).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.snackBar.open('Login successful!', 'Close', {
            duration: 3000,
            panelClass: ['success-snackbar']
          });
          this.router.navigate([this.returnUrl]);
        } else {
          this.hasServerError = true;
          this.serverErrorMessage = response.message || 'Login failed!';
        }
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Login error:', error);
        this.hasServerError = true;
        this.serverErrorMessage = error.error?.message || 'Login failed. Please try again.';
      }
    });
  }

  // Helper methods for template
  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}