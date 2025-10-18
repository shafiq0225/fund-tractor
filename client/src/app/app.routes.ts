import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { NavComponent } from './features/nav/nav.component';
import { NavImportComponent } from './features/nav/nav-import/nav-import.component';
import { NavDashboardComponent } from './features/nav/nav-dashboard/nav-dashboard.component';
import { ManageSchemesComponent } from './features/nav/manage-schemes/manage-schemes.component';
import { NavReportComponent } from './features/nav/nav-report/nav-report.component';
import { SchemePerformanceComponent } from './features/nav/nav-report/scheme-performance/scheme-performance.component';
import { NavCompareComponent } from './features/nav/nav-compare/nav-compare.component';
import { LayoutComponent } from './layout/layout.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { SignupComponent } from './features/signup/signup.component';
import { UnauthorizedComponent } from './features/unauthorized/unauthorized.component';

export const routes: Routes = [
    // Public routes - no layout
    { 
        path: 'login', 
        component: LoginComponent 
    },
    { 
        path: 'signup', 
        component: SignupComponent 
    },
    { 
        path: 'unauthorized', 
        component: UnauthorizedComponent 
    },
    
    // Protected routes - with layout
    {
        path: '',
        component: LayoutComponent,
        canActivate: [authGuard], // Only check authentication for parent
        children: [
            // Main Dashboard - Accessible to all authenticated users
            { 
                path: '', 
                component: DashboardComponent
            },
            
            // NAV Management Section
            {
                path: 'nav',
                component: NavComponent,
                children: [
                    { 
                        path: '', 
                        redirectTo: 'dashboard', 
                        pathMatch: 'full' 
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'dashboard', 
                        component: NavDashboardComponent,
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // RESTRICTED to Admin and Employee only
                    { 
                        path: 'import', 
                        component: NavImportComponent,
                        canActivate: [roleGuard], // Add roleGuard here
                        data: { roles: ['Admin', 'Employee'] }
                    },
                    
                    // RESTRICTED to Admin and Employee only
                    { 
                        path: 'manage', 
                        component: ManageSchemesComponent,
                        canActivate: [roleGuard], // Add roleGuard here
                        data: { roles: ['Admin', 'Employee'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'report', 
                        component: NavReportComponent,
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'scheme', 
                        component: SchemePerformanceComponent,
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'compare', 
                        component: NavCompareComponent,
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                ]
            },
            
            // Other main app routes
            { 
                path: 'portfolio', 
                component: DashboardComponent
            },
            { 
                path: 'funds', 
                component: DashboardComponent
            },
            { 
                path: 'transactions', 
                component: DashboardComponent
            },
            { 
                path: 'performance', 
                component: DashboardComponent
            },
        ]
    },
    
    // Fallback routes
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' }
];