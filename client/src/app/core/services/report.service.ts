// src/app/features/nav/nav-report/services/report.service.ts
import { Injectable } from '@angular/core';
import { MutualFund } from '../../shared/models/Amfi/mutual-fund.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private mutualFunds: MutualFund[] = [
    {
      id: 1,
      name: "HDFC Mutual Fund",
      type: "Hybrid Fund",
      scheme: "Equity Hybrid",
      rate1: 100.00,
      percent1: 0.65,
      rate2: 200.00,
      percent2: 100.00,
      change: 100.00,
      rank: 1,
      icon: "pie_chart",
      color: "yellow"
    },
    {
      id: 2,
      name: "FT",
      type: "Growth Fund",
      scheme: "Large Cap",
      rate1: 26.01,
      percent1: 0.66,
      rate2: 27.08,
      percent2: 0.80,
      change: 4.11,
      rank: 2,
      icon: "trending_up",
      color: "green"
    },
    {
      id: 3,
      name: "DSP Mutual Fund",
      type: "Equity Fund",
      scheme: "Mid Cap",
      rate1: 25.12,
      percent1: 0.65,
      rate2: 26.12,
      percent2: 0.75,
      change: 3.98,
      rank: 3,
      icon: "account_balance",
      color: "blue"
    },
    {
      id: 4,
      name: "ICICI Mutual Fund",
      type: "Debt Fund",
      scheme: "Corporate Bond",
      rate1: 52.45,
      percent1: 0.45,
      rate2: 53.20,
      percent2: 0.52,
      change: 1.43,
      rank: 4,
      icon: "pie_chart",
      color: "purple"
    },
    {
      id: 5,
      name: "SBI Mutual Fund",
      type: "Index Fund",
      scheme: "Nifty 50",
      rate1: 78.90,
      percent1: 0.32,
      rate2: 79.15,
      percent2: 0.35,
      change: 0.32,
      rank: 5,
      icon: "savings",
      color: "red"
    },
    {
      id: 6,
      name: "Axis Mutual Fund",
      type: "Equity Fund",
      scheme: "Small Cap",
      rate1: 45.60,
      percent1: 0.55,
      rate2: 45.20,
      percent2: 0.50,
      change: -0.88,
      rank: 6,
      icon: "show_chart",
      color: "blue"
    },
    {
      id: 7,
      name: "Kotak Mutual Fund",
      type: "Debt Fund",
      scheme: "Gilt Fund",
      rate1: 32.15,
      percent1: 0.40,
      rate2: 31.80,
      percent2: 0.38,
      change: -1.09,
      rank: 7,
      icon: "account_balance_wallet",
      color: "purple"
    },
    {
      id: 8,
      name: "Aditya Birla Mutual Fund",
      type: "Hybrid Fund",
      scheme: "Multi Asset",
      rate1: 68.90,
      percent1: 0.60,
      rate2: 67.50,
      percent2: 0.55,
      change: -2.03,
      rank: 8,
      icon: "pie_chart",
      color: "green"
    },
    {
      id: 9,
      name: "Nippon India Mutual Fund",
      type: "Small Cap Fund",
      scheme: "Small Cap",
      rate1: 120.45,
      percent1: 0.75,
      rate2: 118.20,
      percent2: 0.70,
      change: -1.87,
      rank: 9,
      icon: "trending_up",
      color: "orange"
    },
    {
      id: 10,
      name: "UTI Mutual Fund",
      type: "Large Cap Fund",
      scheme: "Blue Chip",
      rate1: 55.80,
      percent1: 0.48,
      rate2: 52.90,
      percent2: 0.42,
      change: -5.20,
      rank: 10,
      icon: "show_chart",
      color: "blue"
    }
  ];

  getMutualFunds(): MutualFund[] {
    return this.mutualFunds.sort((a, b) => b.change - a.change);
  }

  getTopPerformers(): MutualFund[] {
    return this.mutualFunds
      .filter(fund => fund.change >= 0)
      .sort((a, b) => b.change - a.change)
      .slice(0, 5);
  }

  getTopLosers(): MutualFund[] {
    return this.mutualFunds
      .filter(fund => fund.change < 0)
      .sort((a, b) => a.change - b.change)
      .slice(0, 5);
  }

  getFundsByScheme(): { scheme: string, funds: MutualFund[] }[] {
    const schemes = [...new Set(this.mutualFunds.map(fund => fund.scheme))];
    return schemes.map(scheme => ({
      scheme,
      funds: this.mutualFunds.filter(fund => fund.scheme === scheme)
    }));
  }

  getTopPerformersByScheme(): { scheme: string, performers: MutualFund[] }[] {
    const schemes = this.getFundsByScheme();
    return schemes.map(schemeData => ({
      scheme: schemeData.scheme,
      performers: schemeData.funds
        .filter(fund => fund.change >= 0)
        .sort((a, b) => b.change - a.change)
        .slice(0, 5)
    }));
  }

  getTopLosersByScheme(): { scheme: string, losers: MutualFund[] }[] {
    const schemes = this.getFundsByScheme();
    return schemes.map(schemeData => ({
      scheme: schemeData.scheme,
      losers: schemeData.funds
        .filter(fund => fund.change < 0)
        .sort((a, b) => a.change - b.change)
        .slice(0, 5)
    }));
  }

  getPerformanceSummary() {
    const bestPerformer = Math.max(...this.mutualFunds.map(fund => fund.change));
    const worstPerformer = Math.min(...this.mutualFunds.map(fund => fund.change));
    const avgReturn = this.mutualFunds.reduce((sum, fund) => sum + fund.change, 0) / this.mutualFunds.length;

    return {
      bestPerformer,
      worstPerformer,
      avgReturn,
      totalFunds: this.mutualFunds.length,
      positiveFunds: this.mutualFunds.filter(fund => fund.change >= 0).length,
      negativeFunds: this.mutualFunds.filter(fund => fund.change < 0).length
    };
  }
}