import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
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

  constructor(
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit() {
    // Check if redirected due to unauthorized access
    this.route.queryParams.subscribe(params => {
      if (params['unauthorized']) {
        this.snackBar.open('Access denied to requested page. You do not have permission.', 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }
}