import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ProductUploadPrices } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class ProductUploadPricesService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  //updateProductUploadPrices(connectorModel: any): Observable<any> {
  //  return this.http.post<any>(this.apiUrl + 'api/ProductUploadPrices/updateProductUploadPrices', connectorModel);
  //}

  getProductUploadPrices(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/ProductUploadPrices/getProductUploadPrices`, { params });
  }

  uploadProductUploadPricesFile(file: File, ERPCustomerID: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    const url = `${this.apiUrl}api/ProductUploadPrices/processProductUploadPricesFile?ERPCustomerID=${ERPCustomerID}`;
    return this.http.post(url, formData);
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/ProductUploadPrices/getERPCustomers`);
  }

  downloadSampleFile(): Observable<any> {
    const url = `${this.apiUrl}api/ProductUploadPrices/downloadSampleFile`;

    return this.http.get(url, {
      responseType: 'arraybuffer',
      observe: 'response'
    });
  }

  priceDescripencies(CustomerID: string): Observable<any> {
    const url = `${this.apiUrl}api/ProductUploadPrices/PriceDescripencies`;
    const params = { CustomerID: CustomerID };

    return this.http.get(url, {
      params: params,
      responseType: 'arraybuffer',
      observe: 'response'
    });

  }
}
