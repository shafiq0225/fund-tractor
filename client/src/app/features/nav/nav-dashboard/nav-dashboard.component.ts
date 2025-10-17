import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-nav-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './nav-dashboard.component.html',
  styleUrls: ['./nav-dashboard.component.scss']
})
export class NavDashboardComponent implements OnInit {
  tiles: any[] = [];
  hasRestrictedAccess: boolean = false;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.checkUserRole();
    this.setupTiles();
  }

  private checkUserRole(): void {
    // Check if user has either FamilyMember OR HeadOfFamily role
    this.hasRestrictedAccess = this.authService.hasRole('FamilyMember') || 
                              this.authService.hasRole('HeadOfFamily');
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

    // If user has restricted access, filter out Import Data and Manage Schemes
    if (this.hasRestrictedAccess) {
      this.tiles = allTiles.filter(tile => 
        tile.title !== 'Import Data' && tile.title !== 'Manage Schemes'
      );
    } else {
      this.tiles = allTiles;
    }
  }
}