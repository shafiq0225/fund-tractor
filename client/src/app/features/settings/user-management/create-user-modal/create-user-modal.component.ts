import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { AuthService, CreateUserRequest } from '../../../../core/services/auth.service';
import { SnackbarService } from '../../../../core/services/snackbar.service';

@Component({
  selector: 'app-create-user-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatIcon],
  templateUrl: './create-user-modal.component.html',
  styleUrls: ['./create-user-modal.component.scss']
})
export class CreateUserModalComponent {
  private authService = inject(AuthService);
  private snackBarService = inject(SnackbarService);
  private fb = inject(FormBuilder);
  
  loading = false;
  userForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<CreateUserModalComponent>
  ) {
    this.userForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      panNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/)]],
      isActive: [true]
    });
  }

  onPanInput(event: any): void {
    const input = event.target;
    const value = input.value.toUpperCase();
    input.value = value;
    this.userForm.patchValue({ panNumber: value });
  }

  onCreate() {
    if (this.userForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    const userData: CreateUserRequest = this.userForm.value;
    this.loading = true;

    this.authService.createUser(userData).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          this.dialogRef.close(response.data);
          this.snackBarService.success(`User "${userData.firstName} ${userData.lastName}" created successfully!`);
        } else {
          this.snackBarService.error(response.message || 'Failed to create user.');
        }
      },
      error: (error) => {
        this.loading = false;
        this.snackBarService.error(error.error?.message || 'Failed to create user. Please try again.');
      }
    });
  }

  cancel() {
    this.dialogRef.close();
  }
}