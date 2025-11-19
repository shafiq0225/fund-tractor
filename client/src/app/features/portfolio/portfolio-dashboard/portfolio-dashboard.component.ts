import { Component, OnInit } from '@angular/core';
import { AuthService, User } from '../../../core/services/auth.service';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-portfolio-dashboard',
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './portfolio-dashboard.component.html',
  styleUrl: './portfolio-dashboard.component.scss'
})
export class PortfolioDashboardComponent implements OnInit {
  tiles: any[] = [];
  currentUser: User | null = null;
  hasAdminAccess: boolean = false;

  constructor(private authService: AuthService, private router: Router) { }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.checkUserRole();
    this.setupTiles();
  }
  private loadCurrentUser(): void {
    this.currentUser = this.authService.getCurrentUser();
  }

  private checkUserRole(): void {
    this.hasAdminAccess = this.authService.hasRole('Admin') ||
      this.authService.hasRole('Employee');
  }

  private setupTiles(): void {
    const allTiles = [
      {
        title: 'Create Investment',
        icon: 'add_circle',
        route: '/portfolio/create-investment',
        color: 'bg-gradient-to-br from-green-500 to-green-600',
        description: 'Add new investment records',
        roles: ['Admin', 'Employee'] // Only show for Admin and Employee
      },
      {
        title: 'My Investments',
        icon: 'list_alt',
        route: '/portfolio/my-investments',
        color: 'bg-gradient-to-br from-blue-500 to-blue-600',
        description: 'View all your investments',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] // Show for all authenticated users
      },
      {
        title: 'Portfolio Summary',
        icon: 'pie_chart',
        route: '/portfolio/summary',
        color: 'bg-gradient-to-br from-purple-500 to-purple-600',
        description: 'Overall portfolio performance',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] // Show for all authenticated users
      },
      {
        title: 'Investment Analysis',
        icon: 'analytics',
        route: '/portfolio/analysis',
        color: 'bg-gradient-to-br from-orange-500 to-orange-600',
        description: 'Detailed investment analysis',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] // Show for all authenticated users
      },
      {
        title: 'Transaction History',
        icon: 'history',
        route: '/portfolio/transactions',
        color: 'bg-gradient-to-br from-red-500 to-red-600',
        description: 'View transaction records',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] // Show for all authenticated users
      }
    ];

    // Filter tiles based on user roles
    this.tiles = allTiles.filter(tile =>
      this.authService.hasAnyRole(tile.roles)
    );
  }

  // Helper methods for template
  isFamilyMember(): boolean {
    return this.authService.hasRole('FamilyMember');
  }

  isHeadOfFamily(): boolean {
    return this.authService.hasRole('HeadOfFamily');
  }

  isAdminOrEmployee(): boolean {
    return this.authService.hasRole('Admin') || this.authService.hasRole('Employee');
  }

  getUserRoles(): string[] {
    return this.currentUser?.roles || [];
  }

}
