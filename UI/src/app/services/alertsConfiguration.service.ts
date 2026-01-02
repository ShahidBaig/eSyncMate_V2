import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Connector } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class alertsConfigurationService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  saveConnector(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/AlertsConfiguration/createAlertsConfiguration', connectorModel);
  }

  updateConnector(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/AlertsConfiguration/updateAlertsConfiguration', connectorModel);
  }

  getAlertsConfiguration(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/AlertsConfiguration/getAlertsConfiguration`, { params });
  }

  getConnectorTypesData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/AlertsConfiguration/getConnectorTypes`);
  }

  getERPCustomers(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/CustomerProductCatalog/getERPCustomers`);
  }
}
