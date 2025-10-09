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
import { MatGridListModule } from '@angular/material/grid-list';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

@Component({
    selector: 'login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    standalone: true,
    imports: [
        MatGridListModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        NgIf,
        MatButtonModule,
        MatIconModule,
        MatSelectModule,
        RouterLink,
        CommonModule,
        TranslateModule
    ],
})
export class LoginComponent {

  hide = true;
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
          this.toast.info({ detail: "INFO", summary: this.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
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

    this.api.login(loginInfo).subscribe({
      next: (res: any) => {
        if (res.token === 'Invalid')
          this.responseMsg = res.message;
        else {
          this.responseMsg = '';
          this.api.saveToken(res.token);
          let isActive = this.api.getTokenUserInfo()?.status.toLocaleUpperCase() === 'ACTIVE' ? true : false;
          localStorage.setItem("email", this.api.getTokenUserInfo()?.email || "");
          if (isActive)
          {
            if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'GECKOTECH')
            {
              this.router.navigateByUrl('/edi/carrier');
            }
            if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'ESYNCMATE' ||
              this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'REPAINTSTUDIOS')
            {
              this.router.navigateByUrl('/edi/all-orders');
            }
            if (this.api.getTokenUserInfo()?.company.toLocaleUpperCase() === 'SURGIMAC') {
              this.router.navigateByUrl('/edi/purchaseOrder');
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
      },
    });
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && this.loginForm.valid) {
      this.login();
    }
  }

  getEmailErrors() {
    if (this.Email.hasError('required')) return this.languageService.getTranslation('emailError');
   /* if (this.Email.hasError('email')) return 'Email is invalid.';*/
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
