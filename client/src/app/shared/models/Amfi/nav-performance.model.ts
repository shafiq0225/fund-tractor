// models/nav-performance.model.ts
export interface NavPerformance {
  nav: number;
  date: string;
  change: number;
  changePercentage: number;
  isPositive: boolean;
}


export interface SchemePerformance {
  status: string;
  schemeCode: string; // Changed from number to string to match JSON
  schemeName: string;
  fundHouse: string;
  currentNav: number;
  lastUpdated: string;
  performance: {
    yesterday: NavPerformance;
    oneWeek: NavPerformance;
    oneMonth: NavPerformance;
    sixMonths: NavPerformance;
    oneYear: NavPerformance;
  };
  historicalData: {
    dates: string[];
    navValues: number[];
  };
}

export interface ApiResponse {
  message: string;
  data: SchemePerformance;
}


export interface PerformanceMetric {
  period: string;
  navValue: number;
  change: number;
  changePercentage: number;
  trend: 'up' | 'down';
  description: string;
}


export interface SchemeInfo {
  schemeCode: number;
  schemeName: string;
  fundHouse: string;
  currentNav: number;
  navDate: string;
}

export interface PerformanceSummary {
  overallTrend: 'up' | 'down' | 'mixed';
  bestPerformer: string;
  worstPerformer: string;
  totalReturn: number;
}
