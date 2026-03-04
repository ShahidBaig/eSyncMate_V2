import { Component, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTable, MatTableModule } from '@angular/material/table';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'view-flow-dialog',
    templateUrl: './view-flow-dialog.component.html',
    styleUrls: ['../edit-flow-dialog/edit-flow-dialog.component.scss'], // reuse styles
    standalone: true,
    providers: [DatePipe],
    imports: [
        MatTableModule,
        DatePipe,
        ReactiveFormsModule,
        NgIf,
        MatButtonModule,
        MatIconModule,
        CommonModule,
        MatTabsModule,
        TranslateModule
    ],
})
export class ViewFlowDialogComponent implements OnInit {
    viewFlowForm: FormGroup;
    daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    detailTableColumns: string[] = ['index', 'routeName', 'status', 'inOut', 'startDate', 'endDate'];

    // Arrays for display
    detailOnDayLists: { name: string }[][] = [];
    detailExecutionTimeLists: { name: string }[][] = [];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;

    constructor(
        public dialogRef: MatDialogRef<ViewFlowDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any,
        private fb: FormBuilder,
    ) {
        this.viewFlowForm = this.fb.group({
            customerID: [{ value: data.customerID, disabled: true }],
            title: [{ value: data.title, disabled: true }],
            description: [{ value: data.description, disabled: true }],
            status: [{ value: data.status, disabled: true }],
            flowDetails: this.fb.array([])
        });
    }

    get flowDetails(): FormArray {
        return this.viewFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        if (this.data.flowDetails && this.data.flowDetails.length > 0) {
            this.data.flowDetails.forEach((detail: any) => {
                const weekDaysArr = detail.weekDays ? detail.weekDays.split(',') : [];
                const weekdayControls = this.daysOfWeek.map(day => this.fb.control({ value: weekDaysArr.includes(day), disabled: true }));

                this.detailOnDayLists.push(
                    detail.onDay && detail.onDay !== '' ? detail.onDay.split(',').map((n: string) => ({ name: n.trim() })) : []
                );
                this.detailExecutionTimeLists.push(
                    detail.executionTime && detail.executionTime !== '' ? detail.executionTime.split(',').map((n: string) => ({ name: n.trim() })) : []
                );

                const group = this.fb.group({
                    routeName: [{ value: detail.routeName || detail.routeId, disabled: true }], // Assuming routeName is provided, otherwise fallback
                    status: [{ value: detail.status, disabled: true }],
                    in_Out: [{ value: detail.in_Out, disabled: true }],
                    frequencyType: [{ value: detail.frequencyType, disabled: true }],
                    startDate: [{ value: detail.startDate ? new Date(detail.startDate) : '', disabled: true }],
                    endDate: [{ value: detail.endDate ? new Date(detail.endDate) : '', disabled: true }],
                    repeatCount: [{ value: detail.repeatCount || 0, disabled: true }],
                    selectedWeekday: this.fb.array(weekdayControls),
                    onDay: [{ value: '', disabled: true }],
                    executionTime: [{ value: '', disabled: true }],
                });
                this.flowDetails.push(group);
            });
        }
    }

    onClose(): void {
        this.dialogRef.close();
    }
}
