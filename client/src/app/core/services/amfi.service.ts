import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResponse } from '../../shared/models/Amfi/ImportResponse';

@Injectable({
  providedIn: 'root'
})
export class AmfiService {
  baseUrl = 'https://localhost:5001/api/amfi/';
  private http = inject(HttpClient);

  ImportNavFromUrl(fileUrl: string): Observable<ImportResponse> {
    return this.http.post<ImportResponse>(this.baseUrl + 'import/url', { fileUrl: fileUrl });
  }
}
