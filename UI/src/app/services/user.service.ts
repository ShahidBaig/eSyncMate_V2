import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  updateUser(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/User/updateUser', connectorModel);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/User/deleteUser', { id });
  }

  getUsers(searchOption: string, searchValue: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get(`${this.apiUrl}api/User/getUser`, { params });
  }

  getRouteTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/RouteTypes/getRoutesTypes`);
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/ProductUploadPrices/getERPCustomers`);
  }

}
