import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DatePipe, NgIf, formatDate, NgFor } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule, AbstractControl, ValidationErrors, ValidatorFn, AbstractControlOptions } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { RouteTypesService } from '../../services/route-types.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { User } from '../../models/models';
import { MatGridListModule } from '@angular/material/grid-list';
import { UserService } from '../../services/user.service';
import { MatCheckboxModule } from '@angular/material/checkbox';

interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'edit-users-dialog',
  templateUrl: './edit-users-dialog.component.html',
  styleUrls: ['./edit-users-dialog.component.scss'],
  standalone: true,
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
    MatCardModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    NgFor,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatIconModule,
    MatProgressSpinnerModule,
    CommonModule,
    MatSelectModule,
    FormsModule,
    TranslateModule,
    MatGridListModule,
    MatCheckboxModule
  ],
})
export class EditUsersDialogComponent implements OnInit {
  updateUserForm: FormGroup | any;
  routeTypesOptions: User[] | undefined;
  customerOptions: Customers[] | undefined;
  hide = true;
  hideConfirm = true;
  constructor(
    public dialogRef: MatDialogRef<EditUsersDialogComponent>,
    private formBuilder: FormBuilder,
    private userApi: UserService,
    private toast: NgToastService,
    public languageService: LanguageService,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
}

  ngOnInit() {
    this.initializeForm();
    this.getERPCustomer();
  }

  getERPCustomer() {
    this.userApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }

  initializeForm() {
    this.updateUserForm = this.fb.group(
      {
        id: this.fb.control(this.data.id, [Validators.required]),
        userID: this.fb.control(this.data.userID),
        firstName: this.fb.control(this.data.firstName, [Validators.required]),
        lastName: this.fb.control(this.data.lastName),
        email: this.fb.control(this.data.email, [Validators.required, Validators.email]),
        mobile: this.fb.control(this.data.mobile),
        password: this.fb.control(this.data.password, [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(15),
        ]),
        rpassword: this.fb.control(this.data.password),
        userType: [this.data.userType, Validators.required],
        status: [this.data.status, Validators.required],
        customerName: [this.data.customerName ? this.data.customerName.split(',') : [], Validators.required],
        isSetupMenu: [this.data.isSetupAllowed]
      },
      {
        validators: [repeatPasswordValidator],
      } as AbstractControlOptions
    );
  }

  onCancel() {
    this.dialogRef.close();
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

  getCustomerError() {
    if (this.CustomerName.hasError('required')) return this.languageService.getTranslation('fieldRequiredMsg');
    if (this.CustomerName.hasError('customerName')) return this.languageService.getTranslation('selectCustomer');
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

  get FirstName(): FormControl {
    return this.updateUserForm.get('firstName') as FormControl;
  }
  get LastName(): FormControl {
    return this.updateUserForm.get('lastName') as FormControl;
  }
  get Email(): FormControl {
    return this.updateUserForm.get('email') as FormControl;
  }
  get Password(): FormControl {
    return this.updateUserForm.get('password') as FormControl;
  }
  get RPassword(): FormControl {
    return this.updateUserForm.get('rpassword') as FormControl;
  }

  get CustomerName(): FormControl {
    return this.updateUserForm.get('customerName') as FormControl;
  }

  updateUsers(): void {
    const userModel = {
      id: this.updateUserForm.get('id')?.value,
      firstName: this.updateUserForm.get('firstName')?.value,
      lastName: this.updateUserForm.get('lastName')?.value, // Add this line
      email: this.updateUserForm.get('email')?.value, // Add this line
      mobile: this.updateUserForm.get('mobile')?.value, // Add this line
      password: this.updateUserForm.get('password')?.value, // Add this line
      userType: this.updateUserForm.get('userType')?.value, // Add this line
      status: this.updateUserForm.get('status')?.value, // Add this line
      customerName: '',
      isSetupAllowed: this.updateUserForm.get('isSetupMenu')?.value,
      createdDate: this.data.createdDate,
      userID: this.data.userID,
    };

    let values = this.updateUserForm.get('customerName')?.value;
    userModel.customerName = values.join(',');

    if (this.updateUserForm.valid) {
      this.userApi.updateUser(userModel).subscribe({
        next: (res) => {
          if (res.code === 100) {
            this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
          } else if (res.code === 400) {
            this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else if (res.code === 401) {
            this.toast.warning({ detail: "WARNING", summary: res.description, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          }

          this.dialogRef.close('updated');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        }
      });
    }
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
