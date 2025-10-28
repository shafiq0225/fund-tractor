import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService, NotificationDto } from '../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './notification.component.html'
})
export class NotificationComponent implements OnInit, OnDestroy {
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  @Input() userId: number = 0;
  @Input() isOpen: boolean = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() closeAllMenus = new EventEmitter<void>();

  // Notification properties
  unreadNotifications: number = 0;
  notifications: NotificationDto[] = [];
  isLoadingNotifications = false;

  private notificationSubscription!: Subscription;
  private unreadCountSubscription!: Subscription;

  ngOnInit(): void {
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

    // Close dropdown when clicking outside (handled by parent)
    // Close dropdown when pressing escape key
    document.addEventListener('keydown', this.onKeydown.bind(this));
  }

  ngOnDestroy(): void {
    if (this.notificationSubscription) {
      this.notificationSubscription.unsubscribe();
    }
    if (this.unreadCountSubscription) {
      this.unreadCountSubscription.unsubscribe();
    }
    document.removeEventListener('keydown', this.onKeydown.bind(this));
  }

  loadNotifications(): void {
    if (this.userId) {
      this.isLoadingNotifications = true;
      this.notificationService.getUserNotifications(this.userId).subscribe({
        next: () => {
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

  toggleNotifications(): void {
    this.isOpen = !this.isOpen;
    this.isOpenChange.emit(this.isOpen);

    // Refresh notifications when opening dropdown
    if (this.isOpen && this.userId) {
      this.notificationService.refreshNotifications(this.userId);
    }
  }

  closeNotifications(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
  }

  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe();
    }

    // Handle notification click based on type
    this.handleNotificationAction(notification);
    this.closeNotifications();
  }

  markAllAsRead(): void {
    const unreadNotifications = this.notifications.filter(n => !n.isRead);
    if (unreadNotifications.length > 0) {
      this.notificationService.markAllAsRead(unreadNotifications);
    }
  }

  private handleNotificationAction(notification: NotificationDto): void {
    switch (notification.type) {
      case 'role_update':
        this.router.navigate(['/emails']);
        break;
      case 'password_reset':
        this.router.navigate(['/emails']);
        break;
      case 'investment_update':
        this.router.navigate(['/investments']);
        break;
      case 'portfolio_update':
        this.router.navigate(['/portfolio']);
        break;
      default:
        // Default action or no navigation
        break;
    }
  }

  navigateToNotifications(): void {
    this.router.navigate(['/emails']);
    this.closeNotifications();
  }

  getNotificationIcon(type: string): string {
    switch (type) {
      case 'role_update':
        return 'security';
      case 'password_reset':  // Add this
        return 'lock_reset';
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
      case 'password_reset':  // Add this
        return 'text-red-600';
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
      case 'password_reset':  // Add this
        return 'bg-red-100';
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

  private onKeydown(event: KeyboardEvent): void {
    // Close notifications when escape key is pressed
    if (event.key === 'Escape' && this.isOpen) {
      this.closeNotifications();
    }
  }
}