import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PaymentJobResponse {
  jobId: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = '/api';

  // limit = null  → מיליון רשומות (ברירת מחדל בשרת)
  // limit = 1000  → דמו על 1,000 רשומות
  runCalculations(limit?: number): Observable<PaymentJobResponse> {
    debugger;
    const url = limit != null
      ? `${this.baseUrl}/payment?limit=${limit}`
      : `${this.baseUrl}/payment`;
    return this.http.post<PaymentJobResponse>(url, {});
  }
}
