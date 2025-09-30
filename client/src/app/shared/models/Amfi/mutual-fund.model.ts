// src/app/features/nav/nav-report/models/mutual-fund.model.ts
export interface MutualFund {
  id: number;
  name: string;
  rate1: number;
  percent1: number;
  rate2: number;
  percent2: number;
  change: number;
  rank: number;
  icon: string;
  color: string;
}