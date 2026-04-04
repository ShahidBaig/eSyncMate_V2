import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Connector } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class ConnectorsService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  saveConnector(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Connectors/createConnector', connectorModel);
  }

  updateConnector(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/Connectors/updateConnector', connectorModel);
  }

  getConnectors(searchOption: string, searchValue: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get(`${this.apiUrl}api/Connectors/getConnectors`, { params });
  }

  getConnectorTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/Connectors/getConnectorTypes`);
  }
}
