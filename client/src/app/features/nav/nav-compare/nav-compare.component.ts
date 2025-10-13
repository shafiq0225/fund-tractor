import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ChartCardComponent } from './chart-card/chart-card.component';
import { MatSortModule } from '@angular/material/sort';
import { BreadcrumbComponent } from "../../../shared/components/breadcrumb/breadcrumb.component";
import { AmfiService } from '../../../core/services/amfi.service';
import { Scheme, ApiResponse } from '../../../shared/models/Amfi/Scheme';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { FundData } from '../../../shared/models/Amfi/fund.model';



@Component({
  selector: 'app-nav-compare',
  templateUrl: './nav-compare.component.html',
  styleUrls: ['./nav-compare.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCheckboxModule,
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatProgressSpinnerModule,
    ChartCardComponent,
    BreadcrumbComponent,
    MatIcon,
    MatTooltip
  ]
})
export class NavCompareComponent implements OnInit {
  schemes: Scheme[] = [];
  selectedSchemes: Scheme[] = [];
  comparisonData: FundData[] = [];
  isComparisonVisible = false;
  isLoading = false;
  isComparing = false;

  // Chart data
  returnsChartData: any;
  aumChartData: any;
  trendChartData: any;
  riskReturnChartData: any;

  // Chart options
  returnsChartOptions: any;
  aumChartOptions: any;
  trendChartOptions: any;
  riskReturnChartOptions: any;

  // Table columns - updated to match API response keys
  displayedColumns: string[] = ['name', 'crisilRank', 'yesterday', '_1week', '_1m', '_6m', '_1y'];

  // Return periods for table - updated to match API response keys exactly
  returnPeriods = [
    { key: 'yesterday', label: 'Yesterday' },
    { key: '_1week', label: '1 Week' },
    { key: '_1m', label: '1M' },
    { key: '_6m', label: '6M' },
    { key: '_1y', label: '1Y', last: true }
  ];

  constructor(private amfiService: AmfiService) { }

  ngOnInit() {
    this.loadSchemes();
    this.initializeChartOptions();
  }

  private loadSchemes() {
    this.isLoading = true;
    this.amfiService.getSchemes().subscribe({
      next: (response: ApiResponse<Scheme[]>) => {
        this.schemes = response.data.filter(scheme => scheme.isApproved);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading schemes:', error);
        this.isLoading = false;
      }
    });
  }

