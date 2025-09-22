import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltip } from '@angular/material/tooltip';
import { Scheme } from '../../../../shared/models/Amfi/Scheme';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-scheme-list',
  imports: [CommonModule, MatSlideToggleModule, MatTooltip, MatIcon, MatProgressSpinner],
  templateUrl: './scheme-list.component.html',
  styleUrl: './scheme-list.component.scss'
})
export class SchemeListComponent {
  @Input() schemes: Scheme[] = [];
  @Output() toggle = new EventEmitter<Scheme>();

  onToggle(scheme: any) {
    scheme.isApproved = !scheme.isApproved;
    this.toggle.emit(scheme);
  }

}
