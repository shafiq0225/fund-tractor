import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResponse } from '../../shared/models/Amfi/ImportResponse';
import { ApiResponse, Scheme } from '../../shared/models/Amfi/Scheme';

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

}
