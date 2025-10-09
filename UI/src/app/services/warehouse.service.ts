import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class WareHouseService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  getWareHouses(): Observable<any> {
    return this.http.get<any>(this.apiUrl + 'api/Warehouses/getWarehouses');
  }
}
