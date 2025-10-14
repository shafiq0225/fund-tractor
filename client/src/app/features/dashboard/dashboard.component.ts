// features/dashboard/dashboard.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {
  mainTiles = [
    { 
      title: 'Portfolio Overview', 
      icon: 'account_balance', 
      route: '/portfolio',
      color: 'bg-gradient-to-br from-blue-500 to-blue-600',
      description: 'View your investment portfolio'
    },
    { 
      title: 'My Funds', 
      icon: 'pie_chart', 
      route: '/funds',
      color: 'bg-gradient-to-br from-green-500 to-green-600',
      description: 'Manage your mutual funds'
    },
    { 
      title: 'Transactions', 
      icon: 'receipt', 
      route: '/transactions',
      color: 'bg-gradient-to-br from-purple-500 to-purple-600',
      description: 'View transaction history'
    },
    { 
      title: 'Performance', 
      icon: 'trending_up', 
      route: '/performance',
      color: 'bg-gradient-to-br from-orange-500 to-orange-600',
      description: 'Track investment performance'
    },
    { 
      title: 'NAV Management', 
      icon: 'table_chart', 
      route: '/nav/dashboard',
      color: 'bg-gradient-to-br from-red-500 to-red-600',
      description: 'Manage NAV calculations'
    }
  ];
}