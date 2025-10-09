import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Routes } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class InvFeedService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getSearchInvFeed(routeID: number,fromDate: string, toDate: string, wareHouse: string): Observable<any> {
    const url = `${this.apiUrl}api/Routes/getinvFeedFromNDClog?ItemID=${routeID}&FromDate=${fromDate}&ToDate=${toDate}&WareHouse=${wareHouse}`;
    return this.http.get(url);
  }
}
