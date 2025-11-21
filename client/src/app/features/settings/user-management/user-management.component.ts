// Update your user-management.component.ts to include admin password change
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService, AdminChangePasswordRequest, User } from '../../../core/services/auth.service';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { BreadcrumbComponent } from '../../../shared/components/breadcrumb/breadcrumb.component';
import { MatTooltip } from '@angular/material/tooltip';
import { CreateUserModalComponent } from './create-user-modal/create-user-modal.component';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, BreadcrumbComponent, MatTooltip],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent implements OnInit {
  private authService = inject(AuthService);
   private dialog = inject(MatDialog);
  users: (User & { newRole?: string })[] = [];
  loading = false;
  passwordLoading = false;
  error = '';
  successMessage = '';

  // Admin password change modal
  showPasswordModal = false;
  selectedUser: User | null = null;
  adminPasswordData: AdminChangePasswordRequest = {
    userId: 0,
    newPassword: '',
    confirmPassword: ''
  };

  ngOnInit() {
    this.loadUsers();
  }

  onCreateUser() {
    const dialogRef = this.dialog.open(CreateUserModalComponent, {
      width: '500px',
      panelClass: 'custom-dialog-container'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Refresh the user list to show the newly created user
        this.loadUsers();
        this.successMessage = `User "${result.firstName} ${result.lastName}" created successfully!`;
        
        // Clear success message after 3 seconds
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      }
    });
  }


  loadUsers() {
    this.loading = true;
    this.error = '';

    this.authService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users.map(user => ({ ...user, newRole: '' }));
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load users. Please try again.';
        this.loading = false;
        console.error('Error loading users:', err);
      }
    });
  }

  updateUserRole(user: User & { newRole?: string }) {
    if (!user.newRole || user.roles.includes(user.newRole)) {
      return;
    }

    this.authService.updateUserRole(user.id, user.newRole).subscribe({
      next: (response: any) => {
        this.successMessage = `Successfully updated ${user.firstName}'s role to ${user.newRole}`;

        // Update local user data
        user.roles = [user.newRole!];
        user.newRole = '';

        // Clear success message after 3 seconds
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update user role. Please try again.';
        console.error('Error updating user role:', err);
      }
    });
  }

  openPasswordModal(user: User) {
    this.selectedUser = user;
    this.adminPasswordData = {
      userId: user.id,
      newPassword: '',
      confirmPassword: ''
    };
    this.showPasswordModal = true;
  }

  closePasswordModal() {
    this.showPasswordModal = false;
    this.selectedUser = null;
    this.adminPasswordData = {
      userId: 0,
      newPassword: '',
      confirmPassword: ''
    };
  }

  onAdminPasswordSubmit() {
    if (this.adminPasswordData.newPassword !== this.adminPasswordData.confirmPassword) {
      this.error = 'New password and confirmation do not match';
      return;
    }

    if (this.adminPasswordData.newPassword.length < 6) {
      this.error = 'Password must be at least 6 characters long';
      return;
    }

    this.passwordLoading = true;

    this.authService.adminChangePassword(this.adminPasswordData).subscribe({
      next: (response) => {
        this.passwordLoading = false;

        if (response.success) {
          this.successMessage = response.message || 'Password reset successfully!';
          this.closePasswordModal();

          // Clear success message after 3 seconds
          setTimeout(() => {
            this.successMessage = '';
          }, 3000);
        } else {
          this.error = response.message || 'Failed to reset password';
        }
      },
      error: (err) => {
        this.passwordLoading = false;
        this.error = err.error?.message || 'Failed to reset password. Please try again.';
        console.error('Error resetting password:', err);
      }
    });
  }

  deleteUser(userId: number) {
    if (!confirm('Are you sure you want to delete this user? This action cannot be undone.')) {
      return;
    }

    this.authService.deleteUser(userId).subscribe({
      next: (response: any) => {
        this.successMessage = 'User deleted successfully';
        this.loadUsers(); // Reload the user list

        // Clear success message after 3 seconds
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete user. Please try again.';
        console.error('Error deleting user:', err);
      }
    });
  }

  getRoleBadgeClass(role: string): string {
    const classes: { [key: string]: string } = {
      'Admin': 'bg-purple-100 text-purple-800',
      'Employee': 'bg-blue-100 text-blue-800',
      'HeadOfFamily': 'bg-green-100 text-green-800',
      'FamilyMember': 'bg-yellow-100 text-yellow-800'
    };
    return classes[role] || 'bg-gray-100 text-gray-800';
  }

  
}