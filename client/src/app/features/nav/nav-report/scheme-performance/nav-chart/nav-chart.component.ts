// components/nav-chart/nav-chart.component.ts
import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, Input, OnChanges, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { Chart, registerables } from 'chart.js';

// Register all Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-nav-chart',
  templateUrl: './nav-chart.component.html',
  styleUrls: ['./nav-chart.component.scss'],
  imports: [CommonModule, DecimalPipe]
})
export class NavChartComponent implements OnChanges, AfterViewInit {
  @Input() historicalData!: { dates: string[]; navValues: number[] };
  @Input() chartType: 'line' | 'bar' = 'line';
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  private chart: Chart | null = null;

  ngAfterViewInit(): void {
    this.createChart();
  }

  ngOnChanges(): void {
    if (this.chart && this.historicalData) {
      this.updateChart();
    }
  }

  private createChart(): void {
  if (!this.chartCanvas || !this.historicalData) return;

  // Destroy existing chart
  if (this.chart) {
    this.chart.destroy();
  }

  const ctx = this.chartCanvas.nativeElement.getContext('2d');
  if (!ctx) return;

  const labels = this.getFormattedLabels();
  const data = this.historicalData.navValues;

  const chartConfig: any = {
    type: this.chartType,
    data: {
      labels: labels,
      datasets: [{
        label: 'NAV Value',
        data: data,
        borderColor: '#007bff',
        backgroundColor: this.chartType === 'line' 
          ? 'rgba(0, 123, 255, 0.1)' 
          : 'rgba(0, 123, 255, 0.7)',
        pointBackgroundColor: '#007bff',
        pointBorderColor: '#fff',
        pointHoverBackgroundColor: '#fff',
        pointHoverBorderColor: '#007bff',
        pointBorderWidth: 2,
        pointHoverRadius: 6,
        pointRadius: 4,
        fill: this.chartType === 'line',
        tension: 0.4,
        borderWidth: 3,
        borderCapStyle: 'round',
        borderJoinStyle: 'round'
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        intersect: false,
        mode: 'index'
      },
      plugins: {
        legend: {
          position: 'top',
          labels: {
            usePointStyle: true,
            padding: 20,
            font: {
              size: 12,
              family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
            },
            color: '#2c3e50'
          }
        },
        tooltip: {
          mode: 'index',
          intersect: false,
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: '#fff',
          bodyColor: '#fff',
          borderColor: 'rgba(255, 255, 255, 0.2)',
          borderWidth: 1,
          cornerRadius: 6,
          padding: 12,
          displayColors: false,
          titleFont: {
            family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
            size: 12
          },
          bodyFont: {
            family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
            size: 12
          },
          callbacks: {
            label: (context: any) => {
              let label = context.dataset.label || '';
              if (label) {
                label += ': ';
              }
              if (context.parsed.y !== null) {
                label += new Intl.NumberFormat('en-IN', {
                  style: 'currency',
                  currency: 'INR',
                  minimumFractionDigits: 2
                }).format(context.parsed.y);
              }
              return label;
            }
          }
        }
      },
      scales: {
        x: {
          display: true,
          title: {
            display: true,
            text: 'Date',
            font: {
              size: 12,
              weight: 'bold',
              family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
            },
            color: '#2c3e50'
          },
          grid: {
            display: false,
            drawBorder: true,
            color: 'rgba(0, 0, 0, 0.1)'
          },
          ticks: {
            maxRotation: 45,
            minRotation: 45,
            font: {
              size: 10,
              family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
            },
            color: '#6c757d'
          }
        },
        y: {
          display: true,
          title: {
            display: true,
            text: 'NAV Value',
            font: {
              size: 12,
              weight: 'bold',
              family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
            },
            color: '#2c3e50'
          },
          ticks: {
            callback: function (value: any) {
              return value;
            },
            font: {
              size: 10,
              family: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
            },
            color: '#6c757d'
          },
          grid: {
            color: 'rgba(0, 0, 0, 0.1)',
            drawBorder: true
          }
        }
      },
      animation: {
        duration: 1000,
        easing: 'easeOutQuart'
      }
    }
  };

  this.chart = new Chart(ctx, chartConfig);
}


  private updateChart(): void {
    if (!this.chart) return;

    const labels = this.getFormattedLabels();
    const data = this.historicalData.navValues;

    this.chart.data.labels = labels;
    this.chart.data.datasets[0].data = data;
    this.chart.update();
  }

  private getFormattedLabels(): string[] {
    return this.historicalData.dates.map(date =>
      new Date(date).toLocaleDateString('en-IN', {
        day: '2-digit',
        month: 'short'
      })
    );
  }

  toggleChartType(): void {
    this.chartType = this.chartType === 'line' ? 'bar' : 'line';
    this.createChart();
  }

  // Helper methods for statistics
  getCurrentNav(): number {
    if (!this.historicalData?.navValues?.length) return 0;
    return this.historicalData.navValues[this.historicalData.navValues.length - 1];
  }

  getTotalChange(): number {
    if (!this.historicalData?.navValues?.length) return 0;
    const firstNav = this.historicalData.navValues[0];
    const lastNav = this.getCurrentNav();
    return ((lastNav - firstNav) / firstNav) * 100;
  }

  getMaxNav(): number {
    if (!this.historicalData?.navValues?.length) return 0;
    return Math.max(...this.historicalData.navValues);
  }
}