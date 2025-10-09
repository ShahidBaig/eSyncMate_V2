import { Component, Inject, OnInit } from '@angular/core';
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
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'history-customer-product-catalog-dialog',
  templateUrl: './history-customer-product-catalog-dialog.component.html',
  styleUrls: ['./history-customer-product-catalog-dialog.component.scss'],
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

export class HistoryCustomerProductCatalogDialogComponent {
  displayedColumns: string[] = ['ERPCustomerID', 'FileName', 'CreatedDate']; // Add the actual column names from your data
  dataSource = this.data.historyData;
  constructor(

    public dialogRef: MatDialogRef<HistoryCustomerProductCatalogDialogComponent>,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any) {

  }


  closeDialog(): void {
    this.dialogRef.close();
  }

}
