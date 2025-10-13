import { CommonModule } from '@angular/common';
import { Component, Input, ViewChild, ElementRef, AfterViewInit, OnChanges, SimpleChanges } from '@angular/core';
import { Chart, registerables, ChartType } from 'chart.js';

Chart.register(...registerables);

// Define allowed chart types
type AllowedChartType = 'bar' | 'line' | 'doughnut' | 'scatter';

@Component({
  selector: 'app-chart-card',
  templateUrl: './chart-card.component.html',
  imports: [CommonModule],
  standalone: true
})
export class ChartCardComponent implements AfterViewInit, OnChanges {
  @Input() title!: string;
  @Input() type: AllowedChartType = 'bar';
  @Input() data: any;
  @Input() options: any;
  
  @ViewChild('chartCanvas') chartCanvas!: ElementRef;
  
  private chart: Chart | null = null;

  ngAfterViewInit() {
    this.createChart();
  }

  ngOnChanges(changes: SimpleChanges) {
    if ((changes['data'] || changes['type'] || changes['options']) && !changes['data']?.firstChange) {
      this.updateChart(changes);
    }
  }

  private createChart() {
    if (this.chartCanvas?.nativeElement) {
      // Ensure the chart is destroyed before creating a new one
      if (this.chart) {
        this.chart.destroy();
      }

      this.chart = new Chart(this.chartCanvas.nativeElement, {
        type: this.type as ChartType,
        data: this.data || {},
        options: this.options || {}
      });
    }
  }

  private updateChart(changes: SimpleChanges) {
    if (this.chart) {
      // If chart type changed, recreate the chart
      if (changes['type'] && !changes['type'].firstChange) {
        this.createChart();
        return;
      }

      // Update data and options
      if (this.data) {
        this.chart.data = this.data;
      }
      if (this.options) {
        this.chart.options = this.options;
      }
      this.chart.update();
    }
  }

  ngOnDestroy() {
    if (this.chart) {
      this.chart.destroy();
    }
  }
}