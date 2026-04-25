import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface FormulaItem {
  symbol: string;
  label: string;
  color: string;
}

@Component({
  selector: 'app-formula-sidebar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './formula-sidebar.component.html',
  styleUrl: './formula-sidebar.component.scss',
})
export class FormulaSidebarComponent {
  formulas: FormulaItem[] = [
    { symbol: 'π',  label: 'פאי',     color: '#14b8a6' },
    { symbol: 'Σ',  label: 'סיגמא',   color: '#3b82f6' },
    { symbol: '∞',  label: 'אינסוף',  color: '#10b981' },
    { symbol: '√',  label: 'שורש',    color: '#eab308' },
    { symbol: 'Δ',  label: 'דלתא',    color: '#ec4899' },
    { symbol: 'μ',  label: 'מיו',     color: '#8b5cf6' },
    { symbol: 'θ',  label: 'תטא',     color: '#f97316' },
    { symbol: 'λ',  label: 'למדא',    color: '#06b6d4' },
    { symbol: '∫',  label: 'אינטגרל', color: '#ef4444' },
    { symbol: 'φ',  label: 'פי',      color: '#6366f1' },
    { symbol: 'α',  label: 'אלפא',    color: '#84cc16' },
    { symbol: 'β',  label: 'בטא',     color: '#f43f5e' },
  ];
}
