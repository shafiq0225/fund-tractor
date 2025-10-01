export interface SchemeResponseDto {
  startDate: string;   // ISO date string
  endDate: string;     // ISO date string
  message: string;
  schemes: SchemeDto[];
}

export interface SchemeDto {
  fundName: string;
  schemeCode: string;
  schemeName: string;
  history: SchemeHistoryDto[];
  rank: number;
}

export interface SchemeHistoryDto {
  date: string;
  nav: number;
  percentage: string;
  isTradingHoliday: boolean;
  isGrowth: boolean;
}

