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
  { title: 'Users', icon: 'group', route: '/users', color: 'bg-purple-500' },
  { title: 'Settings', icon: 'settings', route: '/settings', color: 'bg-orange-500' },
  { title: 'Billing', icon: 'payments', route: '/billing', color: 'bg-pink-500' },
];

}
