// features/settings/settings-dashboard.component.ts
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIcon } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-settings-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIcon],
  templateUrl: './settings-dashboard.component.html',
  styleUrls: ['./settings-dashboard.component.scss']
})
export class SettingsDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  
  isAdmin = false;
  totalUsers = 0;
  adminCount = 0;

  ngOnInit() {
    this.isAdmin = this.authService.hasRole('Admin');
    // You can load actual stats here if needed
    this.loadStats();
  }

  loadStats() {
    // This is a placeholder - you can implement actual stats loading
    if (this.isAdmin) {
      // For demo purposes, setting some default values
      this.totalUsers = 24;
      this.adminCount = 3;
    }
  }
}