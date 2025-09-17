import { Routes } from '@angular/router';
import { NavComponent } from './features/nav/nav.component';
import { NavImportComponent } from './features/nav/nav-import/nav-import.component';
import { NavDashboardComponent } from './features/nav/nav-dashboard/nav-dashboard.component';

export const routes: Routes = [
    {
        path: 'nav',
        component: NavComponent,
        children: [
            { path: '', component: NavDashboardComponent },   
            { path: 'import', component: NavImportComponent }
        ]
    }
];
