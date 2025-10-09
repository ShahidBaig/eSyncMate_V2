import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Routes } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class RoutesService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getRoutes(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue)
    return this.http.get(`${this.apiUrl}api/Routes/getRoutes`, { params });
  }

  saveRoute(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Routes/createRoute', connectorModel);
  }

  updateRoute(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Routes/updateRoute', connectorModel);
  }

  getPartnerGroups(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/Routes/getRoutes`, { params });
  }

  getCustomersData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/getCustomers`);
  }

  getConnectors(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/getConnectors`);
  }

  getMaps(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/getMaps`);
  }

  getRouteTypes(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/GetRouteTypes`);
  }

  getPartnerGroup(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/GetPartnerGroup`);
  }

  getRoutelog(RouteId: number): Observable<any> {
    const url = `${this.apiUrl}api/Routes/GetRouteLog?RouteId=${RouteId}`;
    return this.http.get(url);
  }
  getRouteData(RouteId: number): Observable<any> {
    const url = `${this.apiUrl}api/Routes/GetRouteData?RouteId=${RouteId}`;
    return this.http.get(url);
  }
  getRouteName(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Routes/getRouteName`);
  }
}
