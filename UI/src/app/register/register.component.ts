import { Component } from '@angular/core';
import { AbstractControl, AbstractControlOptions, FormBuilder, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators, ReactiveFormsModule } from '@angular/forms';
import { User, UserType } from '../models/models';
import { ApiService } from '../services/api.service';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NgIf, NgFor } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatSelectModule } from '@angular/material/select';
import { NgToastService } from 'ng-angular-popup';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { UserService } from '../services/user.service';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { environment } from 'src/environments/environment';
interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: true,
  imports: [
    MatGridListModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    NgFor,
    MatButtonModule,
    MatIconModule,
    RouterLink,
    MatSelectModule,
    TranslateModule,
    MatCheckboxModule
  ],
})
export class RegisterComponent {
  hide = true;
  responseMsg: string = '';
  registerForm: FormGroup;
  userTypeOptions = ['Admin', 'Reader'];
  statusOptions = ['Active', 'Blocked'];
  customerOptions: Customers[] | undefined;

  constructor(private fb: FormBuilder, private api: ApiService,
    private toast: NgToastService,
    private route: Router,
    public languageService: LanguageService,
    private user: UserService
  ) {
    this.registerForm = fb.group(
      {
        firstName: fb.control('', [Validators.required]),
        lastName: fb.control(''),
        email: fb.control('', [Validators.required, Validators.email]),
        mobile: fb.control(''),
        password: fb.control('', [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(15),
        ]),
        rpassword: fb.control(''),
        userType: ['', Validators.required],
        status: ['', Validators.required],
        customerName: ['', Validators.required],
        isSetupMenu: false,
        userID: fb.control('', [Validators.required]),
      },
      {
        validators: [repeatPasswordValidator],
      } as AbstractControlOptions
    );
  }

  ngOnInit(): void {
    this.getERPCustomer();
  }

  getERPCustomer() {
    this.user.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }
  register() {
    const user = {
      id: 0,
      firstName: this.registerForm.get('firstName')?.value,
      lastName: this.registerForm.get('lastName')?.value,
      email: this.registerForm.get('email')?.value,
      userType: this.registerForm.get('userType')?.value,
      mobile: this.registerForm.get('mobile')?.value,
      password: this.registerForm.get('password')?.value,
      status: this.registerForm.get('status')?.value,
      customerName: '',
      isSetupAllowed: this.registerForm.get('isSetupMenu')?.value,
      userID: this.registerForm.get('userID')?.value,
    };

    let values = this.registerForm.get('customerName')?.value;
    user.customerName = values.join(',');

    if (this.registerForm.valid) {
      this.api.createAccount(user).subscribe({
        next: (res: any) => {

          if (res.code == 100) {
            this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
            if (environment.productName === 'SURGIMAC') {
              this.route.navigate(['edi/purchaseOrder']);
            } else {
              this.route.navigate(['edi/all-orders']);
            }
          }
          else {
            this.toast.info({ detail: "", summary: res.description, duration: 5000, position: 'topRight' });
          }
        },
        error: (err: any) => {
          console.log('Error: ');
          console.log(err);
        },
      });
    }
  }
  getUserIDErrors() {
    if (this.FirstName.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    return '';
  }

  getFirstNameErrors() {
    if (this.FirstName.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    return '';
  }
  getLastNameErrors() {
    if (this.LastName.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    return '';
  }
  getEmailErrors() {
    if (this.Email.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    if (this.Email.hasError('email')) return this.languageService.getTranslation('invalidEmailMsg');
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

  getCustomerError() {
    if (this.CustomerName.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    if (this.CustomerName.hasError('customerName')) return this.languageService.getTranslation('selectCustomer');
    return '';
  }
  get FirstName(): FormControl {
    return this.registerForm.get('firstName') as FormControl;
  }
  get LastName(): FormControl {
    return this.registerForm.get('lastName') as FormControl;
  }
  get Email(): FormControl {
    return this.registerForm.get('email') as FormControl;
  }
  get Password(): FormControl {
    return this.registerForm.get('password') as FormControl;
  }
  get RPassword(): FormControl {
    return this.registerForm.get('rpassword') as FormControl;
  }
  get CustomerName(): FormControl {
    return this.registerForm.get('customerName') as FormControl;
  }

  get UserID(): FormControl {
    return this.registerForm.get('userID') as FormControl;
  }
}

export const repeatPasswordValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const pwd = control.get('password')?.value;
  const rpwd = control.get('rpassword')?.value;
  if (pwd === rpwd) {
    control.get('rpassword')?.setErrors(null);
    return null;
  } else {
    control.get('rpassword')?.setErrors({ rpassword: true });
    return { rpassword: true };
  }
};
