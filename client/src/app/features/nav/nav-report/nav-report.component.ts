import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { AmfiService } from '../../../core/services/amfi.service';
import { SchemeDto, SchemeResponseDto } from '../../../shared/models/Amfi/SchemeResponseDto';
import { Subject, takeUntil } from 'rxjs';
import { BreadcrumbComponent } from "../../../shared/components/breadcrumb/breadcrumb.component";
import { TruncatePipe } from "../../../shared/pipes/truncate-pipe";

interface FundUI {
  rank: number;
  name: string;
  rate1: number;
  percent1: number;
  rate2: number;
  percent2: number;
  change: number;
  absoluteChange: number;
}

@Component({
  selector: 'app-nav-report',
  imports: [
    MatIcon,
    MatCard,
    MatCardHeader,
    MatCardTitle,
    MatCardSubtitle,
    MatCardContent,
    CommonModule,
    MatTooltip,
    BreadcrumbComponent,
    TruncatePipe
],
  templateUrl: './nav-report.component.html',
  styleUrls: ['./nav-report.component.scss']
})
export class NavReportComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  allFunds: FundUI[] = [];
  filteredFunds: FundUI[] = [];
  currentTime = new Date();
  startDate = '';
  endDate = '';
  isLoading = true;
  errorMessage = '';

  // Statistics
  totalFunds = 0;
  positiveFunds = 0;
  negativeFunds = 0;
  bestPerformer: FundUI | null = null;
  worstPerformer: FundUI | null = null;

  private reportService = inject(AmfiService);

  ngOnInit(): void {
    this.getDailySchemes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private mapSchemeToFundUI(s: SchemeDto): FundUI {
    const h1 = s.history[0]; // Latest NAV
    const h2 = s.history[1]; // Previous NAV

    const rate1 = h1?.nav ?? 0;
    const percent1 = parseFloat(h1?.percentage ?? '0');
    const rate2 = h2?.nav ?? 0;
    const percent2 = parseFloat(h2?.percentage ?? '0');

    // Calculate change between latest and older NAV
    const change = rate1 !== 0 ? ((rate2 - rate1) / rate1) * 100 : 0;
    const absoluteChange = rate2 - rate1;

    return {
      rank: s.rank,
      name: s.schemeName,
      rate1,
      percent1,
      rate2,
      percent2,
      change,
      absoluteChange
    };
  }

  getDailySchemes(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.reportService.getDailySchemesWithRank()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: SchemeResponseDto) => {
          this.startDate = res.startDate;
          this.endDate = res.endDate;
          this.allFunds = res.schemes
            .map((s: SchemeDto) => this.mapSchemeToFundUI(s))
            .sort((a, b) => b.change - a.change); // Sort by performance descending

          this.calculateStatistics();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error fetching schemes:', err);
          this.errorMessage = 'Failed to load fund data. Please try again later.';
          this.isLoading = false;
        }
      });
  }

  private calculateStatistics(): void {
    this.totalFunds = this.allFunds.length;
    this.positiveFunds = this.allFunds.filter(fund => fund.change > 0).length;
    this.negativeFunds = this.allFunds.filter(fund => fund.change < 0).length;

    if (this.allFunds.length > 0) {
      this.bestPerformer = [...this.allFunds].sort((a, b) => b.change - a.change)[0];
      this.worstPerformer = [...this.allFunds].sort((a, b) => a.change - b.change)[0];
    }
  }

  getDateRangeDays(): number {
    if (!this.startDate || !this.endDate) return 0;
    const start = new Date(this.startDate);
    const end = new Date(this.endDate);
    return Math.ceil((end.getTime() - start.getTime()) / (1000 * 3600 * 24));
  }

  getPositiveFundsCount(): number {
    return this.positiveFunds;
  }

  getNegativeFundsCount(): number {
    return this.negativeFunds;
  }

  getFundRankClass(fund: FundUI): string {
    if (fund.rank === 1) return 'bg-gradient-to-r from-yellow-400 to-yellow-600 text-white shadow-lg';
    if (fund.rank === 2) return 'bg-gradient-to-r from-gray-400 to-gray-600 text-white shadow-md';
    if (fund.rank === 3) return 'bg-gradient-to-r from-amber-600 to-amber-800 text-white shadow-md';
    if (fund.change >= 5) return 'bg-green-100 text-green-800 border border-green-300';
    if (fund.change >= 0) return 'bg-blue-50 text-blue-800 border border-blue-200';
    return 'bg-red-50 text-red-800 border border-red-200';
  }

  getPerformanceColor(change: number): string {
    if (change > 5) return 'text-green-600';
    if (change > 2) return 'text-green-500';
    if (change > 0) return 'text-blue-500';
    if (change > -2) return 'text-orange-500';
    return 'text-red-600';
  }

}