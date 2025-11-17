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
import { SettingsDashboardComponent } from './features/settings/settings-dashboard/settings-dashboard.component';
import { ChangePasswordComponent } from './features/settings/change-password/change-password.component';
import { UserManagementComponent } from './features/settings/user-management/user-management.component';
import { EmailComponent } from './features/email/email.component';
import { InvestmentDashboardComponent } from './features/investment/investment-dashboard/investment-dashboard.component';
import { CreateInvestmentComponent } from './features/investment/create-investment/create-investment.component';

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
        canActivate: [authGuard],
        children: [
            // Main Dashboard - Accessible to all authenticated users
            {
                path: '',
                component: DashboardComponent,
                pathMatch: 'full'
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
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee'] }
                    },

                    // RESTRICTED to Admin and Employee only
                    {
                        path: 'manage',
                        component: ManageSchemesComponent,
                        canActivate: [roleGuard],
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
            {
                path: 'portfolio',
                children: [
                    {
                        path: '',
                        component: InvestmentDashboardComponent,
                        pathMatch: 'full'
                    },
                    {
                        path: 'create-investment',
                        component: CreateInvestmentComponent,
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee'] }
                    },
                    // You can add more portfolio routes here
                    {
                        path: 'my-investments',
                        component: InvestmentDashboardComponent, // Replace with actual component
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    },
                    {
                        path: 'summary',
                        component: InvestmentDashboardComponent, // Replace with actual component
                        canActivate: [roleGuard],
                        data: { roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember'] }
                    }
                ]
            },

            // Settings Section
            {
                path: 'settings',
                component: SettingsDashboardComponent
            },
            {
                path: 'settings/change-password',
                component: ChangePasswordComponent
            },
            {
                path: 'settings/user-management',
                component: UserManagementComponent,
                canActivate: [roleGuard],
                data: { roles: ['Admin'] }
            },

            // Email Section
            {
                path: 'emails',
                component: EmailComponent
            },

            // Other main app routes (these might need to be updated to actual components)
            {
                path: 'funds',
                component: DashboardComponent // Replace with actual component
            },
            {
                path: 'transactions',
                component: DashboardComponent // Replace with actual component
            },
            {
                path: 'performance',
                component: DashboardComponent // Replace with actual component
            },
        ]
    },

    // Fallback routes - ONLY the wildcard route
    { path: '**', redirectTo: '/login' }
];