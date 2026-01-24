import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {  DataGridComponent } from "./components/data-grid/data-grid.component";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet,  DataGridComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('timeseries-analytics-ui');
}
