import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-nav-import',
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule
  ],
  templateUrl: './nav-import.component.html',
  styleUrl: './nav-import.component.scss'
})
export class NavImportComponent {
  constructor(private router: Router, private route: ActivatedRoute) {}

  goToDashboard() {
    // Navigate to the default child of /nav
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('dropZone') dropZone!: ElementRef<HTMLDivElement>;

  isDragging = false;
  uploadedFile: File | null = null;
  uploadProgress = 0; // Default progress for demo
  fileUrl = '';
  uploading = false;
  uploadInterval: any;

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
      event.dataTransfer.clearData();
    }
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.uploadedFile = file;
      this.startUploadSimulation();
    }
  }

  startUploadSimulation() {
    this.uploadProgress = 0;
    this.uploading = true;
    clearInterval(this.uploadInterval);

    this.uploadInterval = setInterval(() => {
      if (this.uploadProgress < 100) {
        this.uploadProgress += 10;
      } else {
        clearInterval(this.uploadInterval);
        this.uploading = false;
      }
    }, 500);
  }


  handleFile(file: File) {
    // Check file type
    const validExtensions = ['.csv', '.xls', '.xlsx', '.txt'];
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!validExtensions.includes(fileExtension)) {
      alert('Please select a CSV, XLS, XLSX, or TXT file');
      return;
    }

    // Check file size (4MB max)
    if (file.size > 4 * 1024 * 1024) {
      alert('File size exceeds 4MB limit');
      return;
    }

    // Replace any existing file with the new one
    this.uploadedFile = file;

    // Simulate upload progress
    this.simulateUploadProgress();
  }

  uploadFromUrl() {
    if (!this.fileUrl) {
      alert('Please enter a valid URL');
      return;
    }

    // Extract filename from URL for display
    const fileName = this.fileUrl.split('/').pop() || 'downloaded_file.csv';

    // Create a mock file object
    this.uploadedFile = new File([], fileName, { type: 'text/csv' });

    // Simulate upload progress
    this.simulateUploadProgress();
  }

  simulateUploadProgress() {
    this.uploadProgress = 0;
    const interval = setInterval(() => {
      this.uploadProgress += 5;
      if (this.uploadProgress >= 100) {
        this.uploadProgress = 100;
        clearInterval(interval);
      }
    }, 100);
  }

  discard() {
    clearInterval(this.uploadInterval);
    this.uploadedFile = null;
    this.uploadProgress = 0;
    this.uploading = false;
  }

  /** 🟥 Delete removes file completely */
  deleteFile() {
    clearInterval(this.uploadInterval);
    this.uploadedFile = null;
    this.uploadProgress = 0;
    this.uploading = false;
  }

  importFile() {
    if (this.uploadProgress === 100 && this.uploadedFile) {
      console.log('Importing file:', this.uploadedFile.name);

      // ✅ Here you call your backend API to process the file
      // this.fileService.import(this.uploadedFile).subscribe(...);

      // Reset UI after import
      alert(`File "${this.uploadedFile.name}" imported successfully!`);
      this.uploadedFile = null;
      this.uploadProgress = 0;
    }
  }
  
}
