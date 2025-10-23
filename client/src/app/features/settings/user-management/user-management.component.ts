// features/admin/user-management/user-management.component.ts
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { BreadcrumbComponent } from '../../../shared/components/breadcrumb/breadcrumb.component';
import { AuthService, User } from '../../../core/services/auth.service';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, BreadcrumbComponent],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent implements OnInit {
  private authService = inject(AuthService);

  users: (User & { newRole?: string })[] = [];
  loading = false;
  error = '';
  successMessage = '';

  ngOnInit() {
    this.loadUsers();
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