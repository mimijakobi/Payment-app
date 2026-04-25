import {
  Component,
  Input,
  AfterViewInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  inject,
  signal,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser, NgFor, NgIf, DecimalPipe } from '@angular/common';
import { Chart, registerables } from 'chart.js';
import { CompareMethodResult } from '../../services/benchmark-compare.service';

Chart.register(...registerables);

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [NgFor, NgIf, DecimalPipe],
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss',
})
export class ResultsComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) data: CompareMethodResult[] = [];

  @ViewChild('totalTimeChart') totalTimeRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('speedRatioChart') speedRatioRef!: ElementRef<HTMLCanvasElement>;

  private platformId = inject(PLATFORM_ID);
  private charts: Chart[] = [];

  showSqlModal = signal(false);

  readonly sqlScript =
`declare @JOB NVARCHAR(MAX) = '27DF8494-5CA8-45C8-978B-F33600FE5044'
;with
   TotalResults AS (
        SELECT
            method,
            SUM(result) AS total_sum_results
        FROM results
        WHERE job_id = @JOB
        GROUP BY method
    ),
    SummaryResults AS (
        SELECT
            CASE
                WHEN MAX(total_sum_results) - MIN(total_sum_results) < 0.01
                    THEN 1
                ELSE 0
            END AS results_match
        FROM TotalResults
    )
    select * from SummaryResults`;

  openSqlModal():  void { this.showSqlModal.set(true);  }
  closeSqlModal(): void { this.showSqlModal.set(false); }

  copySql(): void {
    navigator.clipboard.writeText(this.sqlScript);
  }

  // ── is_fastest יכול להגיע כ-number (0/1) או boolean (true/false) ──
  private isFastest(m: CompareMethodResult): boolean {
    return !!m.is_fastest;
  }

  // ── Computed helpers ──

  get hasResultsMismatch(): boolean {
    return this.data.some((m) => !m.results_match);
  }

  get fastestMethod(): CompareMethodResult | undefined {
    return this.data.find((m) => this.isFastest(m));
  }

  get slowestMethod(): CompareMethodResult | undefined {
    return [...this.data].sort((a, b) => b.relative_speed - a.relative_speed)[0];
  }

  get sortedBySpeed(): CompareMethodResult[] {
    return [...this.data].sort((a, b) => a.total_run_time - b.total_run_time);
  }

  // ── Lifecycle ──

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId) || !this.data.length) return;
    setTimeout(() => this.buildCharts(), 0);
  }

  ngOnDestroy(): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];
  }

  // ── Charts ──

  private buildCharts(): void {
    this.buildTotalTimeChart();
    this.buildSpeedRatioChart();
  }

  private buildTotalTimeChart(): void {
    const canvas = this.totalTimeRef?.nativeElement;
    if (!canvas) return;

    const bg     = this.data.map((m) => this.isFastest(m) ? '#10b981cc' : '#6366f1cc');
    const border = this.data.map((m) => this.isFastest(m) ? '#10b981'   : '#6366f1');

    const chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: this.data.map((m) => m.method),
        datasets: [{
          label: 'זמן ריצה כולל (שניות)',
          data: this.data.map((m) => m.total_run_time),
          backgroundColor: bg,
          borderColor: border,
          borderWidth: 2,
          borderRadius: 10,
          borderSkipped: false,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: { label: (ctx) => ` ${ctx.raw} שניות` },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { font: { size: 13, weight: 'bold' } },
          },
          y: {
            beginAtZero: true,
            grid: { color: '#f1f5f9' },
            ticks: { callback: (v) => `${v}s` },
          },
        },
      },
    });

    this.charts.push(chart);
  }

  // גרף אופקי — הפוך מהגרף הראשון ומציג x1.0 / x2.0 / x3.0 על הציר
  private buildSpeedRatioChart(): void {
    const canvas = this.speedRatioRef?.nativeElement;
    if (!canvas) return;

    const sorted  = this.sortedBySpeed;                   // מוין מהיר → איטי
    const bg      = sorted.map((m) => this.isFastest(m) ? '#10b981cc' : '#f59e0bcc');
    const border  = sorted.map((m) => this.isFastest(m) ? '#10b981'   : '#f59e0b');

    const chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: sorted.map((m) => m.method),
        datasets: [{
          label: 'פי כמה איטי מהמהיר ביותר',
          data: sorted.map((m) => m.relative_speed),
          backgroundColor: bg,
          borderColor: border,
          borderWidth: 2,
          borderRadius: 10,
          borderSkipped: false,
        }],
      },
      options: {
        indexAxis: 'y',           // ← הופך לגרף אופקי
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const val = Number(ctx.raw);
                return val === 1 ? ' x1.00 — המהירה ביותר' : ` x${val.toFixed(2)} איטי יותר`;
              },
            },
          },
        },
        scales: {
          x: {
            beginAtZero: true,
            min: 0,
            grid: { color: '#f1f5f9' },
            ticks: { callback: (v) => `x${v}` },
          },
          y: {
            grid: { display: false },
            ticks: { font: { size: 13, weight: 'bold' } },
          },
        },
      },
    });

    this.charts.push(chart);
  }

  // ── Table helper ──

  isFastestRow(row: CompareMethodResult): boolean {
    return this.isFastest(row);
  }
}
