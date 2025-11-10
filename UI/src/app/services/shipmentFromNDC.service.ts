import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Inventory, RouteLog } from '../models/models';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ShipmentFromNDCService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient, private jwt: JwtHelperService) { }

  getInventory(shipmentID: string, transactionDate: string, status: string, poNumber: string) {
    return this.http.get<any>(this.apiUrl + 'api/v1/shipmentFromNDC/getShipmentFromNDC/' + shipmentID + '/' + transactionDate + '/' + status + '/' + poNumber );
  }

  getInvoice(invoiceNo: string, invoiceDate: string, status: string, poNumber: string) {
    return this.http.get<any>(this.apiUrl + 'api/v1/SalesInvoiceNDC/getSalesInvoiceNDC/' + invoiceNo + '/' + invoiceDate + '/' + status + '/' + poNumber );
  }

  getSalesInvoiceNDCDetail(InvoiceNo: any): Observable<any> {
    return this.http.get(`${this.apiUrl}api/v1/SalesInvoiceNDC/getSalesInvoiceNDCDetail?InvoiceNo=${InvoiceNo}`);
  }

  getInventoryFiles(customerId: string, itemId: string, batchId: string = '') {
    return this.http.get<Inventory[]>(this.apiUrl + 'api/v1/inventory/getInventoryFiles/' + customerId + '/' + itemId + '/' + batchId);
  }

  getbatchWise(batchID: string) {
    return this.http.get<any[]>(this.apiUrl + 'api/v1/shipmentFromNDC/getBatchData/' + batchID );
  }

  getBatchWiseItemID(itemID: string, batchID:string): Observable<any> {
    const url = `${this.apiUrl}api/v1/inventory/getBatchWiseItemID?ItemID=${itemID}&BatchID=${batchID}`;
    return this.http.get<any>(url);

  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getERPCustomers`);
  }

  getRouteTypes(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/v1/inventory/getRouteTypes`);
  }
}
