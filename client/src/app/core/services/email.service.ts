import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StoredEmailDto {
    id: number;
    userId: number;
    toEmail: string;
    subject: string;
    body: string;
    type: string;
    status: string;
    metadata?: any;
    createdAt: string;
    viewedAt?: string;
}

export interface ApiResponse<T> {
    success: boolean;
    message?: string;
    data?: T;
}

@Injectable({
    providedIn: 'root'
})
export class EmailService {
    private http = inject(HttpClient);
    apiUrl = 'https://localhost:5001/api/notifications';

    getUserEmails(userId: number): Observable<ApiResponse<StoredEmailDto[]>> {
        return this.http.get<ApiResponse<StoredEmailDto[]>>(`${this.apiUrl}/emails/${userId}`);
    }

    markEmailAsViewed(emailId: number): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(`${this.apiUrl}/emails/${emailId}/view`, {});
    }

    getAllEmails(): Observable<ApiResponse<StoredEmailDto[]>> {
        return this.http.get<ApiResponse<StoredEmailDto[]>>(`${this.apiUrl}/emails`);
    }

    getEmailById(emailId: number): Observable<ApiResponse<StoredEmailDto>> {
        return this.http.get<ApiResponse<StoredEmailDto>>(`${this.apiUrl}/emails/${emailId}/detail`);
    }

}