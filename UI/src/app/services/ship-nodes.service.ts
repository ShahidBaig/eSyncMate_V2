import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class ShipNodesService {
  apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getShipNodes(source: string, customerID: string, searchValue: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('source', source)
      .set('customerID', customerID)
      .set('searchValue', searchValue)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}api/ShipNodes/getShipNodes`, { params });
  }

  getWarehouses(source: string): Observable<any> {
    const params = new HttpParams().set('source', source);
    return this.http.get<any>(`${this.apiUrl}api/ShipNodes/getWarehouses`, { params });
  }

  /** Only customers that already have ship nodes — for the list screen filter. */
  getCustomers(source: string): Observable<any> {
    const params = new HttpParams().set('source', source);
    return this.http.get<any>(`${this.apiUrl}api/ShipNodes/getCustomers`, { params });
  }

  saveShipNode(shipNodeModel: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/ShipNodes/createShipNode`, shipNodeModel);
  }

  updateShipNode(shipNodeModel: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}api/ShipNodes/updateShipNode`, shipNodeModel);
  }

  // POST, not DELETE — the Kong proxy blocks DELETE
  deleteShipNode(id: number, source: string): Observable<any> {
    const params = new HttpParams().set('source', source);
    return this.http.post<any>(`${this.apiUrl}api/ShipNodes/deleteShipNode/${id}`, {}, { params });
  }
}
