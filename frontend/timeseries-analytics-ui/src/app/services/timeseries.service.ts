import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AgGridRequest, AgGridResponse } from '../models/ag-grid-request.model';
import { TimeSeriesRecord } from '../models/timeseries-record.model';

@Injectable({
  providedIn: 'root'
})
export class TimeseriesService {
  private readonly apiUrl = `${environment.apiUrl}/TimeSeries`;

  constructor(private http: HttpClient) {}

  query(request: AgGridRequest): Observable<AgGridResponse<TimeSeriesRecord>> {
    return this.http.post<AgGridResponse<TimeSeriesRecord>>(
      `${this.apiUrl}/query`,
      request
    );
  }

  getRecordCount(): Observable<{ totalRecords: number }> {
    return this.http.get<{ totalRecords: number }>(
      `${environment.apiUrl}/DataSeed/count`
    );
  }

  generateData(recordCount: number): Observable<any> {
    return this.http.post(
      `${environment.apiUrl}/DataSeed/generate`,
      { recordCount }
    );
  }
}
