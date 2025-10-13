import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-nav-dashboard',
  imports: [RouterLink,
    MatCardModule, MatIconModule, CommonModule],
  templateUrl: './nav-dashboard.component.html',
  styleUrl: './nav-dashboard.component.scss'
})
export class NavDashboardComponent {
  tiles = [
    { title: 'Import Nav', icon: 'upload_file', route: 'import', color: 'bg-blue-500' },
    { title: 'Scheme Management', icon: 'list_alt', route: 'manage', color: 'bg-green-500' },
    { title: 'Report', icon: 'table_chart', route: 'report', color: 'bg-purple-500' },
    { title: 'Compare', icon: 'compare', route: 'compare', color: 'bg-orange-500' }
  ];

}
