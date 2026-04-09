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
  otpForm: FormGroup;
  responseMsg: string = '';
  message: string | null = '';

  // BizMate pattern: 3 views in one component
  isLoginView: boolean = true;
  isScanQRView: boolean = false;
  isVerifyOTPView: boolean = false;
  isOtpInvalid: boolean = false;
  isOTPVerifyProgress: boolean = false;

  // Store login response (like BizMate authService.authResponse)
  authResult: any = null;

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
      password: fb.control('', [Validators.required, Validators.minLength(8), Validators.maxLength(15)]),
    });

    this.otpForm = fb.group({
      d1: ['', Validators.required], d2: ['', Validators.required], d3: ['', Validators.required],
      d4: ['', Validators.required], d5: ['', Validators.required], d6: ['', Validators.required],
    });

    // Redirect if already logged in
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

  // Step 1: Login with password
  login() {
    let loginInfo = {
      userID: this.loginForm.get('userID')?.value,
      password: this.loginForm.get('password')?.value,
    };

    localStorage.setItem("userID", loginInfo.userID);
    this.isLoading = true;
    this.responseMsg = '';

    this.api.login(loginInfo).subscribe({
      next: (res: any) => {
        if (res.token === 'Invalid') {
          this.responseMsg = res.message;
          this.isLoading = false;
        } else if (res.requiresMfa) {
          // Store auth result (like BizMate)
          this.authResult = {
            secretKey: res.secretKey,
            qrCodeImage: res.qrImage,
            userID: res.userId,
            userId: res.userIdNum,
            email: res.email
          };

          this.isLoading = false;
          this.isLoginView = false;

          if (res.requiresSetup && this.authResult.qrCodeImage) {
            this.isScanQRView = true;
          } else {
            this.isVerifyOTPView = true;
          }
        } else {
          // Direct login (no MFA — fallback)
          this.isLoading = false;
          this.handleSuccessLogin(res.token, res.menus);
        }
      },
      error: (err: any) => {
        this.responseMsg = 'Login failed. Please try again.';
        this.isLoading = false;
      },
    });
  }

  // Step 2/3: Verify OTP (same for QR scan view and verify view)
  onVerifyOtp() {
    const otp = `${this.otpForm.get('d1')?.value}${this.otpForm.get('d2')?.value}${this.otpForm.get('d3')?.value}${this.otpForm.get('d4')?.value}${this.otpForm.get('d5')?.value}${this.otpForm.get('d6')?.value}`;
    if (!otp || otp.length !== 6 || !this.authResult) return;

    this.isOTPVerifyProgress = true;
    this.isOtpInvalid = false;

    this.api.verifyMFADirect({
      SecretKey: this.authResult.secretKey,
      Code: otp,
      UserID: this.authResult.userID
    }).subscribe({
      next: (res: any) => {
        this.isOTPVerifyProgress = false;

        if (res.code === 100 && res.token && res.token !== 'Invalid') {
          this.toast.success({ detail: "SUCCESS", summary: res.message, duration: 3000, position: 'topRight' });
          this.handleSuccessLogin(res.token, res.menus);
        } else {
          this.showOtpError(res.message || 'Incorrect verification code. Please try again.');
        }
      },
      error: () => {
        this.isOTPVerifyProgress = false;
        this.showOtpError('Verification failed. Please try again.');
      },
    });
  }

  showOtpError(message: string) {
    this.isOtpInvalid = true;
    this.isOTPVerifyProgress = false;
    this.toast.error({ detail: "Verification Failed", summary: message, duration: 5000, position: 'topRight' });

    // Auto-clear error state + reset OTP fields after 5 seconds
    setTimeout(() => {
      this.isOtpInvalid = false;
      this.otpForm.reset();
    }, 5000);
  }

  handleSuccessLogin(token: string, menus: any) {
    this.api.saveToken(token);
    if (menus) {
      this.api.saveUserMenus(menus);
    }

    localStorage.setItem("email", this.api.getTokenUserInfo()?.email || "");

    let isActive = this.api.getTokenUserInfo()?.status.toLocaleUpperCase() === 'ACTIVE';
    if (isActive) {
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
    } else {
      this.responseMsg = this.languageService.getTranslation('activeMsg');
      this.api.deleteToken();
    }
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

  onDigitInput(event: any, index: number) {
    const value = event.target.value;

    // Only allow single digit
    if (value.length > 1) {
      event.target.value = value.charAt(value.length - 1);
      this.otpForm.get('d' + index)?.setValue(event.target.value);
    }

    // Auto-focus next
    if (value && index < 6) {
      const next = event.target.parentElement?.parentElement?.querySelector(`input[data-index="${index + 1}"]`);
      if (next) next.focus();
    }
  }

  onDigitKeydown(event: KeyboardEvent, index: number) {
    // Backspace: clear and go back
    if (event.key === 'Backspace') {
      const current = this.otpForm.get('d' + index)?.value;
      if (!current && index > 1) {
        const prev = (event.target as HTMLElement)?.parentElement?.parentElement?.querySelector(`input[data-index="${index - 1}"]`) as HTMLInputElement;
        if (prev) { prev.focus(); prev.select(); }
      }
    }
    // Enter: submit
    if (event.key === 'Enter' && this.otpForm.valid) {
      this.onVerifyOtp();
    }
  }

  onDigitPaste(event: ClipboardEvent) {
    event.preventDefault();
    const paste = event.clipboardData?.getData('text')?.trim() || '';
    const digits = paste.replace(/\D/g, '').substring(0, 6);
    for (let i = 0; i < digits.length; i++) {
      this.otpForm.get('d' + (i + 1))?.setValue(digits[i]);
    }
    // Focus last filled or 6th
    const focusIndex = Math.min(digits.length, 6);
    const target = (event.target as HTMLElement)?.closest('.otp-digits')?.querySelector(`input[data-index="${focusIndex}"]`) as HTMLInputElement;
    if (target) target.focus();
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      if (this.isLoginView && this.loginForm.valid) {
        this.login();
      } else if ((this.isVerifyOTPView || this.isScanQRView) && this.otpForm.valid) {
        this.onVerifyOtp();
      }
    }
  }

  getPasswordErrors() {
    if (this.Password.hasError('required')) return this.languageService.getTranslation('passwordRequire');
    if (this.Password.hasError('minlength')) return this.languageService.getTranslation('minPassword');
    if (this.Password.hasError('maxlength')) return this.languageService.getTranslation('maxPassword');
    return '';
  }

  get Password(): FormControl {
    return this.loginForm.get('password') as FormControl;
  }

  cancel() {
    this.loginForm.reset();
    this.otpForm.reset();
    this.isOTPVerifyProgress = false;
    this.isLoginView = true;
    this.isScanQRView = false;
    this.isVerifyOTPView = false;
    this.isOtpInvalid = false;
    this.authResult = null;
    this.responseMsg = '';
  }
}
