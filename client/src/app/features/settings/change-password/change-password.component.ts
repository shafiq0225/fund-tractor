// features/settings/change-password/change-password.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { BreadcrumbComponent } from '../../../shared/components/breadcrumb/breadcrumb.component';
import { AuthService, ChangePasswordRequest } from '../../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, BreadcrumbComponent],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  passwordData: ChangePasswordRequest = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  loading = false;
  error = '';
  successMessage = '';
  fieldErrors: { [key: string]: string } = {};

  onSubmit() {
    this.error = '';
    this.successMessage = '';
    this.fieldErrors = {};

    // Client-side validation
    if (!this.passwordData.currentPassword) {
      this.fieldErrors['currentPassword'] = 'Current password is required';
    }

    if (!this.passwordData.newPassword) {
      this.fieldErrors['newPassword'] = 'New password is required';
    } else if (this.passwordData.newPassword.length < 6) {
      this.fieldErrors['newPassword'] = 'Password must be at least 6 characters long';
    }

    if (!this.passwordData.confirmPassword) {
      this.fieldErrors['confirmPassword'] = 'Confirm password is required';
    } else if (this.passwordData.newPassword !== this.passwordData.confirmPassword) {
      this.fieldErrors['confirmPassword'] = 'New password and confirmation do not match';
    }

    if (Object.keys(this.fieldErrors).length > 0) {
      return;
    }

    this.loading = true;

    this.authService.changePassword(this.passwordData).subscribe({
      next: (response) => {
        this.loading = false;
        
        if (response.success) {
          this.successMessage = response.message || 'Password updated successfully!';
          
          // Clear form
          this.passwordData = {
            currentPassword: '',
            newPassword: '',
            confirmPassword: ''
          };

          // Redirect back to settings after 2 seconds
          setTimeout(() => {
            this.router.navigate(['/settings']);
          }, 2000);
        } else {
          this.error = response.message || 'Failed to update password';
        }
      },
      error: (err) => {
        this.loading = false;
        
        if (err.error?.errors && Array.isArray(err.error.errors)) {
          // Handle field-level errors from API
          const apiErrors = err.error.errors;
          apiErrors.forEach((errorMsg: string) => {
            if (errorMsg.toLowerCase().includes('current password')) {
              this.fieldErrors['currentPassword'] = errorMsg;
            } else if (errorMsg.toLowerCase().includes('new password')) {
              this.fieldErrors['newPassword'] = errorMsg;
            } else if (errorMsg.toLowerCase().includes('confirm')) {
              this.fieldErrors['confirmPassword'] = errorMsg;
            } else {
              this.error = errorMsg;
            }
          });
        } else {
          this.error = err.error?.message || 'Failed to update password. Please try again.';
        }
        
        console.error('Error changing password:', err);
      }
    });
  }

  onCancel() {
    this.router.navigate(['/settings']);
  }

  showFieldError(fieldName: string): boolean {
    return !!this.fieldErrors[fieldName];
  }

  getFieldError(fieldName: string): string {
    return this.fieldErrors[fieldName] || '';
  }
}