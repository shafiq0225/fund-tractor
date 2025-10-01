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
    this.router.navigate(['../'], { relativeTo: this.route });
  }

}
