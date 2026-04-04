import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RouteTypesService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  saveRouteTypes(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/RouteTypes/createRouteTypes', connectorModel);
  }


  updateRouteTypes(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/RouteTypes/updateRouteTypes', connectorModel);
  }


  getRouteTypes(searchOption: string, searchValue: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get(`${this.apiUrl}api/RouteTypes/getRouteTypes`, { params });
  }

  getRouteTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/RouteTypes/getRoutesTypes`);
  }
}
