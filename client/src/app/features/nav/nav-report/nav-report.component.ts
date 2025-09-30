import { Component, inject, OnInit } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { ReportService } from '../../../core/services/report.service';
import { MutualFund } from '../../../shared/models/Amfi/mutual-fund.model';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIcon } from '@angular/material/icon';
import { AmfiService, SchemeDto, SchemeResponseDto } from '../../../core/services/amfi.service';

interface FundUI {
  rank: number;
  name: string;
  rate1: number;
  percent1: number;
  rate2: number;
  percent2: number;
  change: number;
}
@Component({
  selector: 'app-nav-report',
  imports: [MatIcon, MatCard, MatCardHeader, MatCardTitle, MatCardSubtitle, MatCardContent, CommonModule],
  templateUrl: './nav-report.component.html',
  styleUrls: ['./nav-report.component.scss']
})
export class NavReportComponent implements OnInit {
  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }
  allFunds: FundUI[] = [];
  currentTime = new Date();
  startDate = '';
  endDate = '';
  ngOnInit(): void {
    this.fetchSchemes();
  }

  private mapSchemeToFundUI(s: SchemeDto): FundUI {
    const h1 = s.history[0];
    const h2 = s.history[1];

    const rate1 = h1?.nav ?? 0;
    const percent1 = parseFloat(h1?.percentage ?? '0');
    const rate2 = h2?.nav ?? 0;
    const percent2 = parseFloat(h2?.percentage ?? '0');

    // Calculate change between latest and older NAV
    const change = rate1 !== 0 ? ((rate2 - rate1) / rate1) * 100 : 0;

    return {
      rank: s.rank,
      name: s.schemeName,
      rate1,
      percent1,
      rate2,
      percent2,
      change
    };
  }

  fetchSchemes() {
    this.reportService.GetTodayAndPreviousWorkingDaySchemes().subscribe({
      next: (res: SchemeResponseDto) => {
        this.startDate = res.startDate;
        this.endDate = res.endDate;
        this.allFunds = res.schemes
          .map((s: SchemeDto) => this.mapSchemeToFundUI(s));
      },
      error: (err) => {
        console.error('Error fetching schemes:', err);
      }
    });
  }

  private reportService = inject(AmfiService);

  getFundRankClass(fund: FundUI): string {
    if (fund.rank === 1) return 'bg-yellow-100 text-yellow-800';
    if (fund.rank <= 3) return 'bg-green-100 text-green-800';
    if (fund.change >= 0) return 'bg-blue-100 text-blue-800';
    return 'bg-red-100 text-red-800';
  }


  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

}
