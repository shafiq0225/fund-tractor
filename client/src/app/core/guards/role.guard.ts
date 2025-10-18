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

  const requiredRoles = route.data['roles'] as string[];
  const userRoles = authService.getCurrentUser()?.roles || [];

  console.log('👤 User roles:', userRoles);
  console.log('🔑 Required roles:', requiredRoles);

  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = requiredRoles.some(role => userRoles.includes(role));
    console.log('✅ Has required role:', hasRequiredRole);

    if (!hasRequiredRole) {
      console.log('🚫 Access denied - insufficient permissions');
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  console.log('🎉 Access granted');
  return true;
};