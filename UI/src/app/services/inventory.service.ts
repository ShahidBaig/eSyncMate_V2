import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Inventory, RouteLog } from '../models/models';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient, private jwt: JwtHelperService) { }

  getInventory(itemID: string, fromDate: string, toDate: string, status: string, customerID: string, routeType: string, pageNumber: number = 1, pageSize: number = 10) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<any>(this.apiUrl + 'api/v1/inventory/getInventory/' + itemID + '/' + fromDate + '/' + toDate + '/' + status + '/' + customerID + '/' + routeType, { params });
  }

  getInventoryFiles(customerId: string, itemId: string, batchId: string = '') {
    return this.http.get<Inventory[]>(this.apiUrl + 'api/v1/inventory/getInventoryFiles/' + customerId + '/' + itemId + '/' + batchId);
  }

  getbatchWise(batchID: string, itemID: string = '', pageNumber: number = 1, pageSize: number = 10) {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (itemID) {
      params = params.set('itemID', itemID);
    }
    return this.http.get<any>(this.apiUrl + 'api/v1/inventory/getBatchData/' + batchID, { params });
  }

  getBatchWiseItemID(itemID: string, batchID:string): Observable<any> {
    const url = `${this.apiUrl}api/v1/inventory/getBatchWiseItemID?ItemID=${itemID}&BatchID=${batchID}`;
    return this.http.get<any>(url);

  }

  getMergedDownloadItems(batchIDs: string[], itemID: string = '', pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    let params = new HttpParams()
      .set('batchIDs', batchIDs.join(','))
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (itemID) {
      params = params.set('itemID', itemID);
    }
    return this.http.get<any>(`${this.apiUrl}api/v1/inventory/getMergedDownloadItems`, { params });
  }

  getDownloadBatches(customerID: string, fromDate: string, toDate: string): Observable<any> {
    let params = new HttpParams()
      .set('customerID', customerID)
      .set('fromDate', fromDate)
      .set('toDate', toDate);
    return this.http.get<any>(`${this.apiUrl}api/v1/inventory/getDownloadBatches`, { params });
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getERPCustomers`);
  }

  getRouteTypes(customerID: string = ''): Observable<any> {
    let params = new HttpParams();
    if (customerID) params = params.set('customerID', customerID);
    return this.http.get(`${this.apiUrl}api/v1/inventory/getRouteTypes`, { params });
  }
}