  private initializeChartOptions() {
    // Returns Chart Options
    this.returnsChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'top' as const },
        tooltip: {
          callbacks: {
            label: (context: any) => `${context.dataset.label}: ${context.raw}%`
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          title: { display: true, text: 'Returns (%)' }
        }
      }
    };

    // AUM Chart Options
    this.aumChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'bottom' as const },
        tooltip: {
          callbacks: {
            label: (context: any) => `AUM: ₹${context.raw.toLocaleString('en-IN')} Cr`
          }
        }
      }
    };

    // Trend Chart Options
    this.trendChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'top' as const }
      },
      scales: {
        y: {
          title: { display: true, text: 'Monthly Return (%)' }
        }
      }
    };

    // Risk-Return Chart Options
    this.riskReturnChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'top' as const },
        tooltip: {
          callbacks: {
            label: (context: any) =>
              `${context.dataset.label}: Risk ${context.raw.x}%, Return ${context.raw.y}%`
          }
        }
      },
      scales: {
        x: {
          title: { display: true, text: 'Risk (%)' }
        },
        y: {
          title: { display: true, text: '1 Year Return (%)' }
        }
      }
    };
  }

  private lastComparedCodes: string[] = [];
  private isCompareInProgress = false;

  compareFunds() {
    if (this.selectedSchemes.length < 2 || this.selectedSchemes.length > 4) {
      return;
    }

    const schemeCodes = this.selectedSchemes.map(s => s.schemeCode);

    // 🔹 Prevent duplicate / same selection comparison
    if (this.arraysEqual(this.lastComparedCodes, schemeCodes)) {
      console.log('Same selection — skipping duplicate comparison call.');
      return;
    }

    // 🔹 Prevent rapid duplicate clicks
    if (this.isCompareInProgress) {
      console.log('Comparison already in progress — please wait.');
      return;
    }

    this.isCompareInProgress = true;
    this.isComparing = true;

    this.amfiService.getSchemeComparison(schemeCodes.join(',')).subscribe({
      next: (comparisonResponse) => {
        this.comparisonData = Object.values(comparisonResponse);
        this.isComparisonVisible = true;
        this.generateCharts();

        // Cache last compared codes for same-selection detection
        this.lastComparedCodes = [...schemeCodes];

        // Smooth scroll
        setTimeout(() => {
          const element = document.getElementById('comparisonSection');
          if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        }, 100);
      },
      error: (error) => {
        console.error('Error comparing schemes:', error);
      },
      complete: () => {
        this.isCompareInProgress = false;
        this.isComparing = false;
      }
    });
  }

  /** 🔸 Helper to check if two string arrays are identical */
  private arraysEqual(arr1: string[], arr2: string[]): boolean {
    if (arr1.length !== arr2.length) return false;
    return arr1.every((code, index) => code === arr2[index]);
  }


  removeScheme(scheme: Scheme): void {
    this.selectedSchemes = this.selectedSchemes.filter(s => s.schemeCode !== scheme.schemeCode);
  }

  // Helper methods for the template
  getSelectionStatus(): string {
    const count = this.selectedSchemes.length;
    switch (count) {
      case 0: return 'No funds selected';
      case 1: return '1 fund selected';
      case 2: return '2 funds selected - Ready!';
      case 3: return '3 funds selected - Ready!';
      case 4: return '4 funds selected - Maximum';
      default: return 'Maximum 4 funds allowed';
    }
  }

  private generateCharts() {
    this.generateReturnsChart();
    this.generateTrendChart();
  }

  private generateReturnsChart() {
    const colors = [
      'rgba(52, 152, 219, 0.7)',
      'rgba(46, 204, 113, 0.7)',
      'rgba(155, 89, 182, 0.7)',
      'rgba(241, 196, 15, 0.7)',
      'rgba(231, 76, 60, 0.7)'
    ];

    this.returnsChartData = {
      labels: this.selectedSchemes.map(scheme => scheme.schemeName),
      datasets: [
        {
          label: 'Yesterday Return (%)',
          data: this.comparisonData.map(fund => fund.returns.yesterday),
          backgroundColor: colors[0],
          borderColor: colors[0].replace('0.7', '1'),
          borderWidth: 1
        },
        {
          label: '1 Week Return (%)',
          data: this.comparisonData.map(fund => fund.returns._1week),
          backgroundColor: colors[1],
          borderColor: colors[1].replace('0.7', '1'),
          borderWidth: 1
        },
        {
          label: '1 Month Return (%)',
          data: this.comparisonData.map(fund => fund.returns._1m),
          backgroundColor: colors[2],
          borderColor: colors[2].replace('0.7', '1'),
          borderWidth: 1
        },
        {
          label: '6 Month Return (%)',
          data: this.comparisonData.map(fund => fund.returns._6m),
          backgroundColor: colors[3],
          borderColor: colors[3].replace('0.7', '1'),
          borderWidth: 1
        },
        {
          label: '1 Year Return (%)',
          data: this.comparisonData.map(fund => fund.returns._1y),
          backgroundColor: colors[4],
          borderColor: colors[4].replace('0.7', '1'),
          borderWidth: 1
        }
      ]
    };
  }

  private generateTrendChart() {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const colors = [
      'rgb(52, 152, 219)',
      'rgb(46, 204, 113)',
      'rgb(155, 89, 182)',
      'rgb(241, 196, 15)'
    ];

    this.trendChartData = {
      labels: months,
      datasets: this.comparisonData.map((fund, index) => ({
        label: fund.name,
        data: fund.monthlyReturns,
        borderColor: colors[index],
        backgroundColor: colors[index].replace('rgb', 'rgba').replace(')', ', 0.1)'),
        tension: 0.3,
        fill: false
      }))
    };
  }

  // Utility methods
  formatReturn(value: number): string {
    return value >= 0 ? `+${value.toFixed(2)}%` : `${value.toFixed(2)}%`;
  }

  getReturnClass(value: number): string {
    return value >= 0 ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold';
  }

  getFundColor(schemeName: string): string {
    const colors = [
      'bg-blue-500', 'bg-green-500', 'bg-purple-500', 'bg-orange-500',
      'bg-red-500', 'bg-indigo-500', 'bg-pink-500', 'bg-teal-500'
    ];
    const index = this.schemes.findIndex(s => s.schemeName === schemeName) % colors.length;
    return colors[index];
  }

  getReturnTrend(returns: any, periodKey: string): any {
    const value = returns[periodKey];

    if (value > 5) {
      return {
        icon: 'north', // Straight up arrow
        color: 'text-green-600 font-bold',
        text: 'Very Positive'
      };
    } else if (value > 0) {
      return {
        icon: 'north_east', // Diagonal up arrow
        color: 'text-green-500',
        text: 'Positive'
      };
    } else if (value < -5) {
      return {
        icon: 'south', // Straight down arrow
        color: 'text-red-600 font-bold',
        text: 'Very Negative'
      };
    } else if (value < 0) {
      return {
        icon: 'south_east', // Diagonal down arrow
        color: 'text-red-500',
        text: 'Negative'
      };
    }
    return {
      icon: 'horizontal_rule',
      color: 'text-gray-500',
      text: 'Neutral'
    };
  }

  // Scroll to top method
  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  getRankIcon(rank: number): string {
    const icons: { [key: number]: string } = {
      1: 'military_tech', // Trophy
      2: 'workspace_premium', // Medal
      3: 'emoji_events', // Award
      4: 'star', // Star
      5: 'flag' // Flag
    };
    return icons[rank] || 'circle';
  }

  getTrendIcon(returnValue: number): string {
    if (returnValue > 5) return 'trending_up';
    if (returnValue > 0) return 'north_east';
    if (returnValue < 0) return 'south_east';
    return 'remove';
  }

  getReturnColor(returnValue: number): string {
    if (returnValue > 5) return 'text-green-600 font-bold';
    if (returnValue > 0) return 'text-green-500';
    if (returnValue < 0) return 'text-red-500';
    return 'text-gray-500';
  }

  getRankClass(rank: number): string {
    const rankClasses: { [key: number]: string } = {
      1: 'bg-gradient-to-r from-amber-400 to-orange-500 text-white shadow',
      2: 'bg-gradient-to-r from-blue-500 to-blue-600 text-white shadow',
      3: 'bg-gradient-to-r from-green-500 to-green-600 text-white shadow',
      4: 'bg-gradient-to-r from-gray-400 to-gray-500 text-white',
      5: 'bg-gradient-to-r from-red-400 to-red-500 text-white'
    };
    return rankClasses[rank] || 'bg-gray-200 text-gray-700';
  }

  getRankText(rank: number): string {
    const rankTexts: { [key: number]: string } = {
      1: 'Top',
      2: 'Excellent',
      3: 'Good',
      4: 'Average'
    };
    return rankTexts[rank] || `Rank ${rank}`;
  }

  getRankTextClass(rank: number): string {
    const textClasses: { [key: number]: string } = {
      1: 'bg-amber-50 text-amber-600',
      2: 'bg-blue-50 text-blue-600',
      3: 'bg-green-50 text-green-600',
      4: 'bg-gray-50 text-gray-600',
      5: 'bg-red-50 text-red-600'
    };
    return textClasses[rank] || 'bg-gray-50 text-gray-500';
  }
}
