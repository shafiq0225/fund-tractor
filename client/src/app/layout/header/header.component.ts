import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { Subscription } from 'rxjs';
import { NotificationComponent } from "../../features/notification/notification.component";

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, NotificationComponent],
  templateUrl: './header.component.html'
})
export class HeaderComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);

  private authSubscription!: Subscription;

  isLoggedIn = false;
  isUserMenuOpen = false;
  userEmail: string = '';
  userInitials: string = '';
  userId: number = 0;

  // Notification state
  isNotificationsOpen: boolean = false;

  ngOnInit(): void {
    // Subscribe to authentication state changes
    this.authSubscription = this.authService.currentUser$.subscribe(user => {
      this.isLoggedIn = !!user;
      if (user) {
        this.userEmail = user.email;
        this.userInitials = this.getUserInitials(user.firstName, user.lastName);
        this.userId = user.id;
      } else {
        this.userEmail = '';
        this.userInitials = '';
        this.userId = 0;
      }
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', this.onDocumentClick.bind(this));
    // Close dropdown when pressing escape key
    document.addEventListener('keydown', this.onKeydown.bind(this));
  }

  ngOnDestroy(): void {
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
    document.removeEventListener('click', this.onDocumentClick.bind(this));
    document.removeEventListener('keydown', this.onKeydown.bind(this));
  }

  onLogout(): void {
    this.authService.logout();
    this.isUserMenuOpen = false;
    this.router.navigate(['/auth/login']);
  }

  private getUserInitials(firstName: string, lastName: string): string {
    return `${firstName?.charAt(0) || ''}${lastName?.charAt(0) || ''}`.toUpperCase();
  }

  private onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;

    // Check if click is outside user menu
    const userMenuButton = target.closest('[data-user-menu-button]');
    const userMenuDropdown = target.closest('[data-user-menu-dropdown]');

    // Close user menu if click is outside
    if (this.isUserMenuOpen && !userMenuButton && !userMenuDropdown) {
      this.isUserMenuOpen = false;
    }
  }

  private onKeydown(event: KeyboardEvent): void {
    // Close all menus when escape key is pressed
    if (event.key === 'Escape') {
      this.closeAllMenus();
    }
  }

  closeAllMenus(): void {
    this.isUserMenuOpen = false;
    this.isNotificationsOpen = false;
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
    // Close notifications if open
    if (this.isNotificationsOpen) {
      this.isNotificationsOpen = false;
    }
  }

  onNotificationsOpenChange(isOpen: boolean): void {
    this.isNotificationsOpen = isOpen;
    // Close user menu if open
    if (this.isUserMenuOpen) {
      this.isUserMenuOpen = false;
    }
  }
}