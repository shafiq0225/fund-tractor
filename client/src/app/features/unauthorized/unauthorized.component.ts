import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.scss']
})
export class UnauthorizedComponent implements OnInit {
  reason: string = 'unknown';

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.reason = params['reason'] || 'unknown';
      console.log('Unauthorized reason:', this.reason);
    });
  }

  getTitle(): string {
    switch (this.reason) {
      case 'inactive':
        return 'Account Inactive';
      case 'no-roles':
        return 'No Roles Assigned';
      case 'insufficient-permissions':
        return 'Access Restricted';
      default:
        return 'Unauthorized Access';
    }
  }

  getMessage(): string {
    switch (this.reason) {
      case 'inactive':
        return 'Your account has been deactivated. Please contact your administrator to reactivate your account.';
      case 'no-roles':
        return 'No roles have been assigned to your account. Please contact your administrator to assign appropriate roles.';
      case 'insufficient-permissions':
        return 'You don\'t have the required permissions to access this page.';
      default:
        return 'You are not authorized to access this page.';
    }
  }

  getIcon(): string {
    switch (this.reason) {
      case 'inactive':
        return 'person_off';
      case 'no-roles':
        return 'admin_panel_settings';
      case 'insufficient-permissions':
        return 'lock';
      default:
        return 'warning';
    }
  }

  getIconContainerClass(): string {
    switch (this.reason) {
      case 'inactive':
        return 'bg-orange-100';
      case 'no-roles':
        return 'bg-blue-100';
      case 'insufficient-permissions':
        return 'bg-red-100';
      default:
        return 'bg-gray-100';
    }
  }

  getIconColorClass(): string {
    switch (this.reason) {
      case 'inactive':
        return 'text-orange-500';
      case 'no-roles':
        return 'text-blue-500';
      case 'insufficient-permissions':
        return 'text-red-500';
      default:
        return 'text-gray-500';
    }
  }

  getBadgeClass(): string {
    switch (this.reason) {
      case 'inactive':
        return 'bg-orange-500';
      case 'no-roles':
        return 'bg-blue-500';
      case 'insufficient-permissions':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  }

  goToDashboard(): void {
    this.router.navigate(['/']);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  contactAdmin(): void {
    const adminEmail = 'admin@fundtrackr.com';
    const subject = 'Account Access Issue - ' + this.getTitle();
    const body = `Hello,\n\nI'm experiencing an issue with my account access:\n\n- Issue: ${this.getTitle()}\n- Description: ${this.getMessage()}\n\nPlease assist with resolving this matter.\n\nThank you.`;
    
    window.location.href = `mailto:${adminEmail}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
  }
}