import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PartnerGroup } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class PartnerGroupsService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }

  savePartnerGroup(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/PartnerGroups/createPartnerGroup', connectorModel);
  }

  updatePartnerGroup(connectorModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'api/PartnerGroups/updatePartnerGroup', connectorModel);
  }

  getPartnerGroups(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/PartnerGroups/getPartnerGroups`, { params });
  }

  getCustomersData(): Observable<any> {
    return this.http.get(`${this.apiUrl}api/PartnerGroups/getCustomers`);
  }
}
