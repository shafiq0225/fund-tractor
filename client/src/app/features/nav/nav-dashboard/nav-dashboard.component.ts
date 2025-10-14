// features/nav/nav-dashboard/nav-dashboard.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-nav-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './nav-dashboard.component.html',
  styleUrls: ['./nav-dashboard.component.scss']
})
export class NavDashboardComponent {
  tiles = [
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
}