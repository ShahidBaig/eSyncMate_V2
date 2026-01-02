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

  getCustomerAlerts(customerId: number): Observable<any> {
    const params = new HttpParams().set('customerId', customerId.toString());
    return this.http.get<any>(`${this.apiUrl}api/Customers/getCustomerAlerts`, { params });
  }

  // POST: insert / update a single alert row
  saveCustomerAlert(model: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/Customers/saveCustomerAlert`, model);
  }

  getAlertConfigurations(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Customers/getAlertConfigurations`);
  }

  deleteCustomerAlert(model: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/Customers/deleteCustomerAlert`, model);
  }
}
