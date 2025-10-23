// features/settings/change-password/change-password.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { BreadcrumbComponent } from '../../../shared/components/breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, BreadcrumbComponent],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {
  private router = inject(Router);

  passwordData = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  loading = false;
  error = '';
  successMessage = '';

  onSubmit() {
    this.error = '';
    this.successMessage = '';

    // Basic validation
    if (this.passwordData.newPassword !== this.passwordData.confirmPassword) {
      this.error = 'New password and confirmation do not match.';
      return;
    }

    if (this.passwordData.newPassword.length < 6) {
      this.error = 'New password must be at least 6 characters long.';
      return;
    }

    this.loading = true;

    // Simulate API call - replace with actual implementation
    setTimeout(() => {
      this.loading = false;
      this.successMessage = 'Password updated successfully!';

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
    }, 1500);
  }

  onCancel() {
    this.router.navigate(['/settings']);
  }
}