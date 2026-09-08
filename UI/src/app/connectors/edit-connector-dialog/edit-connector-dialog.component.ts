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
import { ConnectorsService } from '../../services/connectors.service';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs';
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
  selector: 'edit-connector-dialog',
  templateUrl: './edit-connector-dialog.component.html',
  styleUrls: ['./edit-connector-dialog.component.scss'],
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
    MatCheckboxModule,
    MatChipsModule,
    MatTabsModule,
    TranslateModule
  ],
})
export class EditConnectorDialogComponent implements OnInit {
  updateConnectorForm: FormGroup | any;
  connectorTypesOptions: ConnectorType[] | undefined;
  // File, SFTP and FTP connectors have always existed in the database and been used by routes,
  // but until now could only be created and edited in SQL because this list stopped at Rest.
  connectivityTypeOptions = ['SqlServer', 'Rest', 'File', 'SFTP', 'FTP'];
  headerChipsValues: HeaderList[] = [];
  paramValues: ParamList[] = [];
  hide = true;
  consumerSecrethide = true;
  tokenHide = true;
  tokenSecretHide = true;

  constructor(
    public dialogRef: MatDialogRef<EditConnectorDialogComponent>,
    private formBuilder: FormBuilder,
    private ConnectorsApi: ConnectorsService,
    private toast: NgToastService,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {

    let jsonObject: any = {};
    try {
      if (this.data.data && this.data.data.trim() !== '') {
        jsonObject = JSON.parse(this.data.data);
      }
    } catch (e) {
      jsonObject = {};
    }

    const connectivityType = jsonObject.ConnectivityType || '';

    if (connectivityType === 'SqlServer')
    {
      this.updateConnectorForm = this.formBuilder.group({
        id: [this.data.id, Validators.required],
        name: [this.data.name, Validators.required],
        connectivityType: [jsonObject.ConnectivityType, Validators.required],
        commandType: [jsonObject.CommandType],
        command: [jsonObject.Command],
        keyFieldName: [jsonObject.KeyFieldName],
        dataFieldName: [jsonObject.DataFieldName],
        customerID: [jsonObject.CustomerID],
        jsonDataCollectionName: [jsonObject.JsonDataCollectionName],
        connectionString: [jsonObject.ConnectionString],
        typeId: [this.data.typeId, Validators.required], // Add this line
        baseUrl: [''],
        authType: [''],
        host: [''],
        url: [''],
        method: [''],
        bodyFormat: [''],
        consumerKey: [''],
        consumerSecret: [''],
        token: [''],
        tokenSecret: [''],
        realm: [''],
        headerValue: [''],
        headerName: [''],
        paramName: [''],
        paramValue: [''],
      });
    }

    // File, SFTP and FTP. Before this branch existed a connector of one of these types matched
    // neither of the two below, so updateConnectorForm was never assigned and the dialog threw on
    // open - the reason they could only ever be edited in SQL.
    if (connectivityType === 'File' || connectivityType === 'SFTP' || connectivityType === 'FTP')
    {
      this.updateConnectorForm = this.formBuilder.group({
        id: [this.data.id, Validators.required],
        name: [this.data.name, Validators.required],
        connectivityType: [connectivityType, Validators.required],
        typeId: [this.data.typeId, Validators.required],

        host: [jsonObject.Host || ''],
        baseUrl: [jsonObject.BaseUrl || ''],
        url: [jsonObject.Url || ''],
        method: [jsonObject.Method || ''],
        consumerKey: [jsonObject.ConsumerKey || ''],
        consumerSecret: [jsonObject.ConsumerSecret || ''],
        realm: [jsonObject.Realm || ''],
        customerID: [jsonObject.CustomerID || ''],

        // Not used by a transfer, but the shared template binds them.
        authType: [jsonObject.AuthType || ''],
        bodyFormat: [''],
        token: [''],
        tokenSecret: [''],
        commandType: [''],
        command: [''],
        keyFieldName: [''],
        dataFieldName: [''],
        jsonDataCollectionName: [''],
        connectionString: [''],
        headerValue: [''],
        headerName: [''],
        paramName: [''],
        paramValue: [''],
      });
    }

    if (connectivityType === 'Rest' || connectivityType === '')
    {
      this.updateConnectorForm = this.formBuilder.group({
        id: [this.data.id, Validators.required],
        name: [this.data.name, Validators.required],
        connectivityType: [jsonObject.ConnectivityType || '', Validators.required],
        baseUrl: [jsonObject.BaseUrl || ''],
        authType: [jsonObject.AuthType || ''],
        host: [jsonObject.Host || ''],
        url: [jsonObject.Url || ''],
        method: [jsonObject.Method || ''],
        bodyFormat: [jsonObject.BodyFormat || ''],
        consumerKey: [jsonObject.ConsumerKey || ''],
        consumerSecret: [jsonObject.ConsumerSecret || ''],
        token: [jsonObject.Token || ''],
        tokenSecret: [jsonObject.TokenSecret || ''],
        realm: [jsonObject.Realm || ''],
        headerValue: [''],
        headerName: [''],
        paramName: [''],
        paramValue: [''],
        commandType: [''],
        command: [''],
        keyFieldName: [''],
        dataFieldName: [''],
        customerID: [jsonObject.CustomerID || ''],
        jsonDataCollectionName: [''],
        typeId: [this.data.typeId, Validators.required], // Add this line
      });

      if (jsonObject.Headers?.length > 0)
      {
        for (const obj of jsonObject.Headers) {
          const header: HeaderList =
          {
            name: obj.name,
            value: obj.value
          };

          this.headerChipsValues.push(header);
        }
      }

      if (jsonObject.Parmeters?.length > 0) {
        for (const obj of jsonObject.Parmeters) {
          const param: ParamList =
          {
            name: obj.name,
            value: obj.value
          };

          this.paramValues.push(param);
        }
      }
    }

    this.loadConnectorTypes();
  }

  loadConnectorTypes() {
    this.ConnectorsApi.getConnectorTypesData().subscribe({
      next: (res: any) => {
        this.connectorTypesOptions = res.connectorTypes;
      },
    });
  }

  onCancel() {
    this.dialogRef.close();
  }

  get showSqlServerTab(): boolean {
    return this.updateConnectorForm.get('connectivityType')?.value === 'SqlServer';
  }

  get showFileTab(): boolean {
    return this.updateConnectorForm.get('connectivityType')?.value === 'File';
  }

  get showSftpTab(): boolean {
    return this.updateConnectorForm.get('connectivityType')?.value === 'SFTP';
  }

  get showFtpTab(): boolean {
    return this.updateConnectorForm.get('connectivityType')?.value === 'FTP';
  }

  get isTransferType(): boolean {
    return ['File', 'SFTP', 'FTP'].includes(this.updateConnectorForm.get('connectivityType')?.value);
  }

  /**
   * Builds the connector JSON for a File, SFTP or FTP transfer.
   *
   * AuthType is set to the connectivity type rather than typed by the user: every transfer route
   * branches on AuthType, so a connector whose AuthType does not match its ConnectivityType is
   * simply never read, silently.
   *
   * The field a value lands in is decided by what the connector class reads, not by the label.
   * FtpConnector takes the remote path as Method; SftpConnector and FileConnector take it as
   * BaseUrl. The labels describe the meaning; this maps them.
   */
  buildTransferData(): any {
    const form = this.updateConnectorForm;
    const type = form.get('connectivityType')?.value;
    const data: any = {};

    data.ConnectivityType = type;
    data.AuthType = type;
    data.CustomerID = form.get('customerID')?.value;
    data.Realm = form.get('realm')?.value;

    if (type === 'File') {
      data.BaseUrl = form.get('baseUrl')?.value;
      data.Url = form.get('url')?.value;
      data.Method = form.get('method')?.value || '*';
    }

    if (type === 'SFTP') {
      data.Host = form.get('host')?.value;
      data.ConsumerKey = form.get('consumerKey')?.value;
      data.ConsumerSecret = form.get('consumerSecret')?.value;
      data.BaseUrl = form.get('baseUrl')?.value;
      data.Url = form.get('url')?.value;
    }

    if (type === 'FTP') {
      data.Host = form.get('host')?.value;
      data.ConsumerKey = form.get('consumerKey')?.value;
      data.ConsumerSecret = form.get('consumerSecret')?.value;
      data.Method = form.get('method')?.value;
    }

    return data;
  }

  get showRestTab(): boolean {
    return this.updateConnectorForm.get('connectivityType')?.value === 'Rest';
  }


  addChips() {
    const headerNameControl = this.updateConnectorForm.get('headerName');
    const headerValueControl = this.updateConnectorForm.get('headerValue');

    if (headerNameControl && headerValueControl && headerNameControl.value !== null && headerValueControl.value !== null) {

      const newHeader: HeaderList = { name: headerNameControl.value, value: headerValueControl.value };
      this.headerChipsValues.push(newHeader);

      headerNameControl.setValue('');
      headerValueControl.setValue('');
    }
  }

  removeChip(index: number) {
    this.headerChipsValues.splice(index, 1);
  }

  editChip(index: number) {
    const chip = this.headerChipsValues[index];
    this.updateConnectorForm.get('headerName')?.setValue(chip.name);
    this.updateConnectorForm.get('headerValue')?.setValue(chip.value);
    this.headerChipsValues.splice(index, 1);
  }

  addParameter() {
    const paramNameControl = this.updateConnectorForm.get('paramName');
    const paramValueControl = this.updateConnectorForm.get('paramValue');

    if (paramNameControl && paramValueControl && paramNameControl.value !== null && paramValueControl.value !== null) {

      const newHeader: ParamList = { name: paramNameControl.value, value: paramValueControl.value };
      this.paramValues.push(newHeader);

      paramNameControl.setValue('');
      paramValueControl.setValue('');
    }
  }

  removeParam(index: number) {
    this.paramValues.splice(index, 1);
  }

  editParam(index: number) {
    const param = this.paramValues[index];
    this.updateConnectorForm.get('paramName')?.setValue(param.name);
    this.updateConnectorForm.get('paramValue')?.setValue(param.value);
    this.paramValues.splice(index, 1);
  }


  updateConnector(): void {

    let data: any = {};
    let jsonString: string = "";

    if (this.updateConnectorForm.get('connectivityType')?.value === 'SqlServer')
    {
      data.ConnectivityType = this.updateConnectorForm.get('connectivityType')?.value;
      data.CommandType = this.updateConnectorForm.get('commandType')?.value;
      data.Command = this.updateConnectorForm.get('command')?.value;
      data.KeyFieldName = this.updateConnectorForm.get('keyFieldName')?.value;
      data.DataFieldName = this.updateConnectorForm.get('dataFieldName')?.value;
      data.CustomerID = this.updateConnectorForm.get('customerID')?.value;
      data.JsonDataCollectionName = this.updateConnectorForm.get('jsonDataCollectionName')?.value;
      data.ConnectionString = this.updateConnectorForm.get('connectionString')?.value;

      jsonString = JSON.stringify(data);
    }

    if (this.updateConnectorForm.get('connectivityType')?.value === 'Rest') {
      data.ConnectivityType = this.updateConnectorForm.get('connectivityType')?.value;
      data.BaseUrl = this.updateConnectorForm.get('baseUrl')?.value;
      data.AuthType = this.updateConnectorForm.get('authType')?.value;
      data.Host = this.updateConnectorForm.get('host')?.value;
      data.Url = this.updateConnectorForm.get('url')?.value;
      data.Method = this.updateConnectorForm.get('method')?.value;

      data.Headers = [];
      data.Parmeters = [];

      if (this.headerChipsValues.length > 0) {
        for (const obj of this.headerChipsValues) {
          const header: HeaderList =
          {
            name: obj.name,
            value: obj.value
          };

          data.Headers.push(header);
        }
      }

      if (this.paramValues.length > 0) {
        for (const obj of this.paramValues) {
          const param: ParamList =
          {
            name: obj.name,
            value: obj.value
          };

          data.Parmeters.push(param);
        }
      }

      data.BodyFormat = this.updateConnectorForm.get('bodyFormat')?.value;
      data.ConsumerKey = this.updateConnectorForm.get('consumerKey')?.value;
      data.ConsumerSecret = this.updateConnectorForm.get('consumerSecret')?.value;
      data.Token = this.updateConnectorForm.get('token')?.value;
      data.TokenSecret = this.updateConnectorForm.get('tokenSecret')?.value;
      data.Realm = this.updateConnectorForm.get('realm')?.value;
      data.CustomerID = this.updateConnectorForm.get('customerID')?.value;

      jsonString = JSON.stringify(data);
    }

    // File, SFTP and FTP - the transfers a route reads documents from and writes them back to.
    if (this.isTransferType)
    {
      jsonString = JSON.stringify(this.buildTransferData());
    }

    const customerModel = {
      id: this.updateConnectorForm.get('id')?.value,
      name: this.updateConnectorForm.get('name')?.value,
      data: jsonString,
      typeId: this.updateConnectorForm.get('typeId')?.value, // Add this line
    };

    if (this.updateConnectorForm.valid) {
      this.ConnectorsApi.updateConnector(customerModel).subscribe({
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

