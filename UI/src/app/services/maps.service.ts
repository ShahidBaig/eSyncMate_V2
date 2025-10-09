import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Map } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class MapsService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  saveMap(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Maps/createMap', connectorModel);
  }

  updateMap(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Maps/updateMap', connectorModel);
  }

  getMaps(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/Maps/getMaps`, { params });
  }

  getMapTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Maps/getMapTypes`);
  }
}
