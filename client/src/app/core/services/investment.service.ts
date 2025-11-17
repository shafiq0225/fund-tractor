import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateInvestmentDto {
  investorId: number;
  schemeCode: string;
  schemeName: string;
  fundName: string;
  navRate: number;
  dateOfPurchase: string;
  investAmount: number;
  modeOfInvestment: string;
  remarks?: string;
}

export interface InvestmentResponse {
  success: boolean;
  message: string;
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class InvestmentService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:5001/api';

  createInvestment(investmentData: CreateInvestmentDto): Observable<InvestmentResponse> {
    return this.http.post<InvestmentResponse>(`${this.apiUrl}/investment`, investmentData);
  }

  getInvestmentsByInvestor(investorId: number): Observable<InvestmentResponse> {
    return this.http.get<InvestmentResponse>(`${this.apiUrl}/investment/investor/${investorId}`);
  }

  getAllInvestments(): Observable<InvestmentResponse> {
    return this.http.get<InvestmentResponse>(`${this.apiUrl}/investment`);
  }

  getInvestmentById(id: number): Observable<InvestmentResponse> {
    return this.http.get<InvestmentResponse>(`${this.apiUrl}/investment/${id}`);
  }

  updateInvestment(id: number, updateData: any): Observable<InvestmentResponse> {
    return this.http.put<InvestmentResponse>(`${this.apiUrl}/investment/${id}`, updateData);
  }

  deleteInvestment(id: number): Observable<InvestmentResponse> {
    return this.http.delete<InvestmentResponse>(`${this.apiUrl}/investment/${id}`);
  }
}