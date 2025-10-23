// breadcrumb.component.ts
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';

interface BreadcrumbLink {
  label: string;
  route?: string;
  icon?: string;
}

@Component({
  selector: 'app-breadcrumb',
  imports: [MatIcon, CommonModule],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss'
})
export class BreadcrumbComponent {
  @Input() links: BreadcrumbLink[] = [];
  @Input() current: string = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  goToDashboard(): void {
    const currentUrl = this.router.url;

    if (currentUrl.startsWith('/nav/scheme')) {
      this.router.navigate(['/nav/report']);
    }
    else if (
      currentUrl.startsWith('/nav/report') ||
      currentUrl.startsWith('/nav/manage') ||
      currentUrl.startsWith('/nav/import') ||
      currentUrl.startsWith('/nav/compare')
    ) {
      this.router.navigate(['/nav']);
    }
    else if (currentUrl.startsWith('/settings')) {
      this.router.navigate(['/settings']);
    }
    else {
      // fallback
      this.router.navigate(['/dashboard']);
    }
  }

  navigateTo(link: BreadcrumbLink): void {
    if (link.route) {
      this.router.navigate([link.route]);
    }
  }
}