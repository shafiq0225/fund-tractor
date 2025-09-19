import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltip } from '@angular/material/tooltip';

@Component({
  selector: 'app-scheme-list',
  imports: [CommonModule, MatSlideToggleModule, MatTooltip],
  templateUrl: './scheme-list.component.html',
  styleUrl: './scheme-list.component.scss'
})
export class SchemeListComponent {
  @Input() schemes: any[] = [];
  @Output() toggle = new EventEmitter<any>();

  onToggle(scheme: any) {
    scheme.status = !scheme.status;
    this.toggle.emit(scheme);
  }

}
