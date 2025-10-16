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
import { SignupComponent } from './features/signup/signup.component';

export const routes: Routes = [
    // Public route - no layout
    { 
        path: 'login', 
        component: LoginComponent 
    },
    { 
        path: 'signup', 
        component: SignupComponent 
    },
    
    // Protected routes - with layout
    {
        path: '',
        component: LayoutComponent,
        canActivate: [authGuard],
        children: [
            // Main Dashboard - Accessible to all authenticated users
            { 
                path: '', 
                component: DashboardComponent,
                data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
            },
            
            // NAV Management Section
            {
                path: 'nav',
                component: NavComponent,
                children: [
                    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'dashboard', 
                        component: NavDashboardComponent,
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // Restricted to Admin and Employee only
                    { 
                        path: 'import', 
                        component: NavImportComponent,
                        data: { roles: ['Admin', 'Employee'] }
                    },
                    
                    // Restricted to Admin and Employee only
                    { 
                        path: 'manage', 
                        component: ManageSchemesComponent,
                        data: { roles: ['Admin', 'Employee'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'report', 
                        component: NavReportComponent,
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'scheme', 
                        component: SchemePerformanceComponent,
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    
                    // Accessible to all authenticated users
                    { 
                        path: 'compare', 
                        component: NavCompareComponent,
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                ]
            },
            
            // Other main app routes
            { 
                path: 'portfolio', 
                component: DashboardComponent,
                data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
            },
            { 
                path: 'funds', 
                component: DashboardComponent,
                data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
            },
            { 
                path: 'transactions', 
                component: DashboardComponent,
                data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
            },
            { 
                path: 'performance', 
                component: DashboardComponent,
                data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
            },
        ]
    },
    
    // Fallback routes
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' }
];