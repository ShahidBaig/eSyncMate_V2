import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { PageHeaderComponent } from './page-header/page-header.component';
import { PageFooterComponent } from './page-footer/page-footer.component';
import { MaterialModule } from './material/material.module';
import { SideNavComponent } from './side-nav/side-nav.component';
import { RegisterComponent } from './register/register.component';
import { LoginComponent } from './login/login.component';
import { ReactiveFormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule, HttpClient } from '@angular/common/http';
import { JwtModule } from '@auth0/angular-jwt';
import { OrdersComponent } from './orders/orders.component';
import { UsersListComponent } from './users-list/users-list.component';
import { ProfileComponent } from './profile/profile.component';
import { Process850Component } from './process850/process850.component';
import { NgToastModule } from 'ng-angular-popup';
import { PopupComponent } from './popup/popup.component';
import { StoresOrderComponent } from './stores-order/stores-order.component';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { CustomersComponent } from './customers/customers.component';
import { MapsComponent } from './maps/maps.component';
import { ConnectorsComponent } from './connectors/connectors.component';
import { FileContentViewerDialogComponent } from './file-content-viewer-dialog/file-content-viewer-dialog.component';
import { AddCustomerDialogComponent } from './customers/add-customer-dialog/add-customer-dialog.component';
import { EditCustomerPopupComponent } from './customers/edit-customer-popup/edit-customer-popup.component';
import { AddConnectorDialogComponent } from './connectors/add-connector-dialog/add-connector-dialog.component';
import { EditConnectorDialogComponent } from './connectors/edit-connector-dialog/edit-connector-dialog.component';
import { AddMapDialogComponent } from './maps/add-map-dialog/add-map-dialog.component';
import { EditMapDialogComponent } from './maps/edit-map-dialog/edit-map-dialog.component';
import { PartnerGroupsComponent } from './partnergroups/partnergroups.component';
import { AddPartnerGroupDialogComponent } from './partnergroups/add-partnergroup-dialog/add-partnergroup-dialog.component';
import { EditPartnerGroupDialogComponent } from './partnergroups/edit-partnergroup-dialog/edit-partnergroup-dialog.component';
import { RoutesComponent } from './routes/routes.component';
import { AddRoutesDialogComponent } from './routes/add-routes-dialog/add-routes-dialog.component';
import { EditRoutesDialogComponent } from './routes/edit-routes-dialog/edit-routes-dialog.component';
import { CustomerProductCatalogComponent } from './customer-product-catalog/customer-product-catalog.component';
import { EditCustomerProductCatalogDialogComponent } from './customer-product-catalog/edit-customer-product-catalog-dialog/edit-customer-product-catalog-dialog.component';
import { AuthInterceptor } from './services/AuthInterceptor';
import { HistoryCustomerProductCatalogDialogComponent } from './customer-product-catalog/history-customer-product-catalog-dialog/history-customer-product-catalog-dialog.component';
import { RouteLogDialogComponent } from './routes/route-log-dialog/route-log-dialog.component';
import { RouteTypesComponent } from './route-types/route-types.component';
import { AddRouteTypesDialogComponent } from './route-types/add-route-types-dialog/add-route-types-dialog.component';
import { EditRouteTypesDialogComponent } from './route-types/edit-route-types-dialog/edit-route-types-dialog.component';
import { RouteDataDialogComponent } from './routes/route-data-dialog/route-data-dialog.component';
import { RouteExceptionComponent } from './route-exception/route-exception.component';
import { CarrierLoadTenderComponent } from './carrier-load-tender/carrier-load-tender.component';
import { ProductUploadPricesComponent } from './product-upload-prices/product-upload-prices.component';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/translations/', '.json');
}
import { ProductDataComponent } from './customer-product-catalog/product-data/product-data.component';
import { UsersComponent } from './users/users.component';
import { EditUsersDialogComponent } from './users/edit-users-dialog/edit-users-dialog.component';
import { EdiFileCounterComponent } from './edi-file-counter/edi-file-counter.component';
import { InventoryComponent } from './inventory/inventory.component';
import { InventorypopupComponent } from './inventory/inventory-popup/inventory-popup.component';
import { ChangePasswordComponent } from './change-password/change-password.component';
import { MatIconModule } from '@angular/material/icon';
import { UpdateStatusModalComponent } from './update-status-modal/update-status-modal.component';
import { InventoryBatchwiseComponent } from './inventory/inventory-batchwise/inventory-batchwise.component';
import { ProductPricesComponent } from './product-prices/product-prices.component';
import { InvFeedFromNDCComponent } from './inv-feed-from-ndc/inv-feed-from-ndc.component';
import { PurchaseOrderComponent } from './purchase-order/purchase-order.component';
import { AddPurchaseOrderComponent } from './purchase-order/add-purchase-order/add-purchase-order.component';
import { MatTableModule } from '@angular/material/table';
import { EditPurchaseOrderComponent } from './purchase-order/edit-purchase-order/edit-purchase-order.component';
import { ViewPurchaseOrderComponent } from './purchase-order/view-purchase-order/view-purchase-order.component';
import { SipmentFromNdcComponent } from './sipment-from-ndc/sipment-from-ndc.component';
import { DetailShipmentFromNdcComponent } from './sipment-from-ndc/detail-shipment-from-ndc/detail-shipment-from-ndc.component';
import { OrderDetailComponent } from './orders/order-detail/order-detail.component';

