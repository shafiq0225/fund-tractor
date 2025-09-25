import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { AmfiService } from '../../../../core/services/amfi.service';
import { SnackbarService } from '../../../../core/services/snackbar.service';
import { AddSchemeRequest } from '../../../../shared/models/Amfi/AddSchemeRequest';

@Component({
  selector: 'app-add-scheme-modal',
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatIcon],
  templateUrl: './add-scheme-modal.component.html',
  styleUrl: './add-scheme-modal.component.scss'
})
export class AddSchemeModalComponent {
  amfiService = inject(AmfiService);
  snackBarService = inject(SnackbarService);
  loading = false;
  schemeForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddSchemeModalComponent>
  ) {
    this.schemeForm = this.fb.group({
      schemeName: ['', [Validators.required]],
      fundName: ['', Validators.required],
      schemeId: ['', Validators.required],
      isApproved: [true],
    });
  }

  onSave() {
    if (this.schemeForm.invalid) return;

    const scheme: AddSchemeRequest = this.schemeForm.value;
    this.loading = true;    
    this.amfiService.addScheme(scheme).subscribe({
      next: (res) => {
        this.loading = false;
        this.dialogRef.close(res);
      },
      error: (err) => {
        this.loading = false;
        this.snackBarService.error(err.error?.message || 'Failed to add scheme.');
      }
    });
  }

  cancel() {
    this.dialogRef.close();
  }

}
