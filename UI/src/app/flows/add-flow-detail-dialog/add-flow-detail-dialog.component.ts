import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, FormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule, MatChipInputEvent } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { NgToastService } from 'ng-angular-popup';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { FlowsService } from '../../services/flows.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'add-flow-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule,
    MatButtonModule,
    MatDatepickerModule,
    TranslateModule
  ],
  templateUrl: './add-flow-detail-dialog.component.html',
  styleUrls: ['./add-flow-detail-dialog.component.scss']
})
export class AddFlowDetailDialogComponent implements OnInit {
  detailInputForm: FormGroup;
  frequencyTypeOptions: string[] = [];
  filteredRouteOptions: any[] = [];
  displayedRouteOptions: any[] = [];
  allRouteOptions: any[] = [];
  daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  routeSearchText: string = '';

  readonly separatorKeysCodes = [ENTER, COMMA] as const;

  inputOnDayList: { name: string }[] = [];
  inputExecutionTimeList: { name: string }[] = [];

  isEdit: boolean = false;

  constructor(
    public dialogRef: MatDialogRef<AddFlowDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private flowsApi: FlowsService,
    private toast: NgToastService
  ) {
    this.isEdit = data.isEdit;
    this.frequencyTypeOptions = data.frequencyTypeOptions || ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
    this.filteredRouteOptions = data.filteredRouteOptions || [];
    this.displayedRouteOptions = [...this.filteredRouteOptions];
    this.allRouteOptions = data.allRouteOptions || [];

    // Default end date: 5 years from now
    const defaultEndDate = new Date();
    defaultEndDate.setFullYear(defaultEndDate.getFullYear() + 5);

    this.detailInputForm = this.fb.group({
      routeId: [data.detail?.routeId || null],
      status: [data.detail?.status || 'Active'],
      frequencyType: [data.detail?.frequencyType || ''],
      startDate: [data.detail?.startDate || ''],
      endDate: [data.detail?.endDate || defaultEndDate],
      repeatCount: [data.detail?.repeatCount || 0],
      selectedWeekday: this.fb.array(this.daysOfWeek.map(() => this.fb.control(false))),
      onDay: [''],
      executionTime: [''],
    });

    if (data.detail) {
      // Set chips
      if (data.detail.onDayList) {
        this.inputOnDayList = [...data.detail.onDayList];
      }
      if (data.detail.executionTimeList) {
        this.inputExecutionTimeList = [...data.detail.executionTimeList];
      }
      // Set weekdays
      const weekDaysArr = data.detail.weekDays || [];
      const weekdayFormArray = this.detailInputForm.get('selectedWeekday') as FormArray;
      this.daysOfWeek.forEach((day, i) => {
        weekdayFormArray.at(i).setValue(weekDaysArr[i] || false);
      });
    }
  }

  ngOnInit(): void {
    this.detailInputForm.get('routeId')?.valueChanges.subscribe((routeId) => {
      if (routeId) {
        this.onRouteSelected(routeId);
      }
    });
  }

  // Route search methods
  filterRouteOptions() {
    const search = this.routeSearchText.trim().toLowerCase();
    if (!search) {
      this.displayedRouteOptions = [...this.filteredRouteOptions];
    } else {
      this.displayedRouteOptions = this.filteredRouteOptions.filter(
        (r: any) => r.name?.toLowerCase().includes(search)
      );
    }
  }

  onRouteSelectOpened(opened: boolean) {
    if (opened) {
      this.routeSearchText = '';
      this.filterRouteOptions();
    }
  }

  onRouteSelected(routeId: number) {
    const route = this.allRouteOptions.find(r => r.id === routeId);

    // Default end date: 5 years from now
    const defaultEndDate = new Date();
    defaultEndDate.setFullYear(defaultEndDate.getFullYear() + 5);

    if (route && !this.isEdit) {
      this.detailInputForm.patchValue({
        frequencyType: route.frequencyType || '',
        startDate: route.startDate ? new Date(route.startDate) : '',
        endDate: route.endDate ? new Date(route.endDate) : defaultEndDate,
        repeatCount: route.repeatCount ?? 0,
      });

      const weekDaysArr = route.weekDays ? route.weekDays.split(',').map((s: string) => s.trim()) : [];
      const weekdayFormArray = this.detailInputForm.get('selectedWeekday') as FormArray;
      this.daysOfWeek.forEach((day, i) => {
        weekdayFormArray.at(i).setValue(weekDaysArr.includes(day));
      });

      this.inputOnDayList = route.onDay && route.onDay !== ''
        ? route.onDay.split(',').map((n: string) => ({ name: n.trim() }))
        : [];

      this.inputExecutionTimeList = route.executionTime && route.executionTime !== ''
        ? route.executionTime.split(',').map((n: string) => ({ name: n.trim() }))
        : [];
    }

    // Call API for existing-flow autofill
    const customerId = this.data.customerID;
    if (customerId) {
      this.flowsApi.getAutofillByRouteId(customerId, routeId).subscribe({
        next: (res: any) => {
          if (res.data) {
            const d = res.data;
            const patch: any = {};
            if (d.frequencyType) patch.frequencyType = d.frequencyType;
            if (d.startDate) patch.startDate = new Date(d.startDate);
            patch.endDate = d.endDate ? new Date(d.endDate) : defaultEndDate;
            if (d.repeatCount) patch.repeatCount = parseInt(d.repeatCount, 10);
            if (Object.keys(patch).length > 0) {
              this.detailInputForm.patchValue(patch, { emitEvent: false });
            }

            if (d.weekDays) {
              const weekDaysArr = d.weekDays.split(',').map((s: string) => s.trim());
              const weekdayFormArray = this.detailInputForm.get('selectedWeekday') as FormArray;
              this.daysOfWeek.forEach((day, i) => {
                weekdayFormArray.at(i).setValue(weekDaysArr.includes(day));
              });
            }

            if (d.onDay && d.onDay !== '') {
              this.inputOnDayList = d.onDay.split(',').map((n: string) => ({ name: n.trim() }));
            }

            if (d.executionTime && d.executionTime !== '') {
              this.inputExecutionTimeList = d.executionTime.split(',').map((n: string) => ({ name: n.trim() }));
            }
          }
        }
      });
    }
  }

  showInputDaysOfWeek(): boolean {
    return this.detailInputForm.get('frequencyType')?.value === 'Weekly';
  }
  showInputExecutionTime(): boolean {
    const v = this.detailInputForm.get('frequencyType')?.value;
    return v === 'Daily' || v === 'Weekly';
  }
  showInputRepeatCount(): boolean {
    const v = this.detailInputForm.get('frequencyType')?.value;
    return v === 'Minutely' || v === 'Hourly';
  }
  showInputOnDayInput(): boolean {
    return this.detailInputForm.get('frequencyType')?.value === 'Monthly';
  }
  getInputWeekdayCtrl(wi: number): FormControl {
    const arr = this.detailInputForm.get('selectedWeekday') as FormArray;
    return arr.at(wi) as FormControl;
  }

  addOnDay(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) {
      const day = parseInt(value, 10);
      if (isNaN(day) || day < 1 || day > 31) {
        this.toast.warning({ detail: "WARNING", summary: "Day must be between 1 and 31", duration: 3000, position: 'topRight' });
        event.chipInput!.clear();
        return;
      }
      if (this.inputOnDayList.some(d => d.name === value)) {
        event.chipInput!.clear();
        return;
      }
      this.inputOnDayList.push({ name: value });
    }
    event.chipInput!.clear();
  }
  removeOnDay(item: { name: string }): void {
    const idx = this.inputOnDayList.indexOf(item);
    if (idx >= 0) { this.inputOnDayList.splice(idx, 1); }
  }
  addExecutionTime(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) {
      const timeRegex = /^([0-1][0-9]|2[0-3]):[0-5][0-9]$/;
      if (!timeRegex.test(value)) {
        this.toast.warning({ detail: "WARNING", summary: "Time must be in HH:MM format (00:00 - 23:59)", duration: 3000, position: 'topRight' });
        event.chipInput!.clear();
        return;
      }
      if (this.inputExecutionTimeList.some(t => t.name === value)) {
        event.chipInput!.clear();
        return;
      }
      this.inputExecutionTimeList.push({ name: value });
    }
    event.chipInput!.clear();
  }
  removeExecutionTime(item: { name: string }): void {
    const idx = this.inputExecutionTimeList.indexOf(item);
    if (idx >= 0) { this.inputExecutionTimeList.splice(idx, 1); }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    const formVal = this.detailInputForm.getRawValue();
    if (!formVal.routeId) {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Route Name", duration: 3000, position: 'topRight' });
      return;
    }
    if (!formVal.frequencyType) {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Frequency Type", duration: 3000, position: 'topRight' });
      return;
    }
    if ((formVal.frequencyType === 'Minutely' || formVal.frequencyType === 'Hourly') && (!formVal.repeatCount || formVal.repeatCount <= 0)) {
      this.toast.warning({ detail: "WARNING", summary: "Repeat Count must be greater than 0", duration: 3000, position: 'topRight' });
      return;
    }
    if ((formVal.frequencyType === 'Daily' || formVal.frequencyType === 'Weekly') && this.inputExecutionTimeList.length === 0) {
      this.toast.warning({ detail: "WARNING", summary: "Please add at least one Execution Time", duration: 3000, position: 'topRight' });
      return;
    }
    if (formVal.frequencyType === 'Weekly') {
      const hasDay = formVal.selectedWeekday.some((v: boolean) => v);
      if (!hasDay) {
        this.toast.warning({ detail: "WARNING", summary: "Please select at least one day of the week", duration: 3000, position: 'topRight' });
        return;
      }
    }
    if (formVal.frequencyType === 'Monthly' && this.inputOnDayList.length === 0) {
      this.toast.warning({ detail: "WARNING", summary: "Please add at least one day of the month", duration: 3000, position: 'topRight' });
      return;
    }

    this.dialogRef.close({
      routeId: formVal.routeId,
      status: formVal.status,
      in_Out: '',
      frequencyType: formVal.frequencyType,
      startDate: formVal.startDate,
      endDate: formVal.endDate,
      repeatCount: formVal.repeatCount,
      weekDays: formVal.selectedWeekday,
      onDayList: [...this.inputOnDayList],
      executionTimeList: [...this.inputExecutionTimeList]
    });
  }
}
