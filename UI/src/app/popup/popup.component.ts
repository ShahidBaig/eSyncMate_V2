import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Component, OnInit, Inject } from '@angular/core';
import { DatePipe, NgIf, NgFor} from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule, FormsModule  } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule, MatSelectChange } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FileContentViewerDialogComponent } from '../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { MatGridListModule } from '@angular/material/grid-list';
import { CarrierLoadTenderService } from '../services/carrierLoadTender.service';
import { NgToastService } from 'ng-angular-popup';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
interface TypeMap {
  [key: string]: { name: string; icon: string };
}


const typeMap: TypeMap = {
  'API-JSON': { name: 'Order Received from Customer', icon: 'arrow_downward' },
  'API-JSON-REQ': { name: 'Order Received', icon: 'arrow_downward' },
  'API-JSON-RES': { name: 'Order Received', icon: 'arrow_downward' },
  'API-ACK': { name: 'Order Acknowledged in Customer Portal', icon: 'check_circle' },
  'API-ACK-SNT': { name: 'Order Acknowledged Sent to Customer Portal', icon: 'check_circle' },
  'ACK-ERR': { name: 'Order Acknowledged Error in Customer Portal', icon: 'check_circle' },
  'ERP-SNT': { name: 'Order Request Sent to ERP', icon: 'error' },
  'ERP-JSON': { name: 'Order Created in ERP', icon: 'arrow_upward' },
  'ERP.ERROR': { name: 'Order Creation Error in ERP', icon: 'error' },
  'ERP.ERR': { name: 'Order Creation Error in ERP', icon: 'error' },
  'ERP-ERROR': { name: 'Order Creation Error in ERP', icon: 'error' },
  'ERP-ERR': { name: 'Order Creation Error in ERP', icon: 'error' },
  'ERPASN-JSON': { name: 'ASN Received from ERP', icon: 'arrow_downward' },
  'ERPASN-ERR': { name: 'ASN Error Received from ERP', icon: 'arrow_downward' },
  'ASN-SNT': { name: 'ASN Request Sent to Customer', icon: 'arrow_upward' },
  'ASN-RES': { name: 'ASN Created in Customer Portal', icon: 'arrow_upward' },
  'ASN-ERR': { name: 'ASN Creation Error in Customer Portal', icon: 'arrow_upward' },
  'ERPCancelOrder-JSON': { name: 'Order Cancellation Received from ERP', icon: 'cancel' },
  'ERPCANLN-JSON': { name: 'Order Cancellation Sent to Customer', icon: 'cancel' },
  '204-EDI': { name: '204-EDI', icon: 'cancel' },
  '204-JSON': { name: '204-JSON', icon: 'cancel' },
  '214-EDI': { name: '214-EDI', icon: 'cancel' },
  '214-JSON': { name: '214-JSON', icon: 'cancel' },
  '990-EDI': { name: '990-EDI', icon: 'cancel' },
  '990-JSON': { name: '990-JSON', icon: 'cancel' },
  '997-EDI': { name: '997-EDI', icon: 'cancel' },
  '850-eSyncMate': { name: '850-eSyncMate', icon: 'cancel' },
  '850-JSON': { name: '850-JSON', icon: 'cancel' },
  '850-Fields': { name: '850-Fields', icon: 'cancel' },
  '855-JSON': { name: '855-JSON', icon: 'cancel' },
  '855-eSyncMate': { name: '855-eSyncMate', icon: 'cancel' },
  'JSON-SNT': { name: 'JSON-SNT', icon: 'cancel' },
  '856-JSON-SNT': { name: '856-JSON-SNT', icon: 'cancel' },
  'ASN-eSyncMate': { name: '856-JSON-SNT', icon: 'cancel' },
  '810-eSyncMate': { name: '810-eSyncMate', icon: 'cancel' },
};
@Component({
  selector: 'popup', 
  templateUrl: './popup.component.html',
  standalone: true,
  styleUrls: ['./popup.component.scss'],
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
    MatSelectModule,
    NgFor,
    MatGridListModule,
    FormsModule,
    TranslateModule
  ],

})


export class PopupComponent {
  showSpinner: boolean = false;
  showDataColumn: boolean = true;
  optionalProperty: boolean = false;

  columns: string[] = [
    // 'id',
    'type',
    'createdDate',
    'data',
    'fileName',
    'DownloadFile',
  ];

  typeMap: TypeMap = typeMap;

  constructor(@Inject(MAT_DIALOG_DATA) public data: any, public dialogRef: MatDialogRef<PopupComponent>,
    private dialog: MatDialog, private api: CarrierLoadTenderService,private toast: NgToastService, public languageService: LanguageService) {
      this.optionalProperty = this.data?.listAckData?.length > 0 ? true : false;
    }

  updateTrackStatus(){
    this.showSpinner = true;
    var tenderID = this.data?.listOfCarrier.id;
    var trackStatus = this.data?.listOfCarrier.trackStatus;
    var consigneeAddress = this.data?.listOfCarrier.consigneeAddress;
    var consigneeCity = this.data?.listOfCarrier.consigneeCity;
    var consigneeState = this.data?.listOfCarrier.consigneeState;
    var consigneeZip = this.data?.listOfCarrier.consigneeZip;
    var consigneeCountry = this.data?.listOfCarrier.consigneeCountry;
    var equipmentNo = this.data?.listOfCarrier.equipmentNo;
    var manualequipmentNo = this.data?.listOfCarrier.manualEquipmentNo;

    if (manualequipmentNo == "")
    {
        manualequipmentNo = 'Empty';
    }


    this.api.updateTrackStatus(tenderID, trackStatus, consigneeAddress, consigneeCity, consigneeState, consigneeZip, consigneeCountry, equipmentNo, manualequipmentNo).subscribe({
      next: (res: any) => {
        if (res && res.code === 200) {
          console.log("respose", res);
          this.toast.success({ detail: 'SUCCESS', summary: res.message, duration: 5000, position: 'topRight' });
          this.dialogRef.close();
        } else {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 5000, position: 'topRight' });
        }
        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      }
    });
  }

  downloadFile(data: string, filename: string) {
    this.showSpinner = true;

    const blob = new Blob([data], { type: 'text/plain' });
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = filename;
    link.click();

    this.showSpinner = false;
  }

  closeDialog(): void {
    this.dialogRef.close();
  }

  viewFile(data: string, filename: any) {
    const fileExtension = filename.split('.').pop().toLowerCase();
    let parsedData;

    if (fileExtension === 'json') {
      try {
        parsedData = JSON.parse(data);
      } catch (e) {
        console.error('Error parsing JSON', e);
      }
    } else if (fileExtension === 'edi') {
      data = data.replace(/~/g, '\n');
      parsedData = data;
    } else {
      parsedData = data;
    }

    this.dialog.open(FileContentViewerDialogComponent, {
      data: { content: parsedData, type: fileExtension },
      width: '800px',
    });
  }

  onStateChange(event: MatSelectChange) {
    this.data.listOfCarrier.consigneeState = event.value;
  }
}
