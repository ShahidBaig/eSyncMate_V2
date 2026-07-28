import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class ShipCodesService {
  apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getShipCodes(customerID: string, searchValue: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('customerID', customerID)
      .set('searchValue', searchValue)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}api/ShipCodes/getShipCodes`, { params });
  }

  saveShipCode(model: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/ShipCodes/createShipCode`, model);
  }

  updateShipCode(model: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/ShipCodes/updateShipCode`, model);
  }

  // POST, not DELETE — the Kong proxy blocks DELETE
  deleteShipCode(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/ShipCodes/deleteShipCode/${id}`, {});
  }
}
