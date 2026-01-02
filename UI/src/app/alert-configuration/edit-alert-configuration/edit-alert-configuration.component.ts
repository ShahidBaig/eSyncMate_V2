import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { alertsConfigurationService } from '../../services/alertsConfiguration.service';

@Component({
  selector: 'edit-alert-configuration',
  templateUrl: './edit-alert-configuration.component.html',
  styleUrls: ['./edit-alert-configuration.component.scss'],
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
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatIconModule,
    MatProgressSpinnerModule,
    CommonModule,
    MatSelectModule,
    FormsModule,
    TranslateModule
  ],
})
export class EditAlertConfigurationComponent implements OnInit {
  updateAlertForm: FormGroup | any;

  constructor(
    public dialogRef: MatDialogRef<EditAlertConfigurationComponent>,
    private formBuilder: FormBuilder,
    private CustomersApi: alertsConfigurationService,
    private toast: NgToastService,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {
    this.updateAlertForm = this.formBuilder.group({
      alertId: [this.data.alertID],
      alertName: [this.data.alertName, Validators.required],
      query: [this.data.query, Validators.required],
      alertType: [this.data.alertType, Validators.required],
      emailSubject: [this.data.emailSubject],
      emailBody: [this.data.emailBody],
    });
  }

  
  onCancel() {
    this.dialogRef.close();
  }

  updateAlertConfiguration(): void {
    const AlertModel = {
      alertId: this.updateAlertForm.get('alertId')?.value,
      alertName: this.updateAlertForm.get('alertName')?.value,
      query: this.updateAlertForm.get('query')?.value,
      alertType: this.updateAlertForm.get('alertType')?.value,
      emailSubject: this.updateAlertForm.get('emailSubject')?.value,
      emailBody: this.updateAlertForm.get('emailBody')?.value,
    };

    if (this.updateAlertForm.valid) {
      this.CustomersApi.updateConnector(AlertModel).subscribe({
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