@NgModule({
  declarations: [AppComponent  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    MaterialModule,
    ReactiveFormsModule,
    HttpClientModule,
    MatIconModule,
    MatTableModule,
    JwtModule.forRoot({
      config: {
        tokenGetter: () => {
          return localStorage.getItem('access_token');
        },
        allowedDomains: ['165.227.248.24:8080'],
      },
    }),
    PageHeaderComponent,
    PageFooterComponent,
    SideNavComponent,
    RegisterComponent,
    LoginComponent,
    OrdersComponent,
    UsersListComponent,
    ProfileComponent,
    Process850Component,
    NgToastModule,
    PopupComponent,
    StoresOrderComponent,
    CommonModule,
    MatSelectModule,
    CustomersComponent,
    MapsComponent,
    ConnectorsComponent,
    FileContentViewerDialogComponent,
    AddCustomerDialogComponent,
    EditCustomerPopupComponent,
    AddConnectorDialogComponent,
    EditConnectorDialogComponent,
    AddMapDialogComponent,
    EditMapDialogComponent,
    PartnerGroupsComponent,
    AddPartnerGroupDialogComponent,
    EditPartnerGroupDialogComponent,
    RoutesComponent,
    AddRoutesDialogComponent,
    EditRoutesDialogComponent,
    CustomerProductCatalogComponent,
    EditCustomerProductCatalogDialogComponent,
    HistoryCustomerProductCatalogDialogComponent,
    RouteLogDialogComponent,
    RouteTypesComponent,
    AddRouteTypesDialogComponent,
    EditRouteTypesDialogComponent,
    RouteDataDialogComponent,
    RouteExceptionComponent,
    CarrierLoadTenderComponent,
    ProductUploadPricesComponent,
    UsersComponent,
    EditUsersDialogComponent,
    EdiFileCounterComponent,
    InventoryComponent,
    InventorypopupComponent,
    ChangePasswordComponent,
    UpdateStatusModalComponent,
    InventoryBatchwiseComponent,
    ProductPricesComponent,
    InvFeedFromNDCComponent,
    PurchaseOrderComponent,
    AddPurchaseOrderComponent,
    EditPurchaseOrderComponent,
    ViewPurchaseOrderComponent,
    SipmentFromNdcComponent,
    DetailShipmentFromNdcComponent,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
    ProductDataComponent
  ],
  providers: [{ provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },],
  bootstrap: [AppComponent]
})
export class AppModule { }
