import { Component, inject } from '@angular/core';
import { SidebarComponent } from "./sidebar/sidebar.component";
import { HeaderComponent } from "./header/header.component";
import { MatProgressBar } from '@angular/material/progress-bar';
import { BusyService } from '../core/services/busy.service';

@Component({
  selector: 'app-layout',
  imports: [SidebarComponent, HeaderComponent, MatProgressBar],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  busyService = inject(BusyService);
}
