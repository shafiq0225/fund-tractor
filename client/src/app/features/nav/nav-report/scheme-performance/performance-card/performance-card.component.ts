import { Component, Input } from '@angular/core';
import { PerformanceMetric } from '../../../../../shared/models/Amfi/nav-performance.model';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-performance-card',
  imports: [DecimalPipe],
  templateUrl: './performance-card.component.html',
  styleUrl: './performance-card.component.scss'
})
export class PerformanceCardComponent {
  @Input() metric!: PerformanceMetric;
  @Input() compact = false;

}
