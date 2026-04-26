import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CompareMethodResult {
  method: string;
  total_run_time: number;
  avg_run_time: number;
  relative_speed: number;
  is_fastest: 0 | 1;
  results_match: 0 | 1;
}

@Injectable({ providedIn: 'root' })
export class BenchmarkCompareService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  runComparison(jobId: string): Observable<CompareMethodResult[]> {
    return this.http.get<CompareMethodResult[]>(`${this.baseUrl}/Payment/report/summary/${jobId}`);
  }
}
