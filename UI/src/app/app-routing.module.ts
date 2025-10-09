import { ChangePasswordComponent } from './change-password/change-password.component';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { combineLatest } from 'rxjs';
import { AuthorizationGuard } from './authorization.guard';
import { AuthenticationGuard } from './guards/authentication.guard';
import { LoginComponent } from './login/login.component';
import { OrdersComponent } from './orders/orders.component';
import { ProfileComponent } from './profile/profile.component';
import { RegisterComponent } from './register/register.component';
import { UsersListComponent } from './users-list/users-list.component';
import { Process850Component } from './process850/process850.component';
import { CustomersComponent } from './customers/customers.component';
import { MapsComponent } from './maps/maps.component';
import { ConnectorsComponent } from './connectors/connectors.component';
import { PartnerGroupsComponent } from './partnergroups/partnergroups.component';
import { RoutesComponent } from './routes/routes.component';
import { CustomerProductCatalogComponent } from './customer-product-catalog/customer-product-catalog.component';
import { RouteTypesComponent } from './route-types/route-types.component';
import { RouteExceptionComponent } from './route-exception/route-exception.component';
import { CarrierLoadTenderComponent } from './carrier-load-tender/carrier-load-tender.component';
import { ProductUploadPricesComponent } from './product-upload-prices/product-upload-prices.component';
import { UsersComponent } from './users/users.component';
import { EdiFileCounterComponent } from './edi-file-counter/edi-file-counter.component';
import { InventoryComponent } from './inventory/inventory.component';
import { ProductPricesComponent } from './product-prices/product-prices.component';
import { InvFeedFromNDCComponent } from './inv-feed-from-ndc/inv-feed-from-ndc.component';
import { PurchaseOrderComponent } from './purchase-order/purchase-order.component';
import { SipmentFromNdcComponent } from './sipment-from-ndc/sipment-from-ndc.component';
import { SalesInvoiceNdcComponent } from './sales-invoice-ndc/sales-invoice-ndc.component';


const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: 'register',
    component: RegisterComponent,
  },
  {
    path: 'change-password',
    component: ChangePasswordComponent,
    // canActivate: [AuthenticationGuard],
  },
  {
    path: 'edi/process850',
    component: Process850Component,
    canActivate: [AuthenticationGuard],
  },
  //{
  //  path: 'edi/all-orders',
  //  component: OrdersComponent,
  //  canActivate: [AuthorizationGuard],
  //},
  {
    path: 'edi/carrier',
    component: CarrierLoadTenderComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/all-orders',
    component: OrdersComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/customers',
    component: CustomersComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/maps',
    component: MapsComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/connectors',
    component: ConnectorsComponent,
    canActivate: [AuthorizationGuard],
  },
  {
     path: 'users/list',
     component: UsersListComponent,
     canActivate: [AuthorizationGuard],
  },
  {
     path: 'users/profile',
     component: ProfileComponent,
     canActivate: [AuthenticationGuard],
  },
  {
    path: 'edi/partnergroups',
    component: PartnerGroupsComponent,
    canActivate: [AuthenticationGuard],
  },
  {
    path: 'edi/routes',
    component: RoutesComponent,
    canActivate: [AuthenticationGuard],
  },
  {
    path: 'edi/customerProductCatalog',
    component: CustomerProductCatalogComponent,
    canActivate: [AuthenticationGuard],
  },
  {
    path: 'edi/routeTypes',
    component: RouteTypesComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/routeExceptions',
    component: RouteExceptionComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/productuploadprices',
    component: ProductUploadPricesComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/users',
    component: UsersComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/ediFileCounter',
    component: EdiFileCounterComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/inventory',
    component: InventoryComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/productPrices',
    component: ProductPricesComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/invFeedFromNDC',
    component: InvFeedFromNDCComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/purchaseOrder',
    component: PurchaseOrderComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/sipmentFromNdc',
    component: SipmentFromNdcComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: 'edi/salesInvoiceNdc',
    component: SalesInvoiceNdcComponent,
    canActivate: [AuthorizationGuard],
  },
  {
    path: "**",
    component: LoginComponent,
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
