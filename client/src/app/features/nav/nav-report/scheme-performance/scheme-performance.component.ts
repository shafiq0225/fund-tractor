import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { ApiResponse, PerformanceMetric, PerformanceSummary, SchemePerformance } from '../../../../shared/models/Amfi/nav-performance.model';
import { CommonModule } from '@angular/common';
import { PerformanceCardComponent } from "./performance-card/performance-card.component";
import { NavChartComponent } from "./nav-chart/nav-chart.component";
import { ActivatedRoute } from '@angular/router';
import { AmfiService } from '../../../../core/services/amfi.service';
import { BreadcrumbComponent } from "../../../../shared/components/breadcrumb/breadcrumb.component";
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-scheme-performance',
  imports: [CommonModule, PerformanceCardComponent, NavChartComponent, BreadcrumbComponent],
  templateUrl: './scheme-performance.component.html',
  styleUrl: './scheme-performance.component.scss'
})
export class SchemePerformanceComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>(); // Add this for cleanup

  schemeCode!: string;
  schemePerformance: SchemePerformance | null = null;
  performanceMetrics: PerformanceMetric[] = [];
  performanceSummary: PerformanceSummary | null = null;
  loading = false;
  error = '';
  lastUpdated = '';
  reportService = inject(AmfiService);
  currentSlide = 0;
  slidesToShow = 3; // Default number of cards to show
  slideWidth = 100 / 3; // Default slide width (33.33%)
  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParams.pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (params) => {
        this.schemeCode = params['scheme'];
        console.log("Scheme Code:", this.schemeCode);

        if (this.schemeCode) {
          this.loadPerformanceData(this.schemeCode);
        } else {
          this.error = 'No scheme code provided';
        }
      },
      error: (error) => {
        console.error('Route params error:', error);
        this.error = 'Failed to read route parameters';
      }
    });

    // Update slides on window resize
    this.updateSlidesToShow();
    window.addEventListener('resize', this.updateSlidesToShow.bind(this));
  }


  ngOnDestroy(): void {
    console.log('🔴 SchemePerformanceComponent destroyed');
    this.destroy$.next();
    this.destroy$.complete();
    window.removeEventListener('resize', this.updateSlidesToShow.bind(this));
  }


  // Carousel Methods
  slideNext(): void {
    const maxSlide = this.performanceMetrics.length - this.slidesToShow;
    if (this.currentSlide < maxSlide) {
      this.currentSlide++;
    }
  }

  slidePrev(): void {
    if (this.currentSlide > 0) {
      this.currentSlide--;
    }
  }

  goToSlide(index: number): void {
    const maxSlide = this.performanceMetrics.length - this.slidesToShow;
    this.currentSlide = Math.min(index, maxSlide);
  }

  getVisibleSlides(): any[] {
    const totalSlides = Math.max(1, this.performanceMetrics.length - this.slidesToShow + 1);
    return Array(totalSlides).fill(0);
  }

  updateSlidesToShow(): void {
    const width = window.innerWidth;

    if (width < 768) {
      this.slidesToShow = 1;
    } else if (width < 1024) {
      this.slidesToShow = 2;
    } else {
      this.slidesToShow = 3;
    }

    // Adjust slide width to fill available space
    this.slideWidth = 100 / this.slidesToShow;

    // Reset current slide if it's beyond new limits
    const maxSlide = Math.max(0, this.performanceMetrics.length - this.slidesToShow);
    if (this.currentSlide > maxSlide) {
      this.currentSlide = maxSlide;
    }
  }

  loadPerformanceData(schemeCode: string): void {
    if (!schemeCode) return;

    this.loading = true;
    this.error = '';

    this.reportService.getNavPerformance(schemeCode).pipe(
      takeUntil(this.destroy$) // Auto-unsubscribe when component destroys
    ).subscribe({
      next: (data: ApiResponse) => {
        // Check if component is still active
        if (this.destroy$.isStopped) return;

        this.schemePerformance = data.data;
        this.performanceMetrics = this.getPerformanceMetrics();
        this.performanceSummary = this.calculateSummary();
        this.lastUpdated = data.data.lastUpdated;
        this.loading = false;
      },
      error: (error) => {
        if (this.destroy$.isStopped) return;

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
