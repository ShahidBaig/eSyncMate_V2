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


  getUsers(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/User/getUser`, { params });
  }

  getRouteTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/RouteTypes/getRoutesTypes`);
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/ProductUploadPrices/getERPCustomers`);
  }
}
