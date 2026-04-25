import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval } from 'rxjs';
import { switchMap, takeWhile, share } from 'rxjs/operators';

export interface JobStatusResponse {
  jobId: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentStatusService {
  private http = inject(HttpClient);
  private baseUrl = '/api';

  pollStatus(jobId: string): Observable<JobStatusResponse> {
    return interval(2000).pipe(
      switchMap(() =>
        this.http.get<JobStatusResponse>(`${this.baseUrl}/payment/status/${jobId}`)
      ),
      takeWhile(
        (res) => res.status !== 'completed' && res.status !== 'failed',
        true
      ),
      share()
    );
  }
}
