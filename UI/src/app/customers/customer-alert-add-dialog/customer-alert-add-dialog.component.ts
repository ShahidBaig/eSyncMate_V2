import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule, MatChipInputEvent } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';

import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { CustomersService } from '../../services/customers.service';

interface AlertOption {
  alertID: number;
  alertName: string;
  emailSubject?: string;
  emailBody?: string;
}

@Component({
  selector: 'app-customer-alert-add-dialog',
  standalone: true,
  templateUrl: './customer-alert-add-dialog.component.html',
  styleUrls: ['./customer-alert-add-dialog.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule
  ]
})
export class CustomerAlertAddDialogComponent {

  form: FormGroup;

  frequencyTypes: string[] = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];

  readonly separatorKeysCodes: number[] = [ENTER, COMMA];

  dailyTimeChips: string[] = [];   // Daily times
  dayOfMonthChips: number[] = [];  // Monthly days
  emailChips: string[] = [];       // Email list
  alertOption: AlertOption[] = [];        // pehle shayad: alertOption?: AlertOption[];
  filteredAlertOption: AlertOption[] = []; // ye bhi initialize kar do

  isEdit: boolean = false;
  constructor(
    @Inject(MAT_DIALOG_DATA)
    public data: { customerId: number; customerName: string; alert: any | null, isEdit: boolean, usedAlertIds?: number[]; },
    private dialogRef: MatDialogRef<CustomerAlertAddDialogComponent>,
    private fb: FormBuilder,
    private customersService: CustomersService
  ) {
    this.form = this.fb.group({
      id: [data.alert?.id ?? 0],
      alertId: [data.alert?.alertId ?? null, Validators.required],
      frequencyType: [data.alert?.frequencyType ?? 'Minutely', Validators.required],
      repeatCount: [null],
      monday: [false],
      tuesday: [false],
      wednesday: [false],
      thursday: [false],
      friday: [false],
      saturday: [false],
      sunday: [false],
      emailSubject: [data.alert?.emailSubject ?? ''],
      emailBody: [data.alert?.emailBody ?? ''],
    });
    this.isEdit = data.isEdit;
    //if (this.isEdit) {
    //  this.form.get('alertId')?.disable();
    //}
    this.getAlertConfiguration();
    if (data.alert) {
      this.populateFromExisting(data.alert);
    }
  }

  get frequencyTypeValue(): string {
    return this.form.get('frequencyType')?.value;
  }

  getAlertName(id: number | null | undefined): string {
    if (id == null) return '';
    const list = this.alertOption ?? [];
    const found = list.find(a => a.alertID === id);
    return found ? found.alertName : '';
  }
  // ---------- populate when editing -----------
  private populateFromExisting(a: any): void {
    if (a.frequencyType === 'Minutely' || a.frequencyType === 'Hourly') {
      const n = Number(a.repeatCount ?? a.value);
      if (!isNaN(n)) {
        this.form.patchValue({ repeatCount: n });
      }
    }

    if (a.frequencyType === 'Daily' || a.frequencyType === 'Weekly') {
      this.dailyTimeChips = (a.executionTime || a.value || '')
        .split(',')
        .map((x: string) => x.trim())
        .filter((x: string) => !!x);
    }

    if (a.frequencyType === 'Weekly') {
      const days = (a.weekDays || a.value || '')
        .split(',')
        .map((x: string) => x.trim().toLowerCase());

      this.form.patchValue({
        monday: days.includes('monday'),
        tuesday: days.includes('tuesday'),
        wednesday: days.includes('wednesday'),
        thursday: days.includes('thursday'),
        friday: days.includes('friday'),
        saturday: days.includes('saturday'),
        sunday: days.includes('sunday')
      });
    }

    if (a.frequencyType === 'Monthly') {
      this.dayOfMonthChips = (a.dayOfMonth || a.value || '')
        .split(',')
        .map((x: string) => Number(x.trim()))
        .filter((x: number) => !isNaN(x) && x >= 1 && x <= 31);
    }

    if (a.emails) {
      this.emailChips = (a.emails || '')
        .split(',')
        .map((e: string) => e.trim())
        .filter((e: string) => !!e);
    }
  }

  onFrequencyTypeChange(): void {
    this.form.patchValue({ repeatCount: null });
    this.dailyTimeChips = [];
    this.dayOfMonthChips = [];
    this.form.patchValue({
      monday: false,
      tuesday: false,
      wednesday: false,
      thursday: false,
      friday: false,
      saturday: false,
      sunday: false
    });
  }

  private bindAlertAutoFill(): void {
    this.form.get('alertId')?.valueChanges.subscribe((alertId: number) => {
      const selected = this.alertOption?.find(a => a.alertID === alertId);
      if (!selected) return;

      this.form.patchValue({
        emailSubject: selected.emailSubject ?? selected.alertName ?? '',
        emailBody: selected.emailBody ?? ''
      }, { emitEvent: false });
    });
  }

  // ---------- chips handlers ----------
  addDailyTime(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) {
      this.dailyTimeChips.push(value);
    }
    event.chipInput?.clear();
  }

  removeDailyTime(index: number): void {
    if (index >= 0) {
      this.dailyTimeChips.splice(index, 1);
    }
  }

  addDayOfMonth(event: MatChipInputEvent): void {
    const raw = (event.value || '').trim();
    const n = Number(raw);

    if (!isNaN(n) && n >= 1 && n <= 31 && !this.dayOfMonthChips.includes(n)) {
      this.dayOfMonthChips.push(n);
      this.dayOfMonthChips.sort((a, b) => a - b);
    }
    event.chipInput?.clear();
  }

  removeDayOfMonth(index: number): void {
    if (index >= 0) {
      this.dayOfMonthChips.splice(index, 1);
    }
  }

  addEmail(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) {
      this.emailChips.push(value);
    }
    event.chipInput?.clear();
  }

  removeEmail(index: number): void {
    if (index >= 0) {
      this.emailChips.splice(index, 1);
    }
  }

  // ---------- save / cancel ----------
  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const f = this.form.value;
    const freqType = f.frequencyType as string;
    let freqValue = '';
    let weekdaysValue = '';


    if (freqType === 'Minutely' || freqType === 'Hourly') {
      const n = Number(f.repeatCount);
      if (!n || n <= 0) {
        this.form.get('repeatCount')?.setErrors({ required: true });
        return;
      }
      freqValue = n.toString();
    }

    if (freqType === 'Daily' || freqType === 'Weekly') {
      if (!this.dailyTimeChips.length) { return; }
      freqValue = this.dailyTimeChips.join(',');
    }

    if (freqType === 'Weekly') {
      const days: string[] = [];
      if (f.monday) { days.push('Monday'); }
      if (f.tuesday) { days.push('Tuesday'); }
      if (f.wednesday) { days.push('Wednesday'); }
      if (f.thursday) { days.push('Thursday'); }
      if (f.friday) { days.push('Friday'); }
      if (f.saturday) { days.push('Saturday'); }
      if (f.sunday) { days.push('Sunday'); }
      if (!days.length) { return; }
      weekdaysValue = days.join(',');
    }

    if (freqType === 'Monthly') {
      if (!this.dayOfMonthChips.length) { return; }
      freqValue = this.dayOfMonthChips.join(',');
    }

    const emails = this.emailChips.join(',');

    const payload = {
      id: f.id ?? 0,
      customerId: this.data.customerId,
      alertId: f.alertId,
      frequencyType: freqType,
      repeatCount: freqType === 'Minutely' || freqType === 'Hourly' ? Number(freqValue) : 0,
      executionTime: freqType === 'Daily' || freqType === 'Weekly' ? freqValue : "",
      weekDays: freqType === 'Weekly' ? weekdaysValue : "",
      dayOfMonth: freqType === 'Monthly' ? freqValue : "",
      emails: emails,
      emailSubject: f.emailSubject,
      emailBody: f.emailBody ,

    };

    this.customersService.saveCustomerAlert(payload).subscribe({
      next: () => this.dialogRef.close('saved'),
      error: err => console.error('Error saving customer alert', err)
    });
  }

  getAlertConfiguration(): void {
    this.customersService.getAlertConfigurations().subscribe({
      next: (res: any) => {
        this.alertOption = res?.alertsConfiguration ?? [];
        this.filteredAlertOption = this.filterAlertsForDropdown(this.alertOption);

        this.bindAlertAutoFill(); // ✅ add this line
      },
      error: err => {
        console.error('Error loading alert configuration', err);
        this.alertOption = [];
        this.filteredAlertOption = [];
      }
    });
  }


  private filterAlertsForDropdown(all: AlertOption[]): AlertOption[] {
    if (!Array.isArray(all)) {
      return [];
    }

    // Edit mode me current alert ko allow karna hai
    if (this.data.alert) {
      return all;
    }

    const used = this.data.usedAlertIds ?? [];

    return all.filter(a => !used.includes(a.alertID));
  }

}
