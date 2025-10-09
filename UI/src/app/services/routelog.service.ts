import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Routes } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class RouteLogService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getSearchRoutelog(routeID: number,fromDate: string, toDate: string, message: string,types: string): Observable<any> {
    const url = `${this.apiUrl}api/Routes/getSearchRoutelog?RouteID=${routeID}&FromDate=${fromDate}&ToDate=${toDate}&Message=${message}&TypeName=${types}`;
    return this.http.get(url);
  }

  getRouteLog(ID: number): Observable<any> {
    const url = `${this.apiUrl}api/Routes/getRouteLogWithID?ID=${ID}`;
    return this.http.get(url);
  }
}
