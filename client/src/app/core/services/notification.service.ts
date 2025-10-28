import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';

export interface NotificationDto {
    id: number;
    userId: number;
    title: string;
    message: string;
    type: string;
    isRead: boolean;
    metadata?: { [key: string]: any };
    createdAt: string;
    readAt?: string;
}

export interface ApiResponse<T> {
    success: boolean;
    message?: string;
    data?: T;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private http = inject(HttpClient);
    apiUrl = 'https://localhost:5001/api/notifications';

    private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
    private unreadCountSubject = new BehaviorSubject<number>(0);

    public notifications$ = this.notificationsSubject.asObservable();
    public unreadCount$ = this.unreadCountSubject.asObservable();

    getUserNotifications(userId: number): Observable<ApiResponse<NotificationDto[]>> {
        return this.http.get<ApiResponse<NotificationDto[]>>(`${this.apiUrl}/user/${userId}`)
            .pipe(
                tap(response => {
                    if (response.success && response.data) {
                        this.notificationsSubject.next(response.data);
                        this.updateUnreadCount(response.data);
                    }
                })
            );
    }

    getUnreadCount(userId: number): Observable<ApiResponse<number>> {
        return this.http.get<ApiResponse<number>>(`${this.apiUrl}/unread-count/${userId}`)
            .pipe(
                tap(response => {
                    if (response.success && response.data !== undefined) {
                        this.unreadCountSubject.next(response.data);
                    }
                })
            );
    }

    markAsRead(notificationId: number): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(`${this.apiUrl}/${notificationId}/read`, {})
            .pipe(
                tap(response => {
                    if (response.success) {
                        // Update local state
                        const currentNotifications = this.notificationsSubject.value;
                        const updatedNotifications = currentNotifications.map(notification =>
                            notification.id === notificationId
                                ? { ...notification, isRead: true, readAt: new Date().toISOString() }
                                : notification
                        );
                        this.notificationsSubject.next(updatedNotifications);
                        this.updateUnreadCount(updatedNotifications);
                    }
                })
            );
    }

    markAllAsRead(notifications: NotificationDto[]): Observable<ApiResponse<any>>[] {
        const unreadNotifications = notifications.filter(n => !n.isRead);
        return unreadNotifications.map(notification =>
            this.markAsRead(notification.id)
        );
    }

    refreshNotifications(userId: number): void {
        this.getUserNotifications(userId).subscribe();
        this.getUnreadCount(userId).subscribe();
    }

    private updateUnreadCount(notifications: NotificationDto[]): void {
        const unreadCount = notifications.filter(n => !n.isRead).length;
        this.unreadCountSubject.next(unreadCount);
    }

    // Add a notification to the local state (for real-time updates)
    addNotification(notification: NotificationDto): void {
        const currentNotifications = this.notificationsSubject.value;
        this.notificationsSubject.next([notification, ...currentNotifications]);
        this.updateUnreadCount([notification, ...currentNotifications]);
    }
}