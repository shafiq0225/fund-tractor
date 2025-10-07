import { Routes } from '@angular/router';
import { NavComponent } from './features/nav/nav.component';
import { NavImportComponent } from './features/nav/nav-import/nav-import.component';
import { NavDashboardComponent } from './features/nav/nav-dashboard/nav-dashboard.component';
import { ManageSchemesComponent } from './features/nav/manage-schemes/manage-schemes.component';
import { NavReportComponent } from './features/nav/nav-report/nav-report.component';
import { SchemePerformanceComponent } from './features/nav/nav-report/scheme-performance/scheme-performance.component';

export const routes: Routes = [
    {
        path: 'nav',
        component: NavComponent,
        children: [
            { path: '', component: NavDashboardComponent },
            { path: 'import', component: NavImportComponent },
            { path: 'manage', component: ManageSchemesComponent },
            { path: 'report', component: NavReportComponent },
            { path: 'scheme', component: SchemePerformanceComponent },
        ]
    }
];
