export interface Scheme {
  id: number;
  fundCode: string;
  schemeCode: string;
  schemeName: string;
  isApproved: boolean;
  approvedName: string;
  createdAt: string;
  lastUpdatedDate: string;
  isUpdating?: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}
