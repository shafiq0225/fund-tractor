import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService, NotificationDto } from '../../core/services/notification.service';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule],
  templateUrl: './header.component.html'
})
export class HeaderComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  private authSubscription!: Subscription;
  private notificationSubscription!: Subscription;
  private unreadCountSubscription!: Subscription;
  private refreshIntervalSubscription!: Subscription;

  isLoggedIn = false;
  isUserMenuOpen = false;
  userEmail: string = '';
  userInitials: string = '';
  userId: number = 0;

  // Notification properties
  isNotificationsOpen: boolean = false;
  unreadNotifications: number = 0;
  notifications: NotificationDto[] = [];
  isLoadingNotifications = false;

  ngOnInit(): void {
    // Subscribe to authentication state changes
    this.authSubscription = this.authService.currentUser$.subscribe(user => {
      this.isLoggedIn = !!user;
      if (user) {
        this.userEmail = user.email;
        this.userInitials = this.getUserInitials(user.firstName, user.lastName);
        this.userId = user.id;

        // Load notifications when user is logged in
        this.loadNotifications();
      } else {
        this.userEmail = '';
        this.userInitials = '';
        this.userId = 0;
        this.stopNotificationPolling();
        this.clearNotifications();
      }
    });

    // Subscribe to notification updates
    this.notificationSubscription = this.notificationService.notifications$
      .subscribe(notifications => {
        this.notifications = notifications;
      });

    // Subscribe to unread count updates
    this.unreadCountSubscription = this.notificationService.unreadCount$
      .subscribe(count => {
        this.unreadNotifications = count;
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
    if (this.notificationSubscription) {
      this.notificationSubscription.unsubscribe();
    }
    if (this.unreadCountSubscription) {
      this.unreadCountSubscription.unsubscribe();
    }
    this.stopNotificationPolling();

    document.removeEventListener('click', this.onDocumentClick.bind(this));
    document.removeEventListener('keydown', this.onKeydown.bind(this));
  }

  private loadNotifications(): void {
    if (this.userId) {
      this.isLoadingNotifications = true;
      this.notificationService.getUserNotifications(this.userId).subscribe({
        next: (response) => {
          // Notifications are automatically updated via the service observable
        },
        error: (error) => {
          console.error('Failed to load notifications:', error);
        },
        complete: () => {
          this.isLoadingNotifications = false;
        }
      });

      // Load unread count separately
      this.notificationService.getUnreadCount(this.userId).subscribe();
    }
  }

  private stopNotificationPolling(): void {
    if (this.refreshIntervalSubscription) {
      this.refreshIntervalSubscription.unsubscribe();
    }
  }

  private clearNotifications(): void {
    this.notifications = [];
    this.unreadNotifications = 0;
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

    // Check if click is outside notifications
    const notificationsButton = target.closest('[data-notifications-button]');
    const notificationsDropdown = target.closest('[data-notifications-dropdown]');

    // Close user menu if click is outside
    if (this.isUserMenuOpen && !userMenuButton && !userMenuDropdown) {
      this.isUserMenuOpen = false;
    }

    // Close notifications if click is outside
    if (this.isNotificationsOpen && !notificationsButton && !notificationsDropdown) {
      this.isNotificationsOpen = false;
    }
  }

  private onKeydown(event: KeyboardEvent): void {
    // Close all menus when escape key is pressed
    if (event.key === 'Escape') {
      this.closeAllMenus();
    }
  }

  // Notification methods
  toggleNotifications(): void {
    this.isNotificationsOpen = !this.isNotificationsOpen;
    // Close user menu if open
    if (this.isUserMenuOpen) {
      this.isUserMenuOpen = false;
    }

    // Refresh notifications when opening dropdown
    if (this.isNotificationsOpen && this.userId) {
      this.notificationService.refreshNotifications(this.userId);
    }
  }

  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe();
    }

    // Handle notification click based on type
    this.handleNotificationAction(notification);
    this.isNotificationsOpen = false;
  }

  markAllAsRead(): void {
    const unreadNotifications = this.notifications.filter(n => !n.isRead);
    if (unreadNotifications.length > 0) {
      this.notificationService.markAllAsRead(unreadNotifications);
    }
  }

  private handleNotificationAction(notification: NotificationDto): void {
    // Navigate based on notification type or metadata
    switch (notification.type) {
      case 'role_update':
        this.navigateToProfile();
        break;
      case 'investment_update':
        this.navigateToInvestments();
        break;
      case 'portfolio_update':
        this.navigateToPortfolio();
        break;
      default:
        // Default action or no navigation
        break;
    }
  }

  // Public navigation methods for template
  navigateToNotifications(): void {
    this.router.navigate(['/notifications']);
    this.isNotificationsOpen = false;
  }

  navigateToProfile(): void {
    this.router.navigate(['/profile']);
  }

  navigateToInvestments(): void {
    this.router.navigate(['/investments']);
  }

  navigateToPortfolio(): void {
    this.router.navigate(['/portfolio']);
  }

  getNotificationIcon(type: string): string {
    switch (type) {
      case 'role_update':
        return 'security';
      case 'investment_update':
        return 'trending_up';
      case 'portfolio_update':
        return 'account_balance';
      case 'system':
        return 'info';
      default:
        return 'notifications';
    }
  }

  getNotificationIconColor(type: string): string {
    switch (type) {
      case 'role_update':
        return 'text-purple-600';
      case 'investment_update':
        return 'text-green-600';
      case 'portfolio_update':
        return 'text-blue-600';
      case 'system':
        return 'text-yellow-600';
      default:
        return 'text-gray-600';
    }
  }

  getNotificationIconBgColor(type: string): string {
    switch (type) {
      case 'role_update':
        return 'bg-purple-100';
      case 'investment_update':
        return 'bg-green-100';
      case 'portfolio_update':
        return 'bg-blue-100';
      case 'system':
        return 'bg-yellow-100';
      default:
        return 'bg-gray-100';
    }
  }

  getNotificationTime(createdAt: string): string {
    const created = new Date(createdAt);
    const now = new Date();
    const diffMs = now.getTime() - created.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return created.toLocaleDateString();
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
}