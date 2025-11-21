import { inject } from '@angular/core';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  console.log('🔐 RoleGuard checking route:', route.routeConfig?.path);
  console.log('📋 Route data:', route.data);

  if (!authService.isLoggedIn()) {
    console.log('❌ User not logged in');
    router.navigate(['/login']);
    return false;
  }

  const user = authService.getCurrentUser();
  const userRoles = user?.roles || [];

  // First check if user is active and has any roles
  if (!user?.isActive) {
    console.log('🚫 User account is inactive');
    router.navigate(['/unauthorized'], {
      queryParams: { reason: 'inactive' }
    });
    return false;
  }

  if (!userRoles || userRoles.length === 0) {
    console.log('🚫 User has no roles assigned');
    router.navigate(['/unauthorized'], {
      queryParams: { reason: 'no-roles' }
    });
    return false;
  }

  const requiredRoles = route.data['roles'] as string[];

  console.log('👤 User roles:', userRoles);
  console.log('🔑 Required roles:', requiredRoles);

  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = requiredRoles.some(role => userRoles.includes(role));
    console.log('✅ Has required role:', hasRequiredRole);

    if (!hasRequiredRole) {
      console.log('🚫 Access denied - insufficient permissions');
      router.navigate(['/unauthorized'], {
        queryParams: { reason: 'insufficient-permissions' }
      });
      return false;
    }
  }

  console.log('🎉 Access granted');
  return true;
};