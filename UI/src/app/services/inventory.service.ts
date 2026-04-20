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

  getInventory(itemID: string, fromDate: string, toDate: string, status: string, customerID: string, pageNumber: number = 1, pageSize: number = 10) {
    const body = {
      itemID: itemID || '',
      customerID: customerID || '',
      startDate: fromDate || '',
      finishDate: toDate || '',
      status: status || '',
      pageNumber,
      pageSize
    };
    return this.http.post<any>(this.apiUrl + 'api/v1/inventory/getInventory', body);
  }

  getInventoryFiles(customerId: string, itemId: string, batchId: string = '') {
    return this.http.get<Inventory[]>(this.apiUrl + 'api/v1/inventory/getInventoryFiles/' + customerId + '/' + itemId + '/' + batchId);
  }

  getBatchItems(batchIDs: string[], itemID: string = '', pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const body = {
      batchIDs: batchIDs.join(','),
      itemID: itemID || '',
      pageNumber,
      pageSize
    };
    return this.http.post<any>(`${this.apiUrl}api/v1/inventory/getBatchItems`, body);
  }

  getConsolidatedDownload(uploadBatchID: string, itemID: string = ''): Observable<any> {
    const body = { uploadBatchID, itemID };
    return this.http.post<any>(`${this.apiUrl}api/v1/inventory/getConsolidatedDownload`, body);
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
