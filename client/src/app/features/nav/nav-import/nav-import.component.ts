import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
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
  uploadedFile: File | null = null;
  uploadProgress: number = 0;
  uploading: boolean = false;
  loading: boolean = false;   // For API calls (fetch/import)
  isDragging: boolean = false;
  private uploadInterval: any;
  importLoading = false;   // For Import button
  fetchLoading = false;    // For Fetch button

  fileUrl: string = 'https://portal.amfiindia.com/spages/NAVAll.txt';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private amfiService: AmfiService,
    private snackBarService: SnackbarService
  ) { }

  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  // Select file manually
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadedFile = input.files[0];
      this.startUploadSimulation();
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
        this.uploading = false; // hide discard after completion
      } else {
        this.uploadProgress += 10; // increase by 10% every 300ms
      }
    }, 300);
  }

  // Discard file and reset
  discard(): void {
    this.uploadedFile = null;
    this.uploading = false;
    this.uploadProgress = 0;
    if (this.uploadInterval) clearInterval(this.uploadInterval);
  }

  deleteFile(): void {
    this.discard();
  }

  // Import after upload completed (fake API for now)
  importFile(): void {
    if (this.uploadProgress === 100 && this.uploadedFile) {
      this.importLoading = true;

      // Simulated API delay (remove later)
      setTimeout(() => {
        this.importLoading = false;
        this.snackBarService.success('File ready for import (API not implemented yet).');
      }, 2000);
    }
  }



  // Drag & Drop
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.uploadedFile = event.dataTransfer.files[0];
      this.startUploadSimulation();
    }
  }

  // Fetch data from AMFI portal
  fetchFromUrl(): void {
    const confirmed = confirm('Are you sure you want to fetch the latest AMFI data?');
    if (!confirmed) return;

    this.fetchLoading = true;
    this.amfiService.ImportNavFromUrl(this.fileUrl).subscribe({
      next: (res) => {
        this.fetchLoading = false;
        this.snackBarService.success(res?.message || 'Data fetched successfully!');
      },
      error: (err) => {
        this.fetchLoading = false;
        this.snackBarService.error(err?.error?.message || 'Error fetching data');
      }
    });
  }

}