import { Component, Inject, OnInit, inject } from '@angular/core';
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
  allRouteOptions: any[] = [];
  daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

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
    this.allRouteOptions = data.allRouteOptions || [];

    this.detailInputForm = this.fb.group({
      routeId: [data.detail?.routeId || null],
      status: [data.detail?.status || 'Active'],
      in_Out: [{ value: data.detail?.in_Out || '', disabled: true }],
      frequencyType: [data.detail?.frequencyType || ''],
      startDate: [data.detail?.startDate || ''],
      endDate: [data.detail?.endDate || ''],
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
    // Only listen if not edit mode or if user changes routeId
    this.detailInputForm.get('routeId')?.valueChanges.subscribe((routeId) => {
      if (routeId) {
        this.onRouteSelected(routeId);
      }
    });
  }

  onRouteSelected(routeId: number) {
    const route = this.allRouteOptions.find(r => r.id === routeId);
    if (route && !this.isEdit) { // Only auto-fill from base route if adding new
      this.detailInputForm.patchValue({
        frequencyType: route.frequencyType || '',
        startDate: route.startDate ? new Date(route.startDate) : '',
        endDate: route.endDate ? new Date(route.endDate) : '',
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
            if (d.endDate) patch.endDate = new Date(d.endDate);
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
    if (value) { this.inputOnDayList.push({ name: value }); }
    event.chipInput!.clear();
  }
  removeOnDay(item: { name: string }): void {
    const idx = this.inputOnDayList.indexOf(item);
    if (idx >= 0) { this.inputOnDayList.splice(idx, 1); }
  }
  addExecutionTime(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value) { this.inputExecutionTimeList.push({ name: value }); }
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
    const formVal = this.detailInputForm.getRawValue(); // gets disabled values too
    if (!formVal.routeId) {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Route Name", duration: 3000, position: 'topRight' });
      return;
    }

    // Return all the data to parent map
    this.dialogRef.close({
      routeId: formVal.routeId,
      status: formVal.status,
      in_Out: formVal.in_Out,
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
// Trigger rebuild
