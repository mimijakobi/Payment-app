import { Component } from '@angular/core';
import { FormulaSidebarComponent } from './components/formula-sidebar/formula-sidebar.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';

@Component({
  selector: 'app-root',
  imports: [FormulaSidebarComponent, DashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
