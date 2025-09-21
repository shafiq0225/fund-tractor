import { CommonModule } from '@angular/common';
import { Component, ElementRef, inject, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { AmfiService } from '../../../core/services/amfi.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

interface UploadedFile {
  name: string;
  size: number;
  file: File;
}

@Component({
  selector: 'app-nav-import',
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './nav-import.component.html',
  styleUrl: './nav-import.component.scss'
})

export class NavImportComponent {
  amfiService = inject(AmfiService);
  snackBarService = inject(SnackbarService);
  uploadedFile: File | null = null;
  uploadProgress: number = 0;
  uploading: boolean = false;
  loading: boolean = false;   // For API calls (fetch/import)
  isDragging: boolean = false;
  private uploadInterval: any;
  importLoading = false;   // For Import button
  fetchLoading = false;    // For Fetch button
  activeMode: 'upload' | 'fetch' | null = null;

  fileUrl: string = 'https://portal.amfiindia.com/spages/NAVAll.txt';

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  // Select file manually
  onFileSelected(event: Event): void {
    if (this.activeMode === 'fetch') return; // block upload when fetching

    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadedFile = input.files[0];
      this.startUploadSimulation();
      this.activeMode = 'upload';
    }
  }

  // Simulate upload progress
  private startUploadSimulation(): void {
    this.uploading = true;
    this.uploadProgress = 0;

    if (this.uploadInterval) clearInterval(this.uploadInterval);

    this.uploadInterval = setInterval(() => {
      if (this.uploadProgress >= 100) {
        clearInterval(this.uploadInterval);
        this.uploading = false;
        // keep activeMode = 'upload' until user discards or imports
      } else {
        this.uploadProgress += 10;
      }
    }, 300);
  }

  // Discard file and reset
  discard(): void {
    this.uploadedFile = null;
    this.uploading = false;
    this.uploadProgress = 0;
    this.activeMode = null;

    if (this.uploadInterval) clearInterval(this.uploadInterval);
  }

  deleteFile(): void {
    this.discard();
  }

  importFile(): void {
    if (!this.uploadedFile) return;

    this.importLoading = true;
    console.log(this.uploadedFile);

    this.amfiService.uploadAndSaveFromFile(this.uploadedFile).subscribe({
      next: (response) => {
        this.importLoading = false;
        this.snackBarService.success(response.message || 'File imported successfully');
        this.discard(); // clear after success
      },
      error: (err) => {
        this.importLoading = false;
        this.snackBarService.error(err?.error?.message || 'Error importing file');
      }
    });
  }


  // Drag & Drop
  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
  }

  onDrop(event: DragEvent): void {
    if (this.activeMode === 'fetch') return; // block when fetching

    event.preventDefault();
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.uploadedFile = event.dataTransfer.files[0];
      this.startUploadSimulation();
      this.activeMode = 'upload';
    }
  }


  // Fetch data from AMFI portal
  fetchFromUrl() {
    if (this.activeMode === 'upload') return; // block fetch when uploading

    this.fetchLoading = true;
    this.activeMode = 'fetch';

    this.amfiService.downloadAndSaveFromUrl(this.fileUrl).subscribe({
      next: (response) => {
        this.fetchLoading = false;
        this.activeMode = null;
        if (response?.message) {
          this.snackBarService.success(response.message);
        }
      },
      error: (err) => {
        this.fetchLoading = false;
        this.activeMode = null;
        this.snackBarService.error(err?.error?.message || 'Error Fetching Data');
      },
    });
  }
}