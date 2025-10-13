export interface FundData {
  name: string;
  crisilRank: number;
  returns: {
    _1m: number;
    _6m: number;
    _1y: number;
    yesterday: number;
    _1week: number;
  };
  monthlyReturns: number[];
}