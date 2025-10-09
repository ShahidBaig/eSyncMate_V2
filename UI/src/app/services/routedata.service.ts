import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Routes } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class RouteDataService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getSearchRouteData(routeID: number,fromDate: string, toDate: string,type: string): Observable<any> {
    const url = `${this.apiUrl}api/Routes/getSearchRouteData?RouteID=${routeID}&FromDate=${fromDate}&ToDate=${toDate}&Type=${type}`;
    return this.http.get<any>(url);

  }

  getDataRoute(ID: number): Observable<any> {
    const url = `${this.apiUrl}api/Routes/getDataRoute?ID=${ID}`;
    return this.http.get(url);
  }

}
