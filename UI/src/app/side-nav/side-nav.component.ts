import { Component } from '@angular/core';
import { SideNavItem } from '../models/models';
import { RouterLinkActive, RouterLink } from '@angular/router';
import { NgFor, TitleCasePipe, CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { ApiService } from '../services/api.service';
import { TranslateModule } from '@ngx-translate/core';
import { MatExpansionModule } from '@angular/material/expansion'; // Import MatExpansionModule

@Component({
  selector: 'side-nav',
  templateUrl: './side-nav.component.html',
  styleUrls: ['./side-nav.component.scss'],
  standalone: true,
  imports: [
    MatListModule,
    NgFor,
    RouterLinkActive,
    RouterLink,
    TitleCasePipe,
    CommonModule,
    TranslateModule,
    MatExpansionModule // Add MatExpansionModule to imports
  ],
})
export class SideNavComponent {
  constructor(public api: ApiService, private router: Router) {}
  apiUrl = environment.apiUrl;
  company = this.api.getTokenUserInfo()?.company;
  isSetupMenu = this.api.getTokenUserInfo()?.isSetupMenu.toLocaleUpperCase() === 'TRUE' || this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN';

  sideNavContent: SideNavItem[] = [
    {
      title: 'nav.carrierLoadTender',
      link: 'edi/carrier',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? true : false
    },
    {
      title: 'nav.orders',
      link: 'edi/all-orders',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' || this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'REPAINTSTUDIOS'  ? true : false
    },
    {
      title: 'nav.customers',
      link: 'edi/customers',
      visible: true
    },
    {
      title: 'nav.connectors',
      link: 'edi/connectors',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },
    {
      title: 'nav.maps',
      link: 'edi/maps',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },

    {
      title: 'nav.partnerGroups',
      link: 'edi/partnergroups',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },
    {
      title: 'nav.routes',
      link: 'edi/routes',
      visible: true
    },
    {
      title: 'nav.customerProductCatalog',
      link: 'edi/customerProductCatalog',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ? true : false
    },
    {
      title: 'nav.productUploadPrices',
      link: 'edi/productuploadprices',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ? true : false
    },
    {
      title: 'nav.productPrices',
      link: 'edi/productPrices',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ? true : false
    },
    {
      title: 'nav.routeTypes',
      link: 'edi/routeTypes',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },
    {
      title: 'nav.hangfireDashboard',
      link: 'hangfire/dashboard',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },

    {
      title: 'nav.routeExceptions',
      link: 'edi/routeExceptions',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH' ? this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false : true,
    },
    {
      title: 'nav.userManagement',
      link: 'edi/users',
      visible: this.api.getTokenUserInfo()?.userType.toLocaleUpperCase() === 'ADMIN' ? true : false
    },
    {
      title: 'nav.ediFileCounter',
      link: 'edi/ediFileCounter',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH'
    },
    {
      title: 'nav.inventory',
      link: 'edi/inventory',
      visible: this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ? true : false
    },
    {
      title: 'nav.invFeedFromNDC',
      link: 'edi/invFeedFromNDC',
      visible: this.api.getTokenUserInfo()?.company.toUpperCase() === 'SURGIMAC' ? true : false
    },
    {
      title: 'nav.purchaseOrder',
      link: 'edi/purchaseOrder',
      visible: this.api.getTokenUserInfo()?.company.toUpperCase() === 'SURGIMAC' ? true : false
    },
    {
      title: 'nav.sipmentFromNdc',
      link: 'edi/sipmentFromNdc',
      visible: this.api.getTokenUserInfo()?.company.toUpperCase() === 'SURGIMAC' ? true : false
    },
    {
      title: 'nav.salesInvoiceNdc',
      link: 'edi/salesInvoiceNdc',
      visible: this.api.getTokenUserInfo()?.company.toUpperCase() === 'SURGIMAC' ? true : false
    },
    {
      title: 'nav.purchaseOrdersTracking',
      link: 'edi/purchaseOrdersTracking',
      visible: this.api.getTokenUserInfo()?.company.toUpperCase() === 'SURGIMAC' ? true : false
    },
  ];

  goToLink(option: SideNavItem) {
    if (option.title.toLowerCase() === 'nav.hangfiredashboard') {
      window.open(this.apiUrl + 'dashboard', '_blank');
    } else {
      this.router.navigate([option.link]);
    }
  }

  isSectionVisible(links: string[]): boolean {

    return this.sideNavContent.some(option => option.link && option.visible);
  }

}
