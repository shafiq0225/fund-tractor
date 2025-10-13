import { Component, inject, OnInit } from '@angular/core';
import { ApiResponse, PerformanceMetric, PerformanceSummary, SchemePerformance } from '../../../../shared/models/Amfi/nav-performance.model';
import { CommonModule } from '@angular/common';
import { PerformanceCardComponent } from "./performance-card/performance-card.component";
import { NavChartComponent } from "./nav-chart/nav-chart.component";
import { ActivatedRoute } from '@angular/router';
import { AmfiService } from '../../../../core/services/amfi.service';
import { BreadcrumbComponent } from "../../../../shared/components/breadcrumb/breadcrumb.component";

@Component({
  selector: 'app-scheme-performance',
  imports: [CommonModule, PerformanceCardComponent, NavChartComponent, BreadcrumbComponent],
  templateUrl: './scheme-performance.component.html',
  styleUrl: './scheme-performance.component.scss'
})
export class SchemePerformanceComponent implements OnInit {
  schemeCode!: string;
  schemePerformance: SchemePerformance | null = null;
  performanceMetrics: PerformanceMetric[] = [];
  performanceSummary: PerformanceSummary | null = null;
  loading = false;
  error = '';
  lastUpdated = '';
  reportService = inject(AmfiService);
  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.schemeCode = params['scheme'];
      console.log("Scheme Code:", this.schemeCode);
      // 🔥 Call API to load scheme details here
      this.loadPerformanceData(this.schemeCode);
    });

  }

  loadPerformanceData(schemeCode: string): void {
    this.loading = true;
    this.error = '';

    this.reportService.getNavPerformance(schemeCode).subscribe({
      next: (data: ApiResponse) => {

        console.log('API Response:', data); // Add this line
        console.log('Performance data:', data?.data.performance); // Add this line
        console.log('Yesterday data:', data?.data.performance?.yesterday); // Add this line

        this.schemePerformance = data.data;
        this.performanceMetrics = this.getPerformanceMetrics();
        this.performanceSummary = this.calculateSummary();
        this.lastUpdated = data.data.lastUpdated;
        this.loading = false;
      },
      error: (error) => {
        this.error = 'Failed to load performance data. Please try again later.';
        this.loading = false;
        console.error('Error loading performance data:', error);
      }
    });
  }

  getPerformanceMetrics(): PerformanceMetric[] {
    if (!this.schemePerformance || !this.schemePerformance.performance) {
      console.warn('Performance data is not available');
      return [];
    }

    const performance = this.schemePerformance.performance;

    // Create metrics with safe navigation and fallbacks
    const metrics = [
      {
        period: 'Yesterday',
        navValue: performance.yesterday?.nav || 0,
        change: performance.yesterday?.change || 0,
        changePercentage: performance.yesterday?.changePercentage || 0,
        trend: (performance.yesterday?.isPositive ? 'up' : 'down') as 'up' | 'down',
        description: 'Compared to previous day'
      },
      {
        period: '1 Week',
        navValue: performance.oneWeek?.nav || 0,
        change: performance.oneWeek?.change || 0,
        changePercentage: performance.oneWeek?.changePercentage || 0,
        trend: (performance.oneWeek?.isPositive ? 'up' : 'down') as 'up' | 'down',
        description: 'Last 7 days performance'
      },
      {
        period: '1 Month',
        navValue: performance.oneMonth?.nav || 0,
        change: performance.oneMonth?.change || 0,
        changePercentage: performance.oneMonth?.changePercentage || 0,
        trend: (performance.oneMonth?.isPositive ? 'up' : 'down') as 'up' | 'down',
        description: 'Last 30 days performance'
      },
      {
        period: '6 Months',
        navValue: performance.sixMonths?.nav || 0,
        change: performance.sixMonths?.change || 0,
        changePercentage: performance.sixMonths?.changePercentage || 0,
        trend: (performance.sixMonths?.isPositive ? 'up' : 'down') as 'up' | 'down',
        description: 'Last 6 months performance'
      },
      {
        period: '1 Year',
        navValue: performance.oneYear?.nav || 0,
        change: performance.oneYear?.change || 0,
        changePercentage: performance.oneYear?.changePercentage || 0,
        trend: (performance.oneYear?.isPositive ? 'up' : 'down') as 'up' | 'down',
        description: 'Last 12 months performance'
      }
    ];

    console.log('Generated metrics:', metrics); // Debug log
    return metrics;
  }


  calculateSummary(): PerformanceSummary {
    if (!this.performanceMetrics.length) {
      return {
        overallTrend: 'mixed',
        bestPerformer: 'N/A',
        worstPerformer: 'N/A',
        totalReturn: 0
      };
    }

    const positiveMetrics = this.performanceMetrics.filter(m => m.trend === 'up').length;
    const negativeMetrics = this.performanceMetrics.filter(m => m.trend === 'down').length;

    let overallTrend: 'up' | 'down' | 'mixed' = 'mixed';
    if (positiveMetrics === this.performanceMetrics.length) overallTrend = 'up';
    if (negativeMetrics === this.performanceMetrics.length) overallTrend = 'down';

    const bestPerformer = [...this.performanceMetrics]
      .filter(m => m.trend === 'up')
      .sort((a, b) => b.changePercentage - a.changePercentage)[0]?.period || 'N/A';

    const worstPerformer = [...this.performanceMetrics]
      .filter(m => m.trend === 'down')
      .sort((a, b) => a.changePercentage - b.changePercentage)[0]?.period || 'N/A';

    const totalReturn = this.performanceMetrics.find(m => m.period === '1 Year')?.changePercentage || 0;

    return {
      overallTrend,
      bestPerformer,
      worstPerformer,
      totalReturn
    };
  }

  refreshData(): void {
    this.loadPerformanceData(this.schemeCode);
  }

  getTrendIcon(trend: string): string {
    switch (trend) {
      case 'up': return '📈';
      case 'down': return '📉';
      default: return '➡️';
    }
  }

  getTrendClass(trend: string): string {
    switch (trend) {
      case 'up': return 'trend-up';
      case 'down': return 'trend-down';
      default: return 'trend-mixed';
    }
  }

  // components/nav-performance/nav-performance.component.ts
  // Add this method to the existing component
  getChartData(): any {
    if (!this.schemePerformance) return null;

    return {
      dates: this.schemePerformance.historicalData.dates,
      navValues: this.schemePerformance.historicalData.navValues
    };
  }

}
