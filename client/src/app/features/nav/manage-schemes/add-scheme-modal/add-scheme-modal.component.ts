import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'app-add-scheme-modal',
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatIcon],
  templateUrl: './add-scheme-modal.component.html',
  styleUrl: './add-scheme-modal.component.scss'
})
export class AddSchemeModalComponent {
  schemeForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<AddSchemeModalComponent>
  ) {
    this.schemeForm = this.fb.group({
      schemeName: ['', [Validators.required]],
      fundName: ['', Validators.required],
      schemeCode: ['', Validators.required],
      isActive: [true],
    });
  }

  save() {
    if (this.schemeForm.valid) {
      this.dialogRef.close(this.schemeForm.value);
    } else {
      this.schemeForm.markAllAsTouched();
    }
  }

  cancel() {
    this.dialogRef.close();
  }

}
