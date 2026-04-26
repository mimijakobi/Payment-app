import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PaymentJobResponse {
  jobId: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // limit = null  → מיליון רשומות (ברירת מחדל בשרת)
  // limit = 1000  → דמו על 1,000 רשומות
  runCalculations(limit?: number): Observable<PaymentJobResponse> {
    const url = limit != null
      ? `${this.baseUrl}/Payment?limit=${limit}`
      : `${this.baseUrl}/Payment`;
      debugger;
    return this.http.post<PaymentJobResponse>(url, {});
  }
}
