import { Component, ElementRef, inject, ViewChild } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('dropZone') dropZone!: ElementRef<HTMLDivElement>;
  
   private authService = inject(AuthService);

  menuItems: any[] = [];

  isDragging = false;
  uploadedFile: File | null = null;
  uploadProgress = 30; // Default progress for demo
  fileUrl = '';

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

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  handleFile(file: File) {
    // Check file type
    const validExtensions = ['.txt', '.xls', '.xlsx', '.csv'];
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    
    if (!validExtensions.includes(fileExtension)) {
      alert('Please select a TXT, XLS, XLSX, or CSV file');
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
    const fileName = this.fileUrl.split('/').pop() || 'downloaded_file.txt';
    
    // Create a mock file object
    this.uploadedFile = new File([], fileName, { type: 'text/plain' });
    
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
    this.uploadedFile = null;
    this.uploadProgress = 0;
    this.fileUrl = '';
  }

  import() {
    if (this.uploadedFile) {
      console.log('Importing file:', this.uploadedFile.name);
      // Here you would typically send the file to your backend API
      alert(`Importing ${this.uploadedFile.name}`);
    }
  }

   ngOnInit(): void {
    this.buildMenuBasedOnRole();
  }

  private buildMenuBasedOnRole(): void {
    const baseMenu = [
      { 
        path: '/', 
        label: 'Dashboard', 
        icon: 'dashboard',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember']
      }
    ];

    const navMenuItems = [
      { 
        path: '/nav/dashboard', 
        label: 'NAV Dashboard', 
        icon: 'table_chart',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember']
      },
      { 
        path: '/nav/import', 
        label: 'Import Data', 
        icon: 'file_upload',
        roles: ['Admin', 'Employee']
      },
      { 
        path: '/nav/manage', 
        label: 'Manage Schemes', 
        icon: 'settings',
        roles: ['Admin', 'Employee']
      },
      { 
        path: '/nav/report', 
        label: 'Reports', 
        icon: 'assessment',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember']
      },
      { 
        path: '/nav/scheme', 
        label: 'Scheme Performance', 
        icon: 'trending_up',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember']
      },
      { 
        path: '/nav/compare', 
        label: 'Compare', 
        icon: 'compare',
        roles: ['Admin', 'Employee', 'HeadOfFamily', 'FamilyMember']
      }
    ];

    // Filter menu items based on user roles
    this.menuItems = [
      ...baseMenu,
      ...navMenuItems.filter(item => 
        this.authService.hasAnyRole(item.roles)
      )
    ];
  }

  canShowMenuItem(roles: string[]): boolean {
    return this.authService.hasAnyRole(roles);
  }
}
