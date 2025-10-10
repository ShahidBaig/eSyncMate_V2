import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PurchaseOrdersTrackingService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient, private jwt: JwtHelperService) { }

  getPurchaseOrdersTracking(purchaseOrderNo: number, orderDate: string, sku: string, poNumber: string) {
    return this.http.get<any>(this.apiUrl + 'api/v1/purchaseOrdersTracking/getPurchaseOrdersTracking/' + purchaseOrderNo + '/' + orderDate + '/' + sku + '/' + poNumber );
  }
}
