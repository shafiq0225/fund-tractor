import { Component, inject, OnInit } from '@angular/core';
import { SidebarComponent } from "./sidebar/sidebar.component";
import { HeaderComponent } from "./header/header.component";
import { MatProgressBar } from '@angular/material/progress-bar';
import { BusyService } from '../core/services/busy.service';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-layout',
  imports: [SidebarComponent, HeaderComponent, MatProgressBar, RouterOutlet],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit {
  busyService = inject(BusyService);
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser: any;
  isAdminOrEmployee = false;

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.isAdminOrEmployee = this.authService.hasAnyRole(['Admin', 'Employee']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  canAccessNavManagement(): boolean {
    return this.authService.hasAnyRole(['Admin', 'Employee', 'HeadOfFamily']);
  }

}
