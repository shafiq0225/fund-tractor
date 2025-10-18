import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService, User } from '../../../core/services/auth.service';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-nav-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './nav-dashboard.component.html',
  styleUrls: ['./nav-dashboard.component.scss']
})
export class NavDashboardComponent implements OnInit {
  tiles: any[] = [];
  currentUser: User | null = null;
  hasAdminAccess: boolean = false;

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {}

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
        title: 'Import Data', 
        icon: 'file_upload', 
        route: '/nav/import',
        color: 'bg-gradient-to-br from-blue-500 to-blue-600'
      },
      { 
        title: 'Manage Schemes', 
        icon: 'manage_accounts', 
        route: '/nav/manage',
        color: 'bg-gradient-to-br from-green-500 to-green-600'
      },
      { 
        title: 'Reports', 
        icon: 'assessment', 
        route: '/nav/report',
        color: 'bg-gradient-to-br from-purple-500 to-purple-600'
      },
      { 
        title: 'Compare', 
        icon: 'compare', 
        route: '/nav/compare',
        color: 'bg-gradient-to-br from-red-500 to-red-600'
      }
    ];

    // If user has admin access, show ALL tiles
    // If user is FamilyMember/HeadOfFamily, hide Import Data and Manage Schemes
    if (this.hasAdminAccess) {
      this.tiles = allTiles; // Show all tiles for Admin/Employee
    } else {
      this.tiles = allTiles.filter(tile => 
        tile.title !== 'Import Data' && tile.title !== 'Manage Schemes'
      ); // Hide import/manage for family users
    }
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