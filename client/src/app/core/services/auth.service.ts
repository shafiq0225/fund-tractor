import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, Observable, tap, throwError } from 'rxjs';
import { ApiResponse } from '../../shared/models/Amfi/Scheme';
import { NotificationService } from './notification.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  data: {
    token: string;
    expires: string;
    user: User;
  };
}

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  panNumber: string;
  createdAt: string;
  roles: string[];
  permissions: string[];
  isActive: boolean;
  hasAccess?: boolean;
  accessStatus?: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  panNumber: string;
  password: string;
  confirmPassword: string;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
  data: User;
  errors?: string[]; // Add errors array to match ApiResponse<T>

}

export interface UpdateRoleRequest {
  userId: number;
  newRole: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface AdminChangePasswordRequest {
  userId: number;
  newPassword: string;
  confirmPassword: string;
}


export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  panNumber: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);
  apiUrl = 'https://localhost:5001/api';
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    this.loadUserFromStorage();
  }

  login(loginData: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, loginData)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.setAuthData(response.data.token, response.data.user);

            if (response.data.user.id) {
              this.notificationService.getUnreadCount(response.data.user.id);
            }
          }
        })
      );
  }

  register(registerData: RegisterRequest): Observable<ApiResponse<User>> {
    console.log('📤 Sending registration request:', registerData);
    return this.http.post<ApiResponse<User>>(`${this.apiUrl}/auth/register`, registerData)
      .pipe(
        tap(response => console.log('📥 Registration API response:', response)),
        catchError(error => {
          console.log('🚨 Registration API error:', error);
          // Re-throw the error so the component can handle it
          return throwError(() => error);
        })
      );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const isExpired = payload.exp * 1000 < Date.now();
      if (isExpired) {
        this.logout();
        return false;
      }
      return true;
    } catch {
      return false;
    }
  }

  hasRole(role: string): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.roles.includes(role) : false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.currentUserSubject.value;
    return user ? roles.some(role => user.roles.includes(role)) : false;
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.permissions.includes(permission) : false;
  }

  // Check if user has access (active and has roles)
  hasAccess(): boolean {
    const user = this.currentUserSubject.value;
    return user ? user.isActive && user.roles.length > 0 : false;
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  private setAuthData(token: string, user: User): void {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private loadUserFromStorage(): void {
    const token = this.getToken();
    const userData = localStorage.getItem('user');

    if (token && userData) {
      try {
        const user = JSON.parse(userData);
        if (this.isTokenValid(token)) {
          this.currentUserSubject.next(user);
        } else {
          this.logout();
        }
      } catch (error) {
        console.error('Error parsing user data from localStorage', error);
        this.logout();
      }
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  // Get all users
  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/auth/users`);
  }

  // Get user by ID
  getUserById(id: number): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/auth/users/${id}`);
  }

  // Update user role
  updateUserRole(userId: number, newRole: string): Observable<any> {
    const updateRoleDto: UpdateRoleRequest = {
      userId,
      newRole
    };
    return this.http.put(`${this.apiUrl}/auth/users/role`, updateRoleDto);
  }

  // Delete user
  deleteUser(userId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/auth/users/${userId}`);
  }

  // Refresh current user data
  refreshCurrentUser(): void {
    const userData = localStorage.getItem('user');
    if (userData) {
      try {
        const user = JSON.parse(userData);
        this.currentUserSubject.next(user);
      } catch (error) {
        console.error('Error refreshing user data', error);
      }
    }
  }

  changePassword(changePasswordData: ChangePasswordRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(
      `${this.apiUrl}/auth/change-password`,
      changePasswordData
    );
  }

  // Admin change password for other users
  adminChangePassword(adminChangePasswordData: AdminChangePasswordRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(
      `${this.apiUrl}/auth/admin/change-password`,
      adminChangePasswordData
    );
  }
  // Add this method to your AuthService class
  createUser(userData: CreateUserRequest): Observable<ApiResponse<User>> {
    return this.http.post<ApiResponse<User>>(`${this.apiUrl}/auth/admin/create-user`, userData);
  }
}