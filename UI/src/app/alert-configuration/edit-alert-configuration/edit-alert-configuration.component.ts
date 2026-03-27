import { Component, Inject, OnInit, ViewChild } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CommonModule, DatePipe, NgIf } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule, MatChipInputEvent } from '@angular/material/chips';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { alertsConfigurationService } from '../../services/alertsConfiguration.service';
import { forkJoin, of } from 'rxjs';

interface CustomerOption {
  id: number;
  name: string;
}

interface CustomerAlertRow {
  id: number;
  customerId: number;
  customerName: string;
  alertId: number;
  frequencyType: string;
  repeatCount: number;
  executionTime: string;
  weekDays: string;
  dayOfMonth: string;
  emails: string;
  emailSubject: string;
  emailBody: string;
  _status: 'existing' | 'new' | 'edited';  // track row state
  _tempId?: number;                          // unique key for new rows
}

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
    MatTabsModule,
    MatCheckboxModule,
    MatChipsModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class EditAlertConfigurationComponent implements OnInit {
  // General tab form
  updateAlertForm: FormGroup;

  // Alert Configuration tab form
  customerAlertForm: FormGroup;

  // Dropdown data
  allCustomerOptions: CustomerOption[] = [];
  customerOptions: CustomerOption[] = [];

  // Frequency
  frequencyTypes: string[] = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
  readonly separatorKeysCodes: number[] = [ENTER, COMMA];

  // Chips
  dailyTimeChips: string[] = [];
  dayOfMonthChips: number[] = [];
  emailChips: string[] = [];

  // Grid — combined list of existing + new + edited rows
  gridRows: CustomerAlertRow[] = [];
  deletedRows: CustomerAlertRow[] = [];  // existing rows marked for deletion
  displayedColumns: string[] = ['customerName', 'frequencyType', 'emails', 'actions'];
  dataSource = new MatTableDataSource<CustomerAlertRow>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  loading = false;
  saving = false;
  isEditingAlert = false;
  editingRowIndex = -1;
  viewMode = false;
  showAlertForm = false;
  private tempIdCounter = 0;

  constructor(
    public dialogRef: MatDialogRef<EditAlertConfigurationComponent>,
    private formBuilder: FormBuilder,
    private alertsConfigService: alertsConfigurationService,
    private toast: NgToastService,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    // General tab
    this.updateAlertForm = this.formBuilder.group({
      alertId: [this.data.alertID],
      alertName: [this.data.alertName, Validators.required],
      description: [this.data.description],
      query: [this.data.query, Validators.required],
      alertType: [this.data.alertType || 'Customer'],
      emailSubject: [this.data.emailSubject],
    });

    // Alert Configuration tab
    this.customerAlertForm = this.formBuilder.group({
      customerId: [null, Validators.required],
      frequencyType: ['Minutely', Validators.required],
      repeatCount: [null],
      monday: [false],
      tuesday: [false],
      wednesday: [false],
      thursday: [false],
      friday: [false],
      saturday: [false],
      sunday: [false],
      emailSubject: [''],
    });
  }

  ngOnInit() {
    this.viewMode = this.data.viewMode === true;
    if (this.viewMode) {
      this.displayedColumns = ['customerName', 'frequencyType', 'emails'];
    }
    this.loadCustomersDropdown();
    this.loadCustomerAlerts();
  }

  get frequencyTypeValue(): string {
    return this.customerAlertForm.get('frequencyType')?.value;
  }

  get hasUnsavedChanges(): boolean {
    return this.gridRows.some(r => r._status === 'new' || r._status === 'edited') || this.deletedRows.length > 0;
  }

  getFrequencyDetail(row: CustomerAlertRow): string {
    switch (row.frequencyType) {
      case 'Minutely': return row.repeatCount ? `(Every ${row.repeatCount} min)` : '';
      case 'Hourly': return row.repeatCount ? `(Every ${row.repeatCount} hr)` : '';
      case 'Daily': return row.executionTime ? `(${row.executionTime})` : '';
      case 'Weekly':
        const parts: string[] = [];
        if (row.weekDays) parts.push(row.weekDays);
        if (row.executionTime) parts.push(row.executionTime);
        return parts.length ? `(${parts.join(' | ')})` : '';
      case 'Monthly': return row.dayOfMonth ? `(Day ${row.dayOfMonth})` : '';
      default: return '';
    }
  }

  getTruncatedEmails(emails: string): string {
    if (!emails) return '';
    const list = emails.split(',').map(e => e.trim()).filter(e => !!e);
    if (list.length <= 2) return list.join(', ');
    return list.slice(0, 2).join(', ') + ` +${list.length - 2} more`;
  }

  // --- Data Loading ---
  loadCustomersDropdown(): void {
    this.alertsConfigService.getCustomersDropdown().subscribe({
      next: (res: any) => {
        this.allCustomerOptions = res?.customers ?? [];
        this.filterCustomerDropdown();
      },
      error: (err: any) => console.error('Error loading customers', err)
    });
  }

  filterCustomerDropdown(): void {
    const usedCustomerIds = this.gridRows.map(a => a.customerId);
    this.customerOptions = this.allCustomerOptions.filter(
      c => !usedCustomerIds.includes(c.id) ||
        (this.isEditingAlert && this.editingRowIndex >= 0 && this.gridRows[this.editingRowIndex]?.customerId === c.id)
    );
  }

  loadCustomerAlerts(): void {
    this.loading = true;
    this.alertsConfigService.getCustomerAlertsByAlertId(this.data.alertID).subscribe({
      next: (res: any) => {
        const alerts = res?.alerts ?? [];
        this.gridRows = alerts.map((a: any) => ({ ...a, _status: 'existing' as const }));
        this.deletedRows = [];
        this.refreshGrid();
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Error loading customer alerts', err);
        this.gridRows = [];
        this.deletedRows = [];
        this.refreshGrid();
        this.loading = false;
      }
    });
  }

  private refreshGrid(): void {
    this.dataSource.data = this.gridRows;
    this.filterCustomerDropdown();
    setTimeout(() => {
      if (this.paginator) {
        this.dataSource.paginator = this.paginator;
      }
    }, 0);
  }

  // --- Frequency handling ---
  onFrequencyTypeChange(): void {
    this.customerAlertForm.patchValue({ repeatCount: null });
    this.dailyTimeChips = [];
    this.dayOfMonthChips = [];
    this.customerAlertForm.patchValue({
      monday: false, tuesday: false, wednesday: false,
      thursday: false, friday: false, saturday: false, sunday: false
    });
  }

  // --- Chip handlers ---
  addDailyTime(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) this.dailyTimeChips.push(value);
    event.chipInput?.clear();
  }

  removeDailyTime(index: number): void {
    if (index >= 0) this.dailyTimeChips.splice(index, 1);
  }

  addDayOfMonth(event: MatChipInputEvent): void {
    const n = Number((event.value || '').trim());
    if (!isNaN(n) && n >= 1 && n <= 31 && !this.dayOfMonthChips.includes(n)) {
      this.dayOfMonthChips.push(n);
      this.dayOfMonthChips.sort((a, b) => a - b);
    }
    event.chipInput?.clear();
  }

  removeDayOfMonth(index: number): void {
    if (index >= 0) this.dayOfMonthChips.splice(index, 1);
  }

  addEmail(event: MatChipInputEvent): void {
    const value = (event.value || '').trim().toLowerCase();
    if (value && !this.emailChips.some(e => e.toLowerCase() === value)) {
      this.emailChips.push(value);
    }
    event.chipInput?.clear();
  }

  removeEmail(index: number): void {
    if (index >= 0) this.emailChips.splice(index, 1);
  }

  // --- Build payload from form ---
  private buildPayloadFromForm(): any | null {
    if (this.customerAlertForm.invalid) {
      this.customerAlertForm.markAllAsTouched();
      return null;
    }

    const f = this.customerAlertForm.value;
    const freqType = f.frequencyType as string;
    let freqValue = '';
    let weekdaysValue = '';

    if (freqType === 'Minutely' || freqType === 'Hourly') {
      const n = Number(f.repeatCount);
      if (!n || n <= 0) {
        this.customerAlertForm.get('repeatCount')?.setErrors({ required: true });
        return null;
      }
      freqValue = n.toString();
    }

    if (freqType === 'Daily' || freqType === 'Weekly') {
      if (!this.dailyTimeChips.length) return null;
      freqValue = this.dailyTimeChips.join(',');
    }

    if (freqType === 'Weekly') {
      const days: string[] = [];
      if (f.monday) days.push('Monday');
      if (f.tuesday) days.push('Tuesday');
      if (f.wednesday) days.push('Wednesday');
      if (f.thursday) days.push('Thursday');
      if (f.friday) days.push('Friday');
      if (f.saturday) days.push('Saturday');
      if (f.sunday) days.push('Sunday');
      if (!days.length) return null;
      weekdaysValue = days.join(',');
    }

    if (freqType === 'Monthly') {
      if (!this.dayOfMonthChips.length) return null;
      freqValue = this.dayOfMonthChips.join(',');
    }

    const customerName = this.allCustomerOptions.find(c => c.id === f.customerId)?.name ?? '';

    return {
      customerId: f.customerId,
      customerName,
      alertId: this.data.alertID,
      frequencyType: freqType,
      repeatCount: freqType === 'Minutely' || freqType === 'Hourly' ? Number(freqValue) : 0,
      executionTime: freqType === 'Daily' || freqType === 'Weekly' ? freqValue : '',
      weekDays: freqType === 'Weekly' ? weekdaysValue : '',
      dayOfMonth: freqType === 'Monthly' ? freqValue : '',
      emails: this.emailChips.join(','),
      emailSubject: f.emailSubject,
      emailBody: '',
    };
  }

  // --- Add to grid (in-memory) ---
  addCustomerAlert(): void {
    const payload = this.buildPayloadFromForm();
    if (!payload) return;

    if (this.isEditingAlert && this.editingRowIndex >= 0) {
      // Update existing row in-memory
      const row = this.gridRows[this.editingRowIndex];
      Object.assign(row, payload);
      if (row._status === 'existing') {
        row._status = 'edited';
      }
      this.toast.success({ detail: "SUCCESS", summary: 'Alert updated in grid.', duration: 2000, position: 'topRight' });
    } else {
      // Add new row in-memory
      const newRow: CustomerAlertRow = {
        ...payload,
        id: 0,
        _status: 'new' as const,
        _tempId: ++this.tempIdCounter,
      };
      this.gridRows.push(newRow);
      this.toast.success({ detail: "SUCCESS", summary: 'Alert added to grid. Click Update to save.', duration: 3000, position: 'topRight' });
    }

    this.resetCustomerAlertForm();
    this.showAlertForm = false;
    this.refreshGrid();
  }

  // --- Edit row in grid (populate form) ---
  cancelAlertForm(): void {
    this.resetCustomerAlertForm();
    this.showAlertForm = false;
  }

  editCustomerAlert(row: CustomerAlertRow): void {
    this.showAlertForm = true;
    this.isEditingAlert = true;
    this.editingRowIndex = this.gridRows.indexOf(row);
    this.filterCustomerDropdown();

    this.dailyTimeChips = [];
    this.dayOfMonthChips = [];
    this.emailChips = [];

    this.customerAlertForm.patchValue({
      customerId: row.customerId,
      frequencyType: row.frequencyType,
      repeatCount: null,
      monday: false, tuesday: false, wednesday: false,
      thursday: false, friday: false, saturday: false, sunday: false,
      emailSubject: row.emailSubject,
    });

    if (row.frequencyType === 'Minutely' || row.frequencyType === 'Hourly') {
      this.customerAlertForm.patchValue({ repeatCount: row.repeatCount });
    }

    if (row.frequencyType === 'Daily' || row.frequencyType === 'Weekly') {
      this.dailyTimeChips = (row.executionTime || '').split(',').map(x => x.trim()).filter(x => !!x);
    }

    if (row.frequencyType === 'Weekly') {
      const days = (row.weekDays || '').split(',').map(x => x.trim().toLowerCase());
      this.customerAlertForm.patchValue({
        monday: days.includes('monday'),
        tuesday: days.includes('tuesday'),
        wednesday: days.includes('wednesday'),
        thursday: days.includes('thursday'),
        friday: days.includes('friday'),
        saturday: days.includes('saturday'),
        sunday: days.includes('sunday'),
      });
    }

    if (row.frequencyType === 'Monthly') {
      this.dayOfMonthChips = (row.dayOfMonth || '').split(',')
        .map(x => Number(x.trim()))
        .filter(x => !isNaN(x) && x >= 1 && x <= 31);
    }

    this.emailChips = (row.emails || '').split(',').map(e => e.trim()).filter(e => !!e);
  }

  // --- Delete row from grid (in-memory) ---
  deleteCustomerAlert(row: CustomerAlertRow): void {
    const index = this.gridRows.indexOf(row);
    if (index < 0) return;

    if (row._status === 'existing' || row._status === 'edited') {
      // Mark for deletion on save
      this.deletedRows.push(row);
    }
    // Remove from grid (new rows just disappear, existing go to deletedRows)
    this.gridRows.splice(index, 1);

    // If we were editing this row, cancel edit
    if (this.isEditingAlert && this.editingRowIndex === index) {
      this.resetCustomerAlertForm();
    } else if (this.isEditingAlert && this.editingRowIndex > index) {
      this.editingRowIndex--;
    }

    this.refreshGrid();
    this.toast.info({ detail: "INFO", summary: 'Alert removed. Click Update to save changes.', duration: 3000, position: 'topRight' });
  }

  resetCustomerAlertForm(): void {
    this.isEditingAlert = false;
    this.editingRowIndex = -1;
    this.filterCustomerDropdown();
    this.customerAlertForm.reset({
      customerId: null,
      frequencyType: 'Minutely',
      repeatCount: null,
      monday: false, tuesday: false, wednesday: false,
      thursday: false, friday: false, saturday: false, sunday: false,
      emailSubject: '',
    });
    this.dailyTimeChips = [];
    this.dayOfMonthChips = [];
    this.emailChips = [];
  }

  // --- Cancel / Close ---
  onCancel() {
    this.dialogRef.close();
  }

  // --- Save all (AlertsConfiguration + new/edited/deleted CustomerAlerts) ---
  updateAlertConfiguration(): void {
    if (!this.updateAlertForm.valid) return;

    this.saving = true;

    const alertModel = {
      alertId: this.updateAlertForm.get('alertId')?.value,
      alertName: this.updateAlertForm.get('alertName')?.value,
      description: this.updateAlertForm.get('description')?.value,
      query: this.updateAlertForm.get('query')?.value,
      alertType: this.updateAlertForm.get('alertType')?.value,
      emailSubject: this.updateAlertForm.get('emailSubject')?.value,
    };

    // Step 1: Save AlertsConfiguration
    this.alertsConfigService.updateConnector(alertModel).subscribe({
      next: (res) => {
        if (res.code !== 100 && res.code !== 200) {
          this.toast.error({ detail: "ERROR", summary: res.message || res.description, duration: 5000, position: 'topRight' });
          this.saving = false;
          return;
        }

        // Step 2: Process customer alert changes
        this.saveCustomerAlertChanges();
      },
      error: (err) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.saving = false;
      }
    });
  }

  private saveCustomerAlertChanges(): void {
    const saveOps: any[] = [];
    const deleteOps: any[] = [];

    // New rows → save
    this.gridRows.filter(r => r._status === 'new').forEach(row => {
      saveOps.push(this.alertsConfigService.saveCustomerAlert({
        id: 0,
        customerId: row.customerId,
        alertId: row.alertId,
        frequencyType: row.frequencyType,
        repeatCount: row.repeatCount,
        executionTime: row.executionTime,
        weekDays: row.weekDays,
        dayOfMonth: row.dayOfMonth,
        emails: row.emails,
        emailSubject: row.emailSubject,
        emailBody: row.emailBody,
      }));
    });

    // Edited rows → save (update)
    this.gridRows.filter(r => r._status === 'edited').forEach(row => {
      saveOps.push(this.alertsConfigService.saveCustomerAlert({
        id: row.id,
        customerId: row.customerId,
        alertId: row.alertId,
        frequencyType: row.frequencyType,
        repeatCount: row.repeatCount,
        executionTime: row.executionTime,
        weekDays: row.weekDays,
        dayOfMonth: row.dayOfMonth,
        emails: row.emails,
        emailSubject: row.emailSubject,
        emailBody: row.emailBody,
      }));
    });

    // Deleted rows → delete
    this.deletedRows.forEach(row => {
      deleteOps.push(this.alertsConfigService.deleteCustomerAlert(row));
    });

    const allOps = [...saveOps, ...deleteOps];

    if (allOps.length === 0) {
      // No customer alert changes, just close
      this.toast.success({ detail: "SUCCESS", summary: 'Alert configuration updated successfully!', duration: 3000, position: 'topRight' });
      this.saving = false;
      this.dialogRef.close('updated');
      return;
    }

    forkJoin(allOps).subscribe({
      next: () => {
        this.toast.success({ detail: "SUCCESS", summary: 'Alert configuration and customer alerts saved successfully!', duration: 3000, position: 'topRight' });
        this.saving = false;
        this.dialogRef.close('updated');
      },
      error: (err) => {
        this.toast.error({ detail: "ERROR", summary: 'Some changes may not have saved. ' + (err.message || ''), duration: 5000, position: 'topRight' });
        this.saving = false;
        // Reload to show actual DB state
        this.loadCustomerAlerts();
      }
    });
  }
}
