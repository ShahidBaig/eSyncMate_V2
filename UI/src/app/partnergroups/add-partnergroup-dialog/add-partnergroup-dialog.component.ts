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
import { PartnerGroupsService } from '../../services/partnerGroups.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

interface Customers {
  id: number;
  name: string;
}

@Component({
  selector: 'add-partnergroup-dialog',
  templateUrl: './add-partnergroup-dialog.component.html',
  styleUrls: ['./add-partnergroup-dialog.component.scss'],
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
export class AddPartnerGroupDialogComponent implements OnInit {
  newPartnerGroupForm: FormGroup;
  customerOptions: Customers[] | undefined;

  constructor(
    public dialogRef: MatDialogRef<AddPartnerGroupDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private partnerGroupsApi: PartnerGroupsService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.newPartnerGroupForm = this.fb.group({
      sourceParty: ['', Validators.required],
      destinationParty: [null, Validators.required], 
      description: ['', Validators.required]
    });
   
    this.newPartnerGroupForm.get('sourceParty')?.valueChanges.subscribe(() => this.updateDescription());
    this.newPartnerGroupForm.get('destinationParty')?.valueChanges.subscribe(() => this.updateDescription());
  }

  ngOnInit() {
    this.getCustomersData();
  }

  updateDescription() {
    const sourcePartyId = this.newPartnerGroupForm.get('sourceParty')?.value || '';
    const destinationPartyId = this.newPartnerGroupForm.get('destinationParty')?.value || '';

    const sourcePartyName = this.customerOptions?.find(customer => customer.id === sourcePartyId)?.name || '';
    const destinationPartyName = this.customerOptions?.find(customer => customer.id === destinationPartyId)?.name || '';
    this.newPartnerGroupForm.get('description')?.setValue(`${sourcePartyName} - ${destinationPartyName}`);
  }

  getCustomersData() {
    this.partnerGroupsApi.getCustomersData().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    const partnerGroupModel = {
      sourcePartyId: this.newPartnerGroupForm.get('sourceParty')?.value,
      destinationPartyId: this.newPartnerGroupForm.get('destinationParty')?.value,
      description: this.newPartnerGroupForm.get('description')?.value
    };

    if (this.newPartnerGroupForm.valid) {
      this.partnerGroupsApi.savePartnerGroup(partnerGroupModel).subscribe({
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

