import { Component, inject, OnInit } from '@angular/core';
import { MatCard, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { ReportService } from '../../../core/services/report.service';
import { MutualFund } from '../../../shared/models/Amfi/mutual-fund.model';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-nav-report',
  imports: [MatIcon, MatCard, MatCardHeader, MatCardTitle, MatCardSubtitle, MatCardContent, CommonModule],
  templateUrl: './nav-report.component.html',
  styleUrl: './nav-report.component.scss'
})
export class NavReportComponent implements OnInit {
  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.allFunds = this.reportService.getMutualFunds();
    console.log(this.allFunds);

  }
  private reportService = inject(ReportService);
  allFunds: MutualFund[] = [];
  currentTime: Date = new Date();

  getFundRankClass(fund: MutualFund): string {
    if (fund.rank === 1) return 'bg-yellow-100 text-yellow-800';
    if (fund.rank <= 3) return 'bg-green-100 text-green-800';
    if (fund.change >= 0) return 'bg-blue-100 text-blue-800';
    return 'bg-red-100 text-red-800';
  }


  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }
}
