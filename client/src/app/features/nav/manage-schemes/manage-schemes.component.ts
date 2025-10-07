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
import { MatDialog } from '@angular/material/dialog';
import { AddSchemeModalComponent } from './add-scheme-modal/add-scheme-modal.component';
import { BreadcrumbComponent } from "../../../shared/components/breadcrumb/breadcrumb.component";

@Component({
  selector: 'app-manage-schemes',
  imports: [CommonModule, MatIcon, SchemeListComponent, MatTooltip, MatProgressBarModule, BreadcrumbComponent],
  templateUrl: './manage-schemes.component.html',
  styleUrl: './manage-schemes.component.scss'
})
export class ManageSchemesComponent implements OnInit {
  amfiService = inject(AmfiService);
  snackBarService = inject(SnackbarService);
  schemes: Scheme[] = [];
  loading = true;
  errorMessage: string | null = null;
  dialog = inject(MatDialog);


  ngOnInit(): void {
    this.fetchSchemes();
  }

  fetchSchemes() {
    this.amfiService.getSchemes().subscribe({
      next: (res) => {
        this.schemes = res.data;
        this.loading = false;
      },
      error: (err) => {
        this.snackBarService.error(err.error?.message || 'Failed to load schemes.');
        this.loading = false;
      }
    });
  }

  toggleStatus(scheme: any) {
    scheme.status = !scheme.status;
  }

  onSchemeToggle(scheme: Scheme) {
    const newStatus = !scheme.isApproved; // invert here
    const prevStatus = scheme.isApproved;

    scheme.isUpdating = true;

    this.amfiService.updateSchemeApproval(scheme.fundCode, scheme.schemeCode, newStatus).subscribe({
      next: (res) => {
        scheme.isApproved = res.isApproved; // set based on API
        scheme.lastUpdatedDate = new Date().toISOString();
        scheme.isUpdating = false;
        const msg = res.isApproved
          ? `${res.fundId} - Scheme ${res.schemeId} approved successfully.`
          : `${res.fundId} - Scheme ${res.schemeId} approval revoked.`;
        this.snackBarService.success(msg || res.message);
      },
      error: (err) => {
        scheme.isApproved = prevStatus; // rollback
        scheme.isUpdating = false;
        this.snackBarService.error(err.error?.message || 'Failed to update scheme status.');
      }
    });
  }

  // Fund-level update event handler
  onFundUpdate(event: { fundId: string; isApproved: boolean }) {
    const fundSchemes = this.schemes.filter(s => s.fundCode === event.fundId);
    fundSchemes.forEach(s => s.isUpdating = true);

    this.amfiService.updateApprovedFund(event.fundId, event.isApproved).subscribe({
      next: (res) => {
        if (res.success) {
          fundSchemes.forEach(s => {
            s.isApproved = event.isApproved;
            s.isUpdating = false;
            s.lastUpdatedDate = new Date().toISOString();
          });
          this.snackBarService.success(event.isApproved
            ? `All schemes under fund ${event.fundId} approved successfully.`
            : `All schemes under fund ${event.fundId} deactivated.`);
        } else {
          fundSchemes.forEach(s => s.isUpdating = false);
          this.snackBarService.error(res.message || 'Failed to update fund.');
        }
      },
      error: (err) => {
        fundSchemes.forEach(s => s.isUpdating = false);
        this.snackBarService.error(err.error?.message || 'Failed to update fund.');
      }
    });
  }

  onAddScheme() {
    const dialogRef = this.dialog.open(AddSchemeModalComponent, {
      width: '500px',
      panelClass: 'custom-dialog-container'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.fetchSchemes();
        this.snackBarService.success(`Scheme "${result.fundName}" added successfully!`);
      }
    });
  }

}
