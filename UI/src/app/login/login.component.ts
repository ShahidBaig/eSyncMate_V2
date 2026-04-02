import { Component } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NgIf } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
    selector: 'login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    standalone: true,
    imports: [
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        NgIf,
        MatButtonModule,
        MatIconModule,
        MatSelectModule,
        RouterLink,
        CommonModule,
        TranslateModule,
        MatProgressBarModule
    ],
})
export class LoginComponent {

  hide = true;
  isLoading = false;
  loginForm: FormGroup;
  responseMsg: string = '';
  message: string | null = '';
  redTextStyle = {
    color: 'red'
  }

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private router: Router,
    public languageService: LanguageService,
    private toast: NgToastService
  ) {
       this.message = localStorage.getItem('sessionExpiryMessage');

       if (this.message && !sessionStorage.getItem('hasReloaded')) {
        sessionStorage.setItem('hasReloaded', 'true');
        location.reload();
      } else
        sessionStorage.removeItem('hasReloaded');

      window.addEventListener('load', () => {
        if (this.message) {
          this.toast.info({ detail: "INFO", summary: this.message, duration: 5000, position: 'topRight' });
          this.message = '';
          localStorage.setItem('sessionExpiryMessage', '');
        }
    });

    this.loginForm = fb.group({
      userID: fb.control(''),
      password: fb.control('', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(15),
      ]),
    });

    // Redirect to first menu from stored menus, fallback to company-based
    const userMenus = this.api.getUserMenus();
    if (userMenus && userMenus.modules && userMenus.modules.length > 0) {
      for (const mod of userMenus.modules) {
        if (mod.menuItems && mod.menuItems.length > 0) {
          const firstMenu = mod.menuItems.find((m: any) => m.canView);
          if (firstMenu && firstMenu.route) {
            this.router.navigateByUrl('/' + firstMenu.route);
            return;
          }
        }
      }
    }

    if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH') {
      this.router.navigateByUrl('/edi/carrier');
    }
    if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' || this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'REPAINTSTUDIOS') {
      this.router.navigateByUrl('/edi/all-orders');
    }
    if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'SURGIMAC') {
      this.router.navigateByUrl('/edi/purchaseOrder');
    }
  }

  login() {
    let loginInfo = {
      userID: this.loginForm.get('userID')?.value,
      password: this.loginForm.get('password')?.value,
    };

    localStorage.setItem("userID", loginInfo.userID);
    this.isLoading = true;

    this.api.login(loginInfo).subscribe({
      next: (res: any) => {
        if (res.token === 'Invalid') {
          this.responseMsg = res.message;
          this.isLoading = false;
        } else {
          this.responseMsg = '';
          this.api.saveToken(res.token);

          // Save user menus from login response
          if (res.menus) {
            this.api.saveUserMenus(res.menus);
          }

          let isActive = this.api.getTokenUserInfo()?.status.toLocaleUpperCase() === 'ACTIVE' ? true : false;
          localStorage.setItem("email", this.api.getTokenUserInfo()?.email || "");
          if (isActive)
          {
            // Navigate to first available menu route from user's menus
            const userMenus = this.api.getUserMenus();
            let navigated = false;
            if (userMenus && userMenus.modules && userMenus.modules.length > 0) {
              for (const mod of userMenus.modules) {
                if (mod.menuItems && mod.menuItems.length > 0) {
                  const firstMenu = mod.menuItems.find((m: any) => m.canView);
                  if (firstMenu && firstMenu.route) {
                    this.router.navigateByUrl('/' + firstMenu.route);
                    navigated = true;
                    break;
                  }
                }
              }
            }
            if (!navigated) {
              this.navigateByCompany();
            }
          }

          else {
            this.responseMsg = this.languageService.getTranslation('activeMsg');
            this.api.deleteToken();
          }
        }
      },
      error: (err: any) => {
        console.log('Error: ');
        console.log(err);
        this.isLoading = false;
      },
    });
  }

  navigateByCompany() {
    if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH') {
      this.router.navigateByUrl('/edi/carrier');
    } else if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ||
      this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'REPAINTSTUDIOS') {
      this.router.navigateByUrl('/edi/all-orders');
    } else if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'SURGIMAC') {
      this.router.navigateByUrl('/edi/purchaseOrder');
    }
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && this.loginForm.valid) {
      this.login();
    }
  }

  getEmailErrors() {
    if (this.Email.hasError('required')) return this.languageService.getTranslation('emailError');
    return '';
  }

  getPasswordErrors() {
    if (this.Password.hasError('required')) return this.languageService.getTranslation('passwordRequire');
    if (this.Password.hasError('minlength'))
      return this.languageService.getTranslation('minPassword');
    if (this.Password.hasError('maxlength'))
      return this.languageService.getTranslation('maxPassword');
    return '';
  }

  get Email(): FormControl {
    return this.loginForm.get('email') as FormControl;
  }
  get Password(): FormControl {
    return this.loginForm.get('password') as FormControl;
  }

  cancel() {
    this.loginForm.reset();
  }
}
