import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Customer } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class CustomersService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  saveCustomer(customerModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Customers/createCustomer', customerModel);
  }

  updateCustomer(customerModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Customers/updateCustomer', customerModel);
  }

  getCustomers(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue)
    return this.http.get(`${this.apiUrl}api/Customers/getCustomers`, { params });
  }

  getCustomersList(): Observable <any> {
    return this.http.get(`${this.apiUrl}api/Customers/getCustomersList`);
  }
}
