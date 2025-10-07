// services/mutual-fund.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { PerformanceMetric, PerformanceSummary, SchemeInfo, SchemePerformance } from '../../shared/models/Amfi/nav-performance.model';

@Injectable({
    providedIn: 'root'
})
export class MutualFundService {
    // private apiUrl = 'http://localhost:3000/api';

    // constructor(private http: HttpClient) { }

    // // getNavPerformance(schemeCode: number): Observable<SchemePerformance> {
    // //     // For now, using mock data. Replace with actual API call when backend is ready
    // //     return this.getMockPerformance();
    // // }

    // getPerformanceMetrics(schemeCode: number): Observable<{
    //     schemeInfo: SchemeInfo;
    //     performanceMetrics: PerformanceMetric[];
    //     summary: PerformanceSummary;
    // }> {
    //     return of(this.generatePerformanceMetrics());
    // }

    // // Enhanced mock data with more realistic calculations
    // private getMockPerformance(): Observable<SchemePerformance> {
    //     const mockData: SchemePerformance = {
    //         status: "success",
    //         schemeCode: "118650",
    //         schemeName: "Nippon India Multi Cap Fund - Direct Plan Growth Plan - Growth Option",
    //         fundHouse: "Nippon India Mutual Fund",
    //         currentNav: 328.5195,
    //         lastUpdated: "2025-10-01T00:00:00.000Z",
    //         performance: {
    //             yesterday: {
    //                 nav: 325.9885,
    //                 date: "2025-09-30T00:00:00.000Z",
    //                 change: 2.531,
    //                 changePercentage: 0.78,
    //                 isPositive: true
    //             },
    //             oneWeek: {
    //                 nav: 326.5076,
    //                 date: "2025-09-26T00:00:00.000Z",
    //                 change: 2.0119,
    //                 changePercentage: 0.62,
    //                 isPositive: true
    //             },
    //             oneMonth: {
    //                 nav: 334.7407,
    //                 date: "2025-09-15T00:00:00.000Z",
    //                 change: -6.2212,
    //                 changePercentage: -1.86,
    //                 isPositive: false
    //             },
    //             sixMonths: {
    //                 nav: 332.4098,
    //                 date: "2025-09-10T00:00:00.000Z",
    //                 change: -3.8903,
    //                 changePercentage: -1.17,
    //                 isPositive: false
    //             },
    //             oneYear: {
    //                 nav: 332.4098,
    //                 date: "2025-09-10T00:00:00.000Z",
    //                 change: -3.8903,
    //                 changePercentage: -1.17,
    //                 isPositive: false
    //             }
    //         },
    //         historicalData: {
    //             dates: [
    //                 "2025-09-10", "2025-09-11", "2025-09-12", "2025-09-15",
    //                 "2025-09-16", "2025-09-17", "2025-09-18", "2025-09-19",
    //                 "2025-09-22", "2025-09-23", "2025-09-24", "2025-09-25",
    //                 "2025-09-26", "2025-09-29", "2025-09-30", "2025-10-01"
    //             ],
    //             navValues: [
    //                 332.4098, 332.5612, 333.7632, 334.7407,
    //                 336.5344, 337.1076, 337.6803, 337.1834,
    //                 335.2770, 335.2467, 333.5333, 330.9942,
    //                 326.5076, 326.4572, 325.9885, 328.5195
    //             ]
    //         }
    //     };

    //     return of(mockData);
    // }

    // private generatePerformanceMetrics(): {
    //     schemeInfo: SchemeInfo;
    //     performanceMetrics: PerformanceMetric[];
    //     summary: PerformanceSummary;
    // } {
    //     const metrics: PerformanceMetric[] = [
    //         {
    //             period: 'Yesterday',
    //             navValue: 325.9885,
    //             change: 2.531,
    //             changePercentage: 0.78,
    //             trend: 'up',
    //             description: 'Compared to previous day'
    //         },
    //         {
    //             period: '1 Week',
    //             navValue: 326.5076,
    //             change: 2.0119,
    //             changePercentage: 0.62,
    //             trend: 'up',
    //             description: 'Last 7 days performance'
    //         },
    //         {
    //             period: '1 Month',
    //             navValue: 334.7407,
    //             change: -6.2212,
    //             changePercentage: -1.86,
    //             trend: 'down',
    //             description: 'Last 30 days performance'
    //         },
    //         {
    //             period: '6 Months',
    //             navValue: 332.4098,
    //             change: -3.8903,
    //             changePercentage: -1.17,
    //             trend: 'down',
    //             description: 'Last 6 months performance'
    //         },
    //         {
    //             period: '1 Year',
    //             navValue: 332.4098,
    //             change: -3.8903,
    //             changePercentage: -1.17,
    //             trend: 'down',
    //             description: 'Last 12 months performance'
    //         }
    //     ];

    //     const schemeInfo: SchemeInfo = {
    //         schemeCode: 118650,
    //         schemeName: "Nippon India Multi Cap Fund - Direct Plan Growth",
    //         fundHouse: "Nippon India Mutual Fund",
    //         currentNav: 328.5195,
    //         navDate: "2025-10-01"
    //     };

    //     const summary: PerformanceSummary = {
    //         overallTrend: 'mixed',
    //         bestPerformer: 'Yesterday',
    //         worstPerformer: '1 Month',
    //         totalReturn: -1.17
    //     };

    //     return { schemeInfo, performanceMetrics: metrics, summary };
    // }
}