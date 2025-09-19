import { Component } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { SchemeListComponent } from "./scheme-list/scheme-list.component";
import { MatTooltip } from '@angular/material/tooltip';

@Component({
  selector: 'app-manage-schemes',
  imports: [MatIcon, SchemeListComponent, MatTooltip],
  templateUrl: './manage-schemes.component.html',
  styleUrl: './manage-schemes.component.scss'
})
export class ManageSchemesComponent {
  constructor(private router: Router, private route: ActivatedRoute) { }

  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  schemes = [
    { id: 1, schemeName: 'DSP Banking & PSU Debt Fund - Direct Plan - Growth', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: true },
    { id: 1, schemeName: 'Franklin India Banking & PSU Debt Fund - Direct - Growth', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: true },
    { id: 1, schemeName: 'Scheme C', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: true },
    { id: 1, schemeName: 'Scheme D', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: false },
    { id: 1, schemeName: 'Scheme E', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: true },
    { id: 1, schemeName: 'Scheme F', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: true },
    { id: 1, schemeName: 'Scheme G', createdAt: '2025-09-02 00:00:00.000', lastUpdatedAt: '2025-09-02 00:00:00.000', approvedBy: 'shafiq', status: false },
  ];

  toggleStatus(scheme: any) {
    scheme.status = !scheme.status;
  }
  openAddSchemeModal() {

  }

  onSchemeToggle(scheme: any) {
    console.log('Toggled:', scheme);
    // here you could call API to persist
  }

}
