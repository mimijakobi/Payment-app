import { Component, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { Subscription } from 'rxjs';
import { PaymentService } from '../../services/payment.service';
import { PaymentStatusService } from '../../services/payment-status.service';
import { BenchmarkCompareService, CompareMethodResult } from '../../services/benchmark-compare.service';
import { DbTable } from '../../models/calculator.models';
import { ResultsComponent } from '../results/results.component';

@Component({
  selector: 'app-dashboard',
  imports: [NgFor, NgIf, ResultsComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private paymentSvc = inject(PaymentService);
  private statusSvc  = inject(PaymentStatusService);
  private compareSvc = inject(BenchmarkCompareService);

  // ── DB Schema ──
  dbTables: DbTable[] = [
    {
      name: 't_data',
      color: '#6366f1',
      columns: [
        { name: 'data_id', note: 'PK' },
        { name: 'a', note: 'Float' },
        { name: 'b', note: 'Float' },
        { name: 'c', note: 'Float' },
        { name: 'd', note: 'Float' },
      ],
    },
    {
      name: 't_targil',
      color: '#10b981',
      columns: [
        { name: 'targil_id', note: 'PK' },
        { name: 'targil', note: 'VARCHAR' },
        { name: 'tnai', note: 'VARCHAR' },
        { name: 'targil_false', note: 'VARCHAR' },
      ],
    },
    {
      name: 't_results',
      color: '#f59e0b',
      columns: [
        { name: 'resultsl_id', note: 'PK' },
        { name: 'data_id', note: 'FK' },
        { name: 'targil_id', note: 'FK' },
        { name: 'method', note: 'VARCHAR' },
        { name: 'result', note: 'Float' },
      ],
    },
    {
      name: 't_log',
      color: '#ec4899',
      columns: [
        { name: 'log_id', note: 'PK' },
        { name: 'targil_id', note: 'FK' },
        { name: 'method', note: 'VARCHAR' },
        { name: 'run_time', note: 'Float' },
      ],
    },
  ];

  // ── פאנל שיטת העבודה ──
  showMethodology = signal(false);

  openMethodology():  void { this.showMethodology.set(true);  }
  closeMethodology(): void { this.showMethodology.set(false); }

  // ── סקריפט מילוי נתונים ──
  showDataScript = signal(false);

  readonly dataScript =
`-- מחיקת נתונים קיימים בצורה בטוחה
DELETE FROM t_data;   -- עכשיו אפשר למחוק את הנתונים עצמם

-- איפוס המונה של ה-ID שיתחיל שוב מ-1
DBCC CHECKIDENT ('t_data', RESEED, 0);

-- הגדרת משתנה לספירה
DECLARE @Counter INT = 1;

-- תחילת לולאה שתרוץ מיליון פעמים
BEGIN TRANSACTION; -- שימוש ב-Transaction מאיץ את התהליך משמעותית
WHILE @Counter <= 1000000
BEGIN
    INSERT INTO t_data (a, b, c, d)
    VALUES (
        RAND() * 100, -- מספר אקראי עבור a
        RAND() * 100, -- מספר אקראי עבור b
        RAND() * 100, -- מספר אקראי עבור c
        RAND() * 100  -- מספר אקראי עבור d
    );

    SET @Counter = @Counter + 1;

    -- כל 50,000 שורות נבצע שמירה כדי לא להעמיס על הזיכרון
    IF @Counter % 50000 = 0
    BEGIN
        COMMIT;
        BEGIN TRANSACTION;
    END
END
COMMIT;

-- בדיקה כמה שורות נוצרו
SELECT COUNT(*) AS TotalRows FROM t_data;`;

  openDataScript():  void { this.showDataScript.set(true);  }
  closeDataScript(): void { this.showDataScript.set(false); }
  copyDataScript():  void { navigator.clipboard.writeText(this.dataScript); }

  // ── סקריפט הכנסת תרגילים ──
  showExerciseScript = signal(false);

  readonly exerciseScript =
`INSERT INTO t_targil (targil, tnai, targil_false)
SELECT v.targil, v.tnai, v.targil_false
FROM (
    VALUES
        ('a + b', NULL, NULL),
        ('c - d', NULL, NULL),
        ('b * d', NULL, NULL),
        ('(a + b) * 8', NULL, NULL),
        ('(c + 200) / 2', NULL, NULL),
        ('(d - b) * 3', NULL, NULL),
        ('b * 2', 'a > 5', 'b / 2'),
        ('a + 1', 'b = c', 'd - 2'),
        ('a + b', 'SQRT(c) > 5', 'a - b'),
        ('d * 10', 'LOG(b) < 2', 'd / 2')
) AS v(targil, tnai, targil_false)
WHERE NOT EXISTS (
    SELECT 1
    FROM t_targil t
    WHERE t.targil = v.targil
      AND ISNULL(t.tnai, '') = ISNULL(v.tnai, '')
      AND ISNULL(t.targil_false, '') = ISNULL(v.targil_false, '')
);`;

  openExerciseScript():  void { this.showExerciseScript.set(true);  }
  closeExerciseScript(): void { this.showExerciseScript.set(false); }
  copyExerciseScript():  void { navigator.clipboard.writeText(this.exerciseScript); }

  // ── UI State ──
  isProcessing    = signal(false);
  progressPercent = signal(0);
  resultMessage   = signal<string | null>(null);
  resultSuccess   = signal<boolean | null>(null);
  showCompareModal = signal(false);
  isComparing      = signal(false);
  compareResult    = signal<CompareMethodResult[] | null>(null);

  private currentJobId = '';
  private progressTimerId?: ReturnType<typeof setInterval>;
  private pollingSub?: Subscription;
  private compareSub?: Subscription;

  // ── הרצת חישובים ──
  onRunBenchmark(limit?: number): void {
    this.isProcessing.set(true);
    this.progressPercent.set(0);
    this.resultMessage.set(null);
    this.resultSuccess.set(null);
    this.compareResult.set(null);

    this.progressTimerId = setInterval(() => {
      const cur = this.progressPercent();
      if (cur < 99) {
        this.progressPercent.set(Math.min(cur + 2, 99));
      }
    }, 1500);

    this.paymentSvc.runCalculations(limit).subscribe({
      next: (response) => {
        this.currentJobId = response.jobId;

        if (response.status === 'completed') {
          this.progressPercent.set(100);
          this.stopProcessing(true);
          return;
        }
        if (response.status === 'failed') {
          this.stopProcessing(false);
          return;
        }
        this.pollingSub = this.statusSvc.pollStatus(response.jobId).subscribe({
          next: (statusRes) => {
            if (statusRes.status === 'completed') {
              this.progressPercent.set(100);
              this.stopProcessing(true);
            } else if (statusRes.status === 'failed') {
              this.stopProcessing(false);
            }
          },
          error: () => this.stopProcessing(false),
        });
      },
      error: () => this.stopProcessing(false),
    });
  }

  private stopProcessing(success: boolean): void {
    clearInterval(this.progressTimerId);
    this.progressTimerId = undefined;
    this.pollingSub?.unsubscribe();
    this.isProcessing.set(false);

    if (success) {
      this.showCompareModal.set(true);
    } else {
      this.resultSuccess.set(false);
      this.resultMessage.set('❌ אירעה שגיאה בעיבוד החישובים. אנא נסה שנית.');
    }
  }

  // ── פופאפ: לא ──
  onDeclineCompare(): void {
    this.showCompareModal.set(false);
    this.resultSuccess.set(true);
    this.resultMessage.set('✅ החישובים הושלמו בהצלחה!');
  }

  // ── פופאפ: כן ──
  onConfirmCompare(): void {
    this.showCompareModal.set(false);
    this.isComparing.set(true);

    this.compareSub = this.compareSvc.runComparison(this.currentJobId).subscribe({
      next: (result) => {
        this.compareResult.set(result as CompareMethodResult[]);
        this.isComparing.set(false);
        this.resultSuccess.set(true);
        this.resultMessage.set('✅ השוואת זמני הריצה הושלמה בהצלחה!');
      },
      error: () => {
        this.isComparing.set(false);
        this.resultSuccess.set(false);
        this.resultMessage.set('❌ שגיאה בביצוע ההשוואה. אנא נסה שנית.');
      },
    });
  }

  ngOnDestroy(): void {
    clearInterval(this.progressTimerId);
    this.pollingSub?.unsubscribe();
    this.compareSub?.unsubscribe();
  }

  // ── DB Schema helpers ──
  getColNote(note: string): { bg: string; color: string; border: string } {
    if (note === 'PK') return { bg: '#6366f118', color: '#818cf8', border: '#6366f130' };
    if (note === 'FK') return { bg: '#f59e0b18', color: '#fbbf24', border: '#f59e0b28' };
    return { bg: '#0f172a', color: '#475569', border: '#1e293b' };
  }

  onCardEnter(event: MouseEvent, color: string): void {
    const el = event.currentTarget as HTMLElement;
    el.style.transform = 'translateY(-2px)';
    el.style.boxShadow = `0 8px 24px ${color}18`;
  }

  onCardLeave(event: MouseEvent): void {
    const el = event.currentTarget as HTMLElement;
    el.style.transform = 'translateY(0)';
    el.style.boxShadow = 'none';
  }
}
