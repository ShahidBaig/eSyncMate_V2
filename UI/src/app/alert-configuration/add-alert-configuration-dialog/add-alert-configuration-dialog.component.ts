import { Component, inject, Inject, OnInit } from '@angular/core';
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
import { ConnectorsService } from '../../services/connectors.service';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipEditedEvent, MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { alertsConfigurationService } from '../../services/alertsConfiguration.service';

interface ConnectorType {
  id: number;
  name: string;
}

interface HeaderList {
  name: number;
  value: string;
}

interface ParamList {
  name: number;
  value: string;
}
interface Customers {
  erpCustomerID: number;
  name: string;
}

interface AlertType {
  name: string;
}


@Component({
  selector: 'add-alert-configuration-dialog',
  templateUrl: './add-alert-configuration-dialog.component.html',
  styleUrls: ['./add-alert-configuration-dialog.component.scss'],
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
    MatTabsModule,
    MatCheckboxModule,
    MatChipsModule,
    TranslateModule
  ],
})

export class AddAlertConfigurationDialogComponent implements OnInit {
  newConnectorForm: FormGroup;
  connectorTypesOptions: ConnectorType[] | undefined;
  connectivityTypeOptions = ['SqlServer', 'Rest'];
  headerChipsValues: HeaderList[] = [];
  paramValues: ParamList[] = [];
  hide = true;
  consumerSecrethide = true;
  tokenHide = true;
  tokenSecretHide = true;
  customerOptions: Customers[] | undefined;

  constructor(
    public dialogRef: MatDialogRef<AddAlertConfigurationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private AlertsConfigurationServiceApi: alertsConfigurationService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.newConnectorForm = this.fb.group({
      alertName: ['', Validators.required],
      //customerID: [null, Validators.required], // Form control for connector type ID
      query: ['', Validators.required],
      alertType: [null, Validators.required],
      emailSubject: [''],
      emailBody: [''],

    });
  }

  ngOnInit() {
    this.getConnectorTypesData();
    this.getCustomersData();
  }

  getConnectorTypesData() {
    this.AlertsConfigurationServiceApi.getConnectorTypesData().subscribe({
      next: (res: any) => {
        this.connectorTypesOptions = res.connectorTypes;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  getCustomersData() {
    this.AlertsConfigurationServiceApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }
  

  onSave(): void {
    let data: any = {};
    
    const connectorModel =
    {
      alertName: this.newConnectorForm.get('alertName')?.value,
      emailSubject: this.newConnectorForm.get('emailSubject')?.value,
      emailBody: this.newConnectorForm.get('emailBody')?.value,
      query: this.newConnectorForm.get('query')?.value,
      alertType: this.newConnectorForm.get('alertType')?.value,
    };

    if (this.newConnectorForm.valid) {
      this.AlertsConfigurationServiceApi.saveConnector(connectorModel).subscribe({
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

          this.dialogRef.close('saved');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });

        }
      });
    }
  }
}

