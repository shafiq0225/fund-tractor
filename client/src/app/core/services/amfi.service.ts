import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, delay, Observable, retryWhen, tap, throwError } from 'rxjs';
import { ImportResponse } from '../../shared/models/Amfi/ImportResponse';
import { ApiResponse, Scheme } from '../../shared/models/Amfi/Scheme';
import { AddSchemeRequest } from '../../shared/models/Amfi/AddSchemeRequest';

interface UpdateFundRequest {
  fundId: string;
  isApproved: boolean;
}

interface UpdateFundResponse {
  fundId: string;
  isApproved: boolean;
  success: boolean;
  message: string;
}

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
  date: string;          // ISO date string
  nav: number;
  percentage: string;    // string because backend sends formatted values like "0.07" or "-1.36"
  isTradingHoliday: boolean;
  isGrowth: boolean;
}



@Injectable({
  providedIn: 'root'
})
export class AmfiService {
  baseUrl = 'https://localhost:5001/api/amfi/';
  private http = inject(HttpClient);

  downloadAndSaveFromUrl(fileUrl: string): Observable<ImportResponse> {
    return this.http.post<ImportResponse>(this.baseUrl + 'import/url', { fileUrl: fileUrl });
  }

  uploadAndSaveFromFile(file: File): Observable<ImportResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportResponse>(this.baseUrl + 'import/file', formData);
  }

  getSchemes(): Observable<ApiResponse<Scheme[]>> {
    return this.http.get<ApiResponse<Scheme[]>>(this.baseUrl + 'schemeslist');
  }

  // future will go thisapproach to handle retries and errors
  // getSchemes() {
  //   return this.http.get<ApiResponse<Scheme[]>>(this.baseUrl + 'schemeslist').pipe(
  //     retryWhen(errors =>
  //       errors.pipe(
  //         tap(err => console.log('API failed, retrying in 3s...', err)),
  //         delay(3000) // wait 3 seconds before retry
  //       )
  //     ),
  //     catchError(err => {
  //       console.error('Still failing', err);
  //       return throwError(() => err); // handle or show error to UI
  //     })
  //   );
  // }


  updateSchemeApproval(fundId: string, schemeId: string, isApproved: boolean): Observable<any> {
    const body = { fundId, schemeId, isApproved };
    return this.http.put<any>(this.baseUrl + 'updateapprovedscheme', body);
  }

  updateApprovedFund(fundId: string, isApproved: boolean): Observable<UpdateFundResponse> {
    const payload: UpdateFundRequest = { fundId, isApproved };
    return this.http.put<UpdateFundResponse>(this.baseUrl + 'updateapprovedfund', payload);
  }

  addScheme(addSchemeRequest: AddSchemeRequest): Observable<AddSchemeRequest> {
    return this.http.post<AddSchemeRequest>(this.baseUrl + 'addapprovedscheme', addSchemeRequest);
  }

  GetTodayAndPreviousWorkingDaySchemes(): Observable<SchemeResponseDto> {
    return this.http.get<SchemeResponseDto>(this.baseUrl + 'schemes/today');
  }
}
