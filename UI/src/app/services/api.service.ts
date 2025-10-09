import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { map } from 'rxjs/operators';
import { Order, RouteLog, User, UserType } from '../models/models';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient, private jwt: JwtHelperService) { }

  createAccount(user: any) {
    return this.http.post(this.apiUrl + 'RegisterUser', user)
  }

  login(loginInfo: any) {
    let params = new HttpParams()
      .append('userID', loginInfo.userID)
      .append('password', loginInfo.password);
    return this.http.get(this.apiUrl + 'Login', {
      params: params,
    });
  }

  updatePassword(userID: any, oldPassword: string, newPassword: string) {
    let params = new HttpParams()
      .append('UserID', userID)
      .append('oldPassword', oldPassword)
      .append('Password', newPassword);
    return this.http.get(this.apiUrl + 'UpdatePassword', {
      params: params,
    });
  }

  saveToken(token: string) {
    localStorage.setItem('access_token', token);

    let tokenData = this.jwt.decodeToken();
    if (tokenData.exp) {
      const expiryDate = new Date(tokenData.exp * 1000);
      localStorage.setItem('tokenExpiry', expiryDate.toISOString());
    }
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('access_token');
  }

  deleteToken() {
    localStorage.removeItem('access_token');
  }

  getTokenUserInfo(): User | null {
    if (!this.isLoggedIn()) return null;
    let token = this.jwt.decodeToken();

    let user: User = {
      id: token.id,
      firstName: token.firstName,
      lastName: token.lastName,
      email: token.email,
      mobile: token.mobile,
      password: '',
      status: token.status,
      customerName: token.customerName,
      createdDate: token.createdDate,
      userType: token.userType,
      company: token.company,
      isSetupMenu: token.isSetupAllowed,
      userID: token.userID,
    };
    return user;
  }

  getAllUsers() {
    return this.http.get<User[]>(this.apiUrl + 'GetAllUsers').pipe(
      map((users) =>
        users.map((user) => {
          let temp: User = user;
          temp.userType = user.userType;
          return temp;
        })
      )
    );
  }

  blockUser(id: number) {
    return this.http.get(this.apiUrl + 'ChangeBlockStatus/1/' + id, {
      responseType: 'text',
    });
  }

  unblockUser(id: number) {
    return this.http.get(this.apiUrl + 'ChangeBlockStatus/0/' + id, {
      responseType: 'text',
    });
  }

  enableUser(id: number) {
    return this.http.get(this.apiUrl + 'ChangeEnableStatus/1/' + id, {
      responseType: 'text',
    });
  }

  disableUser(id: number) {
    return this.http.get(this.apiUrl + 'ChangeEnableStatus/0/' + id, {
      responseType: 'text',
    });
  }

  getOrders(orderId: number, fromDate: string, toDate: string, orderNumber: string, status: string, ExternalId: string, CustomerId: string) {
    return this.http.get<Order[]>(this.apiUrl + 'EDIProcessor/api/v1/orders/getOrders/' + orderId + '/' + fromDate + '/' + toDate + '/' + orderNumber + '/' + status + '/' + ExternalId + '/' + CustomerId);
  }


  getRouteExceptions(name: string, message: string, fromDate: string, toDate: string, status: string) {
    return this.http.get<RouteLog[]>(this.apiUrl + 'api/v1/routeExceptions/getRouteExceptions/' + name + '/' + message + '/' + fromDate + '/' + toDate + '/' + status);
  }

  uploadFile(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post(this.apiUrl + 'EDIProcessor/api/v1/orders/process850', formData);
  }

  getOrderFiles(orderId: Number) {
    return this.http.get<Order[]>(this.apiUrl + 'EDIProcessor/api/v1/orders/getOrderFiles/' + orderId);
  }

  getStoresOrder(orderId: number) {
    return this.http.get<Order[]>(
      `${this.apiUrl}EDIProcessor/api/v1/orders/getOrderStores?orderId=${orderId}`,
      //`${this.apiurl}api/Users/getTopUsers?userNo=${userNo}`,
      {}
    );
  }

  async syncOrderStore(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/syncOrderStore?orderStoreId=${orderId}`,
      {}
    );
  }

  generateASN(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/process856?orderId=${orderId}`,
      {}
    );
  }

  markForASN(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/markFor856?orderId=${orderId}`,
      {}
    );
  }

  createInvoice(orderId: number) {
    return this.http.post(
      `${this.apiUrl}createInvoice?orderId=${orderId}`,
      {}
    );
  }

  process810(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/process810?orderId=${orderId}`,
      {}
    );
  }

  generate855EDI(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/process855?orderId=${orderId}`,
      {}
    );
  }

  syncOrder(orderId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/syncOrder?orderId=${orderId}`,
      {}
    );
  }

  generateASNForStoreOrders(file: File, orderId: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    const url = `${this.apiUrl}EDIProcessor/api/v1/orders/process856Store?orderId=${orderId}`;
    return this.http.post(url, formData);
  }

  generate810ForStoreOrders(file: File, orderId: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    const url = `${this.apiUrl}EDIProcessor/api/v1/orders/process810Store?orderId=${orderId}`;
    return this.http.post(url, formData);
  }

  markForASNForStoreOrders(orderStoreId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/markSendOrderStore?orderStoreId=${orderStoreId}`,
      {}
    );
  }

  createInvoiceForStoreOrders(orderStoreId: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/createInvoiceOrderStore?orderStoreId=${orderStoreId}`,
      {}
    );
  }

  getMaps(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/Maps/getMaps`, { params });
  }

  getPartnerGroups(searchOption: string, searchValue: string): Observable<any> {
    const params = new HttpParams()
      .set('searchOption', searchOption)
      .set('searchValue', searchValue);

    return this.http.get(`${this.apiUrl}api/PartnerGroups/getPartnerGroups`, { params });
  }

    updateStatus(status:any,orderId: any) {
      const url = `${this.apiUrl}EDIProcessor/api/v1/orders/setOrderStatus?OrderId=${orderId}&Status=${status}`;
      return this.http.post(url,"");
  }

  processForShipment(orderNumber: string) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/processOrderForShipment?OrderNumber=${orderNumber}`,
      {}
    );
  }

  ReProccess(orderNumber: string, customerName: string, status: any, customerOrderNumber: string, isASNError: number) {
    return this.http.post(
      `${this.apiUrl}EDIProcessor/api/v1/orders/reprocessOrder?OrderId=${orderNumber}&CustomerName=${customerName}&Status=${status}&OrderNumber=${customerOrderNumber}&isASNError=${isASNError}`,
      {}
    );
  }

  updateCLTStatus(status: any, cltId: any, completionDate:any) {
    const url = `${this.apiUrl}EDIProcessor/api/v1/CarrierLoadTender/setCLTStatus?CLTId=${cltId}&Status=${status}&CompletionDate=${completionDate}`;
    return this.http.post(url, "");
  }

  getSalesOrderDetail(Id:any): Observable<any> {
    return this.http.get(`${this.apiUrl}EDIProcessor/api/v1/orders/getOrderDetail?OrderID=${Id}`);
  }

  updateSalesOrder(orderModel: any): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'EDIProcessor/api/v1/orders/updateSalesOrder', orderModel);
  }
}
