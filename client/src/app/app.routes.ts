// routes.ts
import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component'; // Add this
import { NavComponent } from './features/nav/nav.component';
import { NavImportComponent } from './features/nav/nav-import/nav-import.component';
import { NavDashboardComponent } from './features/nav/nav-dashboard/nav-dashboard.component';
import { ManageSchemesComponent } from './features/nav/manage-schemes/manage-schemes.component';
import { NavReportComponent } from './features/nav/nav-report/nav-report.component';
import { SchemePerformanceComponent } from './features/nav/nav-report/scheme-performance/scheme-performance.component';
import { NavCompareComponent } from './features/nav/nav-compare/nav-compare.component';
import { AuthGuard } from './core/guards/auth.guard';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
    // Public route - no layout
    { 
        path: 'login', 
        component: LoginComponent 
    },
    
    // Protected routes - with layout
    {
        path: '',
        component: LayoutComponent,
        canActivate: [AuthGuard],
        children: [
            // Main Dashboard
            { path: '', component: DashboardComponent }, // This is your main dashboard
            
            // NAV Management Section
            {
                path: 'nav',
                component: NavComponent,
                children: [
                    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
                    { path: 'dashboard', component: NavDashboardComponent },
                    { path: 'import', component: NavImportComponent },
                    { path: 'manage', component: ManageSchemesComponent },
                    { path: 'report', component: NavReportComponent },
                    { path: 'scheme', component: SchemePerformanceComponent },
                    { path: 'compare', component: NavCompareComponent },
                ]
            },
            
            // Other main app routes (you can add more here)
            { path: 'portfolio', component: DashboardComponent }, // Example
            { path: 'funds', component: DashboardComponent }, // Example
            { path: 'transactions', component: DashboardComponent }, // Example
            { path: 'performance', component: DashboardComponent }, // Example
        ]
    },
    
    // Fallback routes
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '**', redirectTo: '/login' }
];