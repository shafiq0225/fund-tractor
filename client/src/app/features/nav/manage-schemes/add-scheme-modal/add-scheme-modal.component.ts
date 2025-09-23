import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'app-add-scheme-modal',
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIcon
  ],
  templateUrl: './add-scheme-modal.component.html',
  styleUrl: './add-scheme-modal.component.scss'
})
export class AddSchemeModalComponent {
  fundName = '';
  schemeId = '';
  schemeName = '';
  isApproved = true;

  constructor(private dialogRef: MatDialogRef<AddSchemeModalComponent>) { }

  isValid() {
    return this.fundName.trim() && this.schemeId.trim() && this.schemeName.trim();
  }

  onSubmit() {
    if (this.isValid()) {
      this.dialogRef.close({
        fundName: this.fundName,
        schemeId: this.schemeId,
        schemeName: this.schemeName,
        isApproved: this.isApproved,
        createdAt: new Date(),
        lastUpdatedDate: new Date(),
        approvedName: 'Admin'
      });
    }
  }

  onCancel() {
    this.dialogRef.close();
  }

  isActive = true;   // Current toggle status
  isUpdating = false;

  toggleStatus() {
    this.isUpdating = true;

    // Simulate API call
    setTimeout(() => {
      this.isActive = !this.isActive; // Toggle state
      this.isUpdating = false;
    }, 200); // API response delay
  }


}
