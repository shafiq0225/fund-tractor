import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { EmailService, StoredEmailDto } from '../../core/services/email.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-email',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    FormsModule
  ],
  templateUrl: './email.component.html',
  styleUrls: ['./email.component.scss']
})
export class EmailComponent implements OnInit {
  private emailService = inject(EmailService);
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  emails: StoredEmailDto[] = [];
  filteredEmails: StoredEmailDto[] = [];
  selectedEmail: StoredEmailDto | null = null;
  isLoading = false;
  isListLoading = false;
  userId: number = 0;

  // Search option
  searchTerm: string = '';

  // Selection
  selectedEmails = new Set<number>();
  selectAll: boolean = false;

  // sort/filter
  sortBy: string = 'newest';
  filter: string = 'all';

  // responsive sidebar
  sidebarOpen = true;
  isMobile = false;



  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user && user.id) {
        this.userId = user.id;
        this.loadEmailList(); // Only load the list initially, not full emails
      }
    });

    // set mobile state
    this.checkMobile();
    window.addEventListener('resize', () => this.checkMobile());
  }

  checkMobile() {
    this.isMobile = window.innerWidth < 768;
    this.sidebarOpen = !this.isMobile;
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
  }

  // Load only email list (basic info) initially

  loadEmailList(): void {
    this.isListLoading = true;
    this.emailService.getUserEmails(this.userId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Store only basic info initially
          this.emails = response.data.map(email => ({
            id: email.id,
            userId: email.userId,
            toEmail: email.toEmail,
            subject: email.subject,
            body: '', // Don't load full body initially
            type: email.type,
            status: email.status,
            metadata: email.metadata,
            createdAt: email.createdAt,
            viewedAt: email.viewedAt,
            userName: email.toEmail
          }));
          this.filteredEmails = [...this.emails];
          this.applyFilter(this.filter);
        } else {
          console.warn('No email data received');
        }
        this.isListLoading = false;
      },
      error: (error) => {
        console.error('Failed to load emails:', error);
        this.isListLoading = false;
      }
    });
  }


  // Method to sanitize HTML with styles
  getSanitizedHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  trackByEmailId(index: number, email: StoredEmailDto): number {
    return email.id;
  }

  // Search
  searchMessages(): void {
    if (!this.searchTerm.trim()) {
      this.filteredEmails = [...this.emails];
    } else {
      const term = this.searchTerm.toLowerCase().trim();
      this.filteredEmails = this.emails.filter(email =>
        email.subject.toLowerCase().includes(term) ||
        (email.body && this.stripHtml(email.body).toLowerCase().includes(term)) ||
        email.type.toLowerCase().includes(term) ||
        email.toEmail.toLowerCase().includes(term)
      );
    }
    this.sortEmails(this.sortBy);
    this.applyFilter(this.filter, true);
  }


  clearSearch(): void {
    this.searchTerm = '';
    this.filteredEmails = [...this.emails];
    this.sortEmails(this.sortBy);
    this.applyFilter(this.filter);
  }


  sortEmails(sortBy: string): void {
    this.sortBy = sortBy;
    this.filteredEmails.sort((a, b) => {
      switch (sortBy) {
        case 'newest': return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        case 'oldest': return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
        case 'unread':
          if (a.status !== 'viewed' && b.status === 'viewed') return -1;
          if (a.status === 'viewed' && b.status !== 'viewed') return 1;
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        default: return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
    });
  }

  onSortChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.sortEmails(target.value);
  }

  // selection
  toggleSelectAll(): void {
    if (this.selectAll) {
      this.filteredEmails.forEach(email => this.selectedEmails.add(email.id));
    } else {
      this.selectedEmails.clear();
    }
  }

  toggleEmailSelection(emailId: number, event: Event): void {
    event.stopPropagation();
    if (this.selectedEmails.has(emailId)) this.selectedEmails.delete(emailId);
    else this.selectedEmails.add(emailId);

    this.selectAll = this.filteredEmails.length > 0 && this.filteredEmails.every(email => this.selectedEmails.has(email.id));
  }

  // bulk actions
  markSelectedAsRead(): void {
    this.selectedEmails.forEach(emailId => {
      const email = this.emails.find(e => e.id === emailId);
      if (email && email.status !== 'viewed') {
        this.emailService.markEmailAsViewed(emailId).subscribe({
          next: () => {
            email.status = 'viewed';
            email.viewedAt = new Date().toISOString();
          }
        });
      }
    });
    this.selectedEmails.clear();
    this.selectAll = false;
  }

  deleteSelected(): void {
    const emailIds = Array.from(this.selectedEmails);
    // Implement your delete API call here
    console.log('Deleting emails:', emailIds);

    // Optimistic UI update
    this.emails = this.emails.filter(e => !this.selectedEmails.has(e.id));
    this.filteredEmails = this.filteredEmails.filter(e => !this.selectedEmails.has(e.id));

    if (this.selectedEmail && this.selectedEmails.has(this.selectedEmail.id)) {
      this.selectedEmail = null;
    }

    this.selectedEmails.clear();
    this.selectAll = false;
  }

  // single item actions
  markSingleAsRead(email: StoredEmailDto, event?: Event) {
    if (event) event.stopPropagation();
    if (email.status !== 'viewed') {
      this.emailService.markEmailAsViewed(email.id).subscribe({
        next: () => {
          email.status = 'viewed';
          email.viewedAt = new Date().toISOString();
        }
      });
    }
  }

  deleteSingle(email: StoredEmailDto, event?: Event) {
    if (event) event.stopPropagation();
    // Implement single delete API call here
    console.log('Deleting email:', email.id);

    this.emails = this.emails.filter(e => e.id !== email.id);
    this.filteredEmails = this.filteredEmails.filter(e => e.id !== email.id);
    if (this.selectedEmail?.id === email.id) this.selectedEmail = null;
  }

  selectEmail(email: StoredEmailDto): void {
    // If email body is not loaded yet, load it
    if (!email.body || email.body.trim().length === 0) {
      this.loadEmailContent(email.id);
    } else {
      this.selectedEmail = email;
    }

    // Mark as viewed if needed
    if (email.status !== 'viewed') {
      this.emailService.markEmailAsViewed(email.id).subscribe({
        next: () => {
          email.status = 'viewed';
          email.viewedAt = new Date().toISOString();
        }
      });
    }

    if (this.isMobile) this.sidebarOpen = false;
  }



  // getters
  get unreadCount(): number {
    return this.emails.filter(e => e.status !== 'viewed').length;
  }

  get selectedCount(): number {
    return this.selectedEmails.size;
  }

  get totalCount(): number {
    return this.filteredEmails.length;
  }

  getEmailIcon(type: string): string {
    const iconMap: { [key: string]: string } = {
      'role_update': 'admin_panel_settings',
      'alert': 'warning',
      'notification': 'notifications',
      'broadcast': 'campaign',
      'system': 'settings'
    };
    return iconMap[type] || 'email';
  }

  getRelativeTime(createdAt: string): string {
    const created = new Date(createdAt);
    const now = new Date();
    const diffMs = now.getTime() - created.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return created.toLocaleDateString();
  }

  getDetailedTime(createdAt: string): string {
    const created = new Date(createdAt);
    return created.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getEmailPreview(body: string): string {
    if (!body || body.trim().length === 0) {
      return 'Click to load message...';
    }
    const text = this.stripHtml(body);
    return text.length > 100 ? text.substring(0, 100) + '...' : text;
  }


  getSenderDisplay(email: StoredEmailDto): string {
    const senderMap: { [key: string]: string } = {
      'role_update': 'User Management System',
      'system': 'System Administrator',
      'alert': 'Security System',
      'broadcast': 'Announcement System'
    };
    return senderMap[email.type] || 'System';
  }

  getRoleChangeSummary(email: StoredEmailDto): string {
    if (email.type === 'role_update' && email.metadata) {
      return `${email.metadata.oldRole} → ${email.metadata.newRole}`;
    }
    return '';
  }

  refreshEmails(): void {
    this.loadEmailList();
    this.selectedEmail = null;
    this.selectedEmails.clear();
    this.searchTerm = '';
  }

  getCurrentTime(): string {
    const now = new Date();
    return now.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    }).toUpperCase();
  }

  // Filter handler
  applyFilter(filter: string, keepCurrentSearch = false) {
    this.filter = filter;
    let pool = keepCurrentSearch && this.searchTerm ? this.filteredEmails : [...this.emails];

    switch (filter) {
      case 'unread':
        pool = this.emails.filter(e => e.status !== 'viewed');
        break;
      case 'role_updates':
        pool = this.emails.filter(e => e.type === 'role_update');
        break;
      default:
        pool = [...this.emails];
    }

    if (this.searchTerm && !keepCurrentSearch) {
      const term = this.searchTerm.toLowerCase().trim();
      pool = pool.filter(e =>
        e.subject.toLowerCase().includes(term) ||
        this.stripHtml(e.body).toLowerCase().includes(term) ||
        e.type.toLowerCase().includes(term) ||
        e.toEmail.toLowerCase().includes(term)
      );
    }

    this.filteredEmails = pool;
    this.sortEmails(this.sortBy);
  }

  // Utility function to strip HTML tags
  private stripHtml(html: string): string {
    if (!html) return '';
    return html.replace(/<[^>]*>/g, '').replace(/&nbsp;/g, ' ').trim();
  }


  // Keyboard shortcuts
  @HostListener('window:keydown', ['$event'])
  handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && this.isMobile) {
      this.sidebarOpen = true;
    }
    if (event.key === 'Delete' && this.selectedCount > 0) {
      this.deleteSelected();
    }
  }

  loadEmailContent(emailId: number): void {
    this.isLoading = true;
    this.emailService.getEmailById(emailId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const fullEmail = response.data;
          // Update the email in the list with full content
          const index = this.emails.findIndex(e => e.id === emailId);
          if (index !== -1) {
            this.emails[index] = { ...this.emails[index], ...fullEmail };
          }

          // Update filtered emails if needed
          const filteredIndex = this.filteredEmails.findIndex(e => e.id === emailId);
          if (filteredIndex !== -1) {
            this.filteredEmails[filteredIndex] = { ...this.filteredEmails[filteredIndex], ...fullEmail };
          }

          // Set as selected email
          this.selectedEmail = this.emails[index];
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load email content:', error);
        this.isLoading = false;
      }
    });
  }



}