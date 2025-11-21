import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  console.log('🔐 AuthGuard checking authentication');

  if (authService.isLoggedIn()) {
    const user = authService.getCurrentUser();
    console.log('✅ User is authenticated:', user);
    
    // Check if user is active and has roles
    if (user && user.isActive && user.roles && user.roles.length > 0) {
      console.log('✅ User is active and has roles');
      return true;
    } else {
      console.log('❌ User is inactive or has no roles');
      
      // Navigate to appropriate page based on the issue
      if (!user?.isActive) {
        console.log('🚫 User account is inactive');
        router.navigate(['/unauthorized'], {
          queryParams: { reason: 'inactive' }
        });
      } else if (!user?.roles || user.roles.length === 0) {
        console.log('🚫 User has no roles assigned');
        router.navigate(['/unauthorized'], {
          queryParams: { reason: 'no-roles' }
        });
      } else {
        console.log('🚫 Unknown access issue');
        router.navigate(['/unauthorized'], {
          queryParams: { reason: 'unknown' }
        });
      }
      return false;
    }
  }

  console.log('❌ User not authenticated, redirecting to login');
  router.navigate(['/login']);
  return false;
};