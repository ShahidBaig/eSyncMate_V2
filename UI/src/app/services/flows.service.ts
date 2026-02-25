import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root',
})
export class FlowsService {
    apiUrl = environment.apiUrl;
    constructor(private http: HttpClient) { }

    getFlows(searchOption: string, searchValue: string): Observable<any> {
        const params = new HttpParams()
            .set('SearchOption', searchOption || 'ALL')
            .set('SearchValue', searchValue || 'ALL');
        return this.http.get(`${this.apiUrl}api/Flows/getFlows`, { params });
    }

    createFlow(flowModel: any): Observable<any> {
        return this.http.post<any>(this.apiUrl + 'api/Flows/createFlow', flowModel);
    }

    updateFlow(flowModel: any): Observable<any> {
        return this.http.put<any>(this.apiUrl + 'api/Flows/updateFlow', flowModel);
    }
}
