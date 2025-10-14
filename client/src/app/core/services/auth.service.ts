// services/auth.service.ts
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

export interface User {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private isAuthenticated = false;
  private users: User[] = []; // In a real app, this would be an API call

  constructor(private router: Router) {
    // Check if user is already logged in
    this.isAuthenticated = localStorage.getItem('isLoggedIn') === 'true';
  }

  signup(userData: User): boolean {
    // Check if user already exists
    const existingUser = this.users.find(user => user.email === userData.email);
    if (existingUser) {
      return false;
    }

    // Add user to "database"
    this.users.push(userData);
    
    // Auto-login after signup
    return this.login(userData.email, userData.password);
  }

  login(email: string, password: string): boolean {
    // Demo authentication - replace with real API call
    if (email === 'demo@example.com' && password === 'password') {
      this.isAuthenticated = true;
      localStorage.setItem('isLoggedIn', 'true');
      localStorage.setItem('userEmail', email);
      return true;
    }
    
    // Check against registered users
    const user = this.users.find(u => u.email === email && u.password === password);
    if (user) {
      this.isAuthenticated = true;
      localStorage.setItem('isLoggedIn', 'true');
      localStorage.setItem('userEmail', email);
      localStorage.setItem('userName', `${user.firstName} ${user.lastName}`);
      return true;
    }
    
    return false;
  }

  logout(): void {
    this.isAuthenticated = false;
    localStorage.removeItem('isLoggedIn');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userName');
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return this.isAuthenticated;
  }

  getCurrentUser(): string | null {
    return localStorage.getItem('userName');
  }
}