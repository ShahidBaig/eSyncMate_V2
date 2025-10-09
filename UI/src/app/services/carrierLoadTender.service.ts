import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { map } from 'rxjs/operators';
import { Order, StatesModel, User, UserType } from '../models/models';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class CarrierLoadTenderService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient, private jwt: JwtHelperService) { }

  getCarrierLoadTender(carrierLoadTenderId: number, fromDate: string, toDate: string, shipmentId: string, shipmentShipperNo: string, status: string, customerName: string) {
    return this.http.get<Order[]>(this.apiUrl + 'EDIProcessor/api/v1/CarrierLoadTender/getCarrierLoadTender/' + carrierLoadTenderId + '/' + fromDate + '/' + toDate + '/' + shipmentId + '/' + shipmentShipperNo + '/' + status + '/' + customerName);
  }

  getCarrierLoadTenderFiles(carrierLoadTenderId: Number) {
    return this.http.get<Order[]>(this.apiUrl + 'EDIProcessor/api/v1/CarrierLoadTender/getCarrierLoadTenderFiles/' + carrierLoadTenderId);
  }

  updateAckStatus(carrierTenderModel: any) {
    return this.http.post<any>(this.apiUrl + 'EDIProcessor/api/v1/CarrierLoadTender/updateAckStatus', carrierTenderModel);
  }

   getAckData(): Observable<any> {
    return this.http.get(this.apiUrl + 'EDIProcessor/api/v1/CarrierLoadTender/getCarrierLoadTenderAckData');
  }

  updateTrackStatus(id: number, trackStatus: string, consigneeAddress: string, consigneeCity: string, consigneeState: string, consigneeZip: string, consigneeCountry: string, equipmentNo: string, manualequipmentNo: string) {
    let params = new HttpParams()
    .append('tenderID', id)
    .append('trackStatus', trackStatus)
    .append('consigneeAddress', consigneeAddress)
    .append('consigneeCity', consigneeCity)
    .append('consigneeState', consigneeState)
    .append('consigneeZip', consigneeZip)
    .append('consigneeCountry', consigneeCountry)
    .append('equipmentNo', equipmentNo)
    .append('manualequipmentNo', manualequipmentNo)
    
    let queryString = params.toString();

  return this.http.post(this.apiUrl + 'EDIProcessor/api/v1/carrierLoadTender/updateTrackStatus?' + queryString, {});
  }

  getEdiFilesCounter(fromDate: string = '', toDate: string = '', customer: number = 0, shipmentID: string = '', shipperNo: string = ''): Observable<any[]> {
    let params = new HttpParams();
    params = params.set('FromDate', fromDate);
    params = params.set('ToDate', toDate);
    params = params.set('Customer', customer.toString());
    params = params.set('ShipmentID', shipmentID);
    params = params.set('ShipperNo', shipperNo);

    return this.http.get<any[]>(`${this.apiUrl}EDIProcessor/api/v1/CarrierLoadTender/getEdiFilesCounter`, { params });
  }

  getStatesData(): Observable<any> {
    return this.http.get<StatesModel[]>(this.apiUrl + 'EDIProcessor/api/v1/CarrierLoadTender/getStatesData');
  }
}
