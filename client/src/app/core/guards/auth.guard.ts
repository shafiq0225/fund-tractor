import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  console.log('🔐 AuthGuard checking authentication');

  if (authService.isLoggedIn()) {
    console.log('✅ User is authenticated');
    return true;
  }

  console.log('❌ User not authenticated, redirecting to login');
  router.navigate(['/login']);
  return false;
};