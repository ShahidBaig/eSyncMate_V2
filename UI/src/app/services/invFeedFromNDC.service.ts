import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { InvFeedFromNDC } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class InvFeedFromNDCService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getInvFeedFromNDC(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/InvFeedFromNDC/getInvFeedFromNDC`, { params });
  }

  getInvFeed(sku: string): Observable<any> {
    const url = `${this.apiUrl}api/Routes/GetinvFeed?RouteID=`+sku ;
    return this.http.get(url);
  }

  uploadFile(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post(this.apiUrl + 'api/InvFeedFromNDC/processInvFeedExcelFile', formData);
  }
}
