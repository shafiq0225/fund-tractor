import { inject } from '@angular/core';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    // Check roles if specified in route data
    const requiredRoles = route.data['roles'] as Array<string>;
    
    if (requiredRoles && requiredRoles.length > 0) {
      const hasRequiredRole = authService.hasAnyRole(requiredRoles);
      
      if (!hasRequiredRole) {
        // Redirect to unauthorized or dashboard based on user role
        const user = authService.getCurrentUser();
        if (user?.roles.includes('FamilyMember') || user?.roles.includes('HeadOfFamily')) {
          // Family users get redirected to dashboard instead of unauthorized
          router.navigate(['/']);
        } else {
          router.navigate(['/unauthorized']);
        }
        return false;
      }
    }
    
    return true;
  }

  // Redirect to login with return URL
  router.navigate(['/login'], { 
    queryParams: { returnUrl: router.url } 
  });
  return false;
};