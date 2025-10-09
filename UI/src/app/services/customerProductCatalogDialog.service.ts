import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CustomerProductCatalog } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class CustomerProductCatalogService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  updateCustomerProductCatalog(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/CustomerProductCatalog/updateCustomerProductCatalog', connectorModel);
  }

  getCustomerProductCatalog(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getCustomerProductCatalog`, { params });
  }

  uploadCustomerProductCatalogFile(file: File, ERPCustomerID: string, ItemType: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    const url = `${this.apiUrl}api/CustomerProductCatalog/processCustomerProductCatalogFile?ERPCustomerID=${ERPCustomerID}&ItemType=${ItemType}`;
    return this.http.post(url, formData);
  }

  getHistoryCustomerProductCatalog(ERPCustomerID: string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/getHistoryCustomerProductCatalog?ERPCustomerID=${ERPCustomerID}`;
    return this.http.get(url);
  }

  getItemTypes(ERPCustomerID: string): Observable<any> {
    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getItemTypes?ERPCustomerID=${ERPCustomerID}`);
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getERPCustomers`);
  }

  getProductsData(ID: number): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/getProductsData?ID=${ID}`;
    return this.http.get(url);
  }

  downloadSampleFile(CustomerID: string, ItemType: string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/downloadSampleFile`;
    const params = { CustomerID: CustomerID, ItemTypeID: ItemType };

    return this.http.get(url, {
      params: params,
      responseType: 'arraybuffer',
      observe: 'response'
    });

  }

  downloadProductPricesSampleFile(CustomerID: string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/downloadProductPricesSampleFile`;
    const params = { CustomerID: CustomerID };

    return this.http.get(url, {
      params: params,
      responseType: 'arraybuffer',
      observe: 'response'
    });

  }


  uploadProductPricesFile(file: File, ERPCustomerID: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    const url = `${this.apiUrl}api/CustomerProductCatalog/processProductPricesFile?ERPCustomerID=${ERPCustomerID}`;
    return this.http.post(url, formData);
  }


  downloadRejectedCSV(CustomerID: string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/downloadRejectedCSV`;
    const params = { CustomerID: CustomerID };

    return this.http.get(url, {
      params: params,
      responseType: 'arraybuffer',
      observe: 'response'
    });

  }

  getDataProduct(ID: number): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/getProductsData?ID=${ID}`;
    return this.http.get(url);
  }


  getPrepareItemData(ID: number,CustomerID : string,ItemType : string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/getPrepareItemData?UserID=${ID}&CustomerID=${CustomerID}&ItemType=${ItemType}`;
    return this.http.get(url);
  }


  insertPrepareItemData(ID: number, CustomerID: string, ItemType: string): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/insertPrepareItemData?UserID=${ID}&CustomerID=${CustomerID}&ItemType=${ItemType}`;
    return this.http.get(url);
  }


  downloadItemsDataCSV(CustomerID: string, ItemType: string,UserID : number): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/downloadItemsDataCSV`;
    const params = { CustomerID: CustomerID, ItemType: ItemType, UserID: UserID };

    //return this.http.get(url);
    return this.http.get(url, {
      params: params
      //responseType: 'arraybuffer',
      //observe: 'response'
    });

  }

  deleteItemData(CustomerID: string, ItemType: string, UserID: number): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/deleteItemsData?UserID=${UserID}&CustomerID=${CustomerID}&ItemType=${ItemType}`;
    return this.http.get(url);
  }


  getSCSBulkUploadPrice(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getSCSBulkUploadPrice`, { params });
  }

  processCustomerProductPrices(UserID: number, CustomerID: string,): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/processCustomerProductPricesData?UserID=${UserID}&customerID=${CustomerID}`;
    return this.http.get(url);
  }

  processResolveError(UserID: number): Observable<any> {
    const url = `${this.apiUrl}api/CustomerProductCatalog/processResolveError?UserID=${UserID}`;
    return this.http.get(url);
  }

}
