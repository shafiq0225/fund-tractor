import { Component, inject, OnInit } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { SchemeListComponent } from "./scheme-list/scheme-list.component";
import { MatTooltip } from '@angular/material/tooltip';
import { Scheme } from '../../../shared/models/Amfi/Scheme';
import { AmfiService } from '../../../core/services/amfi.service';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-manage-schemes',
  imports: [CommonModule, MatIcon, SchemeListComponent, MatTooltip, MatProgressBarModule],
  templateUrl: './manage-schemes.component.html',
  styleUrl: './manage-schemes.component.scss'
})
export class ManageSchemesComponent implements OnInit {
  amfiService = inject(AmfiService);
  snackBarService = inject(SnackbarService);
  constructor(private router: Router, private route: ActivatedRoute) { }
  schemes: Scheme[] = [];
  loading = true;
  errorMessage: string | null = null;


  ngOnInit(): void {
    this.fetchSchemes();
  }

  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  fetchSchemes() {
    this.amfiService.getSchemes().subscribe({
      next: (res) => {
        this.schemes = res.data;
        console.log(this.schemes);

        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load schemes.';
        this.loading = false;
      }
    });
  }

  toggleStatus(scheme: any) {
    scheme.status = !scheme.status;
  }

  onSchemeToggle(scheme: Scheme) {
    const newStatus = scheme.isApproved;
    const prevStatus = !scheme.isApproved;

    scheme.isApproved = newStatus;
    scheme.isUpdating = true; // lock toggle while API call is in progress

    this.amfiService.updateSchemeApproval(scheme.fundCode, scheme.schemeCode, newStatus).subscribe({
      next: (res) => {
        scheme.isApproved = res.isApproved;
        scheme.lastUpdatedDate = new Date().toISOString();
        scheme.isUpdating = false;

        // Use backend message OR custom friendly message
        const msg = res.isApproved
          ? `${res.fundId} - Scheme ${res.schemeId} approved successfully.`
          : `${res.fundId} - Scheme ${res.schemeId} approval revoked.`;

        this.snackBarService.success(msg || res.message);
      },
      error: (err) => {
        // rollback if API failed
        scheme.isApproved = prevStatus;
        scheme.isUpdating = false;

        this.snackBarService.error(err.error.messgae || 'Failed to update scheme status.');
      }
    });
  }


}
