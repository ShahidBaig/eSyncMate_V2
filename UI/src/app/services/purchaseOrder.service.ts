import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PurchaseOrder } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class PurchaseOrderService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  //saveMap(connectorModel: any): Observable<any> {
  //  return this.http.post<any>(this.apiUrl + 'api/Maps/createMap', connectorModel);
  //}

  //updateMap(connectorModel: any): Observable<any> {
  //  return this.http.post<any>(this.apiUrl + 'api/Maps/updateMap', connectorModel);
  //}

  getPurchaseOrder(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getPurchaseorders`, { params });
  }


  savePurchaseOrder(orderModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/PurchaseOrder/createPurchaseorders', orderModel);
  }

  updatePurchaseOrder(orderModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/PurchaseOrder/updatePurchaseorders', orderModel);
  }

  markForRelease(orderId: number): Observable<any>  {
    return this.http.post<any>(`${this.apiUrl}api/PurchaseOrder/markForRelease?orderId=${orderId}`,
      {}
    );
  }

  updateQty(payload: any): Observable<any> {
    const url = `${this.apiUrl}api/PurchaseOrder/updateqty`;
    return this.http.post<any>(url, payload);
  }

  getItemsData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getItems`);
  }

  getSupplierData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getSuppliers`);
  }

  getPurchaseOrderDetail(Id:any): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getPurchaseOrderDetail?OrderID=${Id}`);
  }

  getItemSelected(ItemID: any, ndcItemID: any): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getItemSelected?ItemID=${ItemID}&NDCItemID=${ndcItemID}`);
  }

  getSuppliersItemsData(SupplierName: any): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PurchaseOrder/getSuppliersItems?SupplierName=${SupplierName}`);
  }
}
