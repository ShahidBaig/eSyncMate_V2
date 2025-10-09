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

@Component({
  selector: 'add-connector-dialog',
  templateUrl: './add-connector-dialog.component.html',
  styleUrls: ['./add-connector-dialog.component.scss'],
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

export class AddConnectorDialogComponent implements OnInit {
  newConnectorForm: FormGroup;
  connectorTypesOptions: ConnectorType[] | undefined;
  connectivityTypeOptions = ['SqlServer', 'Rest'];
  headerChipsValues: HeaderList[] = [];
  paramValues: ParamList[] = [];
  hide = true;
  consumerSecrethide = true;
  tokenHide = true;
  tokenSecretHide = true;

  constructor(
    public dialogRef: MatDialogRef<AddConnectorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private ConnectorsApi: ConnectorsService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.newConnectorForm = this.fb.group({
      name: ['', Validators.required],
      typeId: [null, Validators.required], // Form control for connector type ID
      data: [''],
      connectivityType: ['', Validators.required],
      commandType: [''],
      command: [''],
      keyFieldName: [''],
      dataFieldName: [''],
      customerID: [''],
      jsonDataCollectionName: [''],
      authType: [''],
      host: [''],
      baseUrl: [''],
      url: [''],
      method: [''],
      bodyFormat: [''],
      consumerKey: [''],
      consumerSecret: [''],
      token: [''],
      tokenSecret: [''],
      realm: [''],
      headerName: [''],
      headerValue: [''],
      paramName: [''],
      paramValue: [''],
      connectionString:[''],
    });
  }

  ngOnInit() {
    this.getConnectorTypesData();
  }

  getConnectorTypesData() {
    this.ConnectorsApi.getConnectorTypesData().subscribe({
      next: (res: any) => {
        this.connectorTypesOptions = res.connectorTypes;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get showSqlServerTab(): boolean {
    return this.newConnectorForm.get('connectivityType')?.value === 'SqlServer';
  }

  get showRestTab(): boolean {
    return this.newConnectorForm.get('connectivityType')?.value === 'Rest';
  }

  addChips()
  {
    const headerNameControl = this.newConnectorForm.get('headerName');
    const headerValueControl = this.newConnectorForm.get('headerValue');
    
    if (headerNameControl && headerValueControl && headerNameControl.value !== null && headerValueControl.value !== null) {

      const newHeader: HeaderList = { name: headerNameControl.value, value: headerValueControl.value };
      this.headerChipsValues.push(newHeader);

      headerNameControl.setValue('');
      headerValueControl.setValue('');
    }
  }

  removeChip(index: number)
  {
    this.headerChipsValues.splice(index, 1);
  }

  addParameter()
  {
    const paramNameControl = this.newConnectorForm.get('paramName');
    const paramValueControl = this.newConnectorForm.get('paramValue');

    if (paramNameControl && paramValueControl && paramNameControl.value !== null && paramValueControl.value !== null) {

      const newHeader: ParamList = { name: paramNameControl.value, value: paramValueControl.value };
      this.paramValues.push(newHeader);

      paramNameControl.setValue('');
      paramValueControl.setValue('');
    }
  }

  removeParam(index: number)
  {
    this.paramValues.splice(index, 1);
  }

  onSave(): void
  {
    let data: any = {};
    let jsonString: string = "";

    if (this.newConnectorForm.get('connectivityType')?.value === 'SqlServer')
    {
      data.ConnectivityType = this.newConnectorForm.get('connectivityType')?.value;
      data.CommandType = this.newConnectorForm.get('commandType')?.value;
      data.Command = this.newConnectorForm.get('command')?.value;
      data.KeyFieldName = this.newConnectorForm.get('keyFieldName')?.value;
      data.DataFieldName = this.newConnectorForm.get('dataFieldName')?.value;
      data.CustomerID = this.newConnectorForm.get('customerID')?.value;
      data.JsonDataCollectionName = this.newConnectorForm.get('jsonDataCollectionName')?.value;
      data.ConnectionString = this.newConnectorForm.get('connectionString')?.value;

      jsonString = JSON.stringify(data);
    }

    if (this.newConnectorForm.get('connectivityType')?.value === 'Rest')
    {
      data.ConnectivityType = this.newConnectorForm.get('connectivityType')?.value;
      data.BaseUrl = this.newConnectorForm.get('baseUrl')?.value;
      data.AuthType = this.newConnectorForm.get('authType')?.value;
      data.Host = this.newConnectorForm.get('host')?.value;
      data.Url = this.newConnectorForm.get('url')?.value;
      data.Method = this.newConnectorForm.get('method')?.value;

      data.Headers = [];
      data.Parmeters = [];

      if (this.headerChipsValues.length > 0)
      {
        for (const obj of this.headerChipsValues)
        {
          const header: HeaderList =
          {
            name: obj.name,
            value: obj.value
          };

          data.Headers.push(header);
        }
      }

      if (this.paramValues.length > 0)
      {
        for (const obj of this.paramValues)
        {
          const param: ParamList =
          {
            name: obj.name,
            value: obj.value
          };

          data.Parmeters.push(param);
        }
      }

      data.BodyFormat = this.newConnectorForm.get('bodyFormat')?.value;
      data.ConsumerKey = this.newConnectorForm.get('consumerKey')?.value;
      data.ConsumerSecret = this.newConnectorForm.get('consumerSecret')?.value;
      data.Token = this.newConnectorForm.get('token')?.value;
      data.TokenSecret = this.newConnectorForm.get('tokenSecret')?.value;
      data.Realm = this.newConnectorForm.get('realm')?.value;
      data.CustomerID = this.newConnectorForm.get('customerID')?.value;

      jsonString = JSON.stringify(data);
    }

    const connectorModel =
    {
      name: this.newConnectorForm.get('name')?.value,
      typeId: this.newConnectorForm.get('typeId')?.value,
      data: jsonString
    };

    if (this.newConnectorForm.valid)
    {
      this.ConnectorsApi.saveConnector(connectorModel).subscribe({
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

