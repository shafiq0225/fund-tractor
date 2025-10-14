// login.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AuthService } from '../../core/services/auth.service';

// Custom validator for password confirmation
export function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (!password || !confirmPassword) {
    return null;
  }

  return password.value === confirmPassword.value ? null : { passwordMismatch: true };
}

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
    MatProgressSpinnerModule,
    MatCheckboxModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isSignup = false;
  hidePassword = true;
  hideConfirmPassword = true;
  isLoading = false;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private snackBar: MatSnackBar,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    // If already logged in, redirect to dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
    }

    this.initForm();
  }

  // Add to your login.component.ts

  // Update the form initialization
  initForm(): void {
    if (this.isSignup) {
      // Signup form
      this.loginForm = this.formBuilder.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        panNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/)]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
        agreeToTerms: [false, Validators.requiredTrue]
      }, { validators: passwordMatchValidator });
    } else {
      // Login form
      this.loginForm = this.formBuilder.group({
        email: ['demo@example.com', [Validators.required, Validators.email]],
        password: ['password', Validators.required]
      });
    }
  }

  // Add PAN input formatting method
  onPanInput(event: any): void {
    const input = event.target;
    const value = input.value.toUpperCase();
    input.value = value;

    // Update form control value
    this.loginForm.patchValue({
      panNumber: value
    });
  }

  // Add method to validate PAN format
  validatePanFormat(pan: string): boolean {
    const panRegex = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/;
    return panRegex.test(pan);
  }

  toggleMode(): void {
    this.isSignup = !this.isSignup;
    this.initForm();
    this.hidePassword = true;
    this.hideConfirmPassword = true;
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      this.isLoading = true;

      if (this.isSignup) {
        this.handleSignup();
      } else {
        this.handleLogin();
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.loginForm.controls).forEach(key => {
        this.loginForm.get(key)?.markAsTouched();
      });
    }
  }

  private handleLogin(): void {
    const { email, password } = this.loginForm.value;

    // Simulate API call
    setTimeout(() => {
      if (this.authService.login(email, password)) {
        this.snackBar.open('Login successful!', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        this.router.navigate(['/']);
      } else {
        this.snackBar.open('Invalid credentials!', 'Close', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
      }
      this.isLoading = false;
    }, 1500);
  }

  private handleSignup(): void {
    const { firstName, lastName, email, password } = this.loginForm.value;

    // Simulate API call for signup
    setTimeout(() => {
      // In a real app, you would call your signup API here
      console.log('Signup attempt with:', { firstName, lastName, email, password });

      // For demo purposes, automatically log the user in after signup
      if (this.authService.login(email, password)) {
        this.snackBar.open('Account created successfully!', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        this.router.navigate(['/']);
      } else {
        this.snackBar.open('Error creating account. Please try again.', 'Close', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
      }
      this.isLoading = false;
    }, 2000);
  }

  // Add to login.component.ts
  getPasswordStrengthClass(level: number): string {
    const password = this.loginForm.get('password')?.value || '';
    const strength = this.calculatePasswordStrength(password);

    if (level <= strength) {
      switch (strength) {
        case 1: return 'bg-red-400';
        case 2: return 'bg-orange-400';
        case 3: return 'bg-yellow-400';
        case 4: return 'bg-green-400';
        default: return 'bg-gray-200';
      }
    }
    return 'bg-gray-200';
  }

  calculatePasswordStrength(password: string): number {
    if (!password) return 0;

    let strength = 0;
    if (password.length >= 6) strength++;
    if (password.match(/[a-z]/) && password.match(/[A-Z]/)) strength++;
    if (password.match(/\d/)) strength++;
    if (password.match(/[^a-zA-Z\d]/)) strength++;

    return strength;
  }
}