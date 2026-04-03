import { Component, inject, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTable, MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormGroup, FormControl, FormBuilder, ReactiveFormsModule, FormsModule, FormArray, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { FlowsService } from '../../services/flows.service';
import { RoutesService } from '../../services/routes.service';
import { CustomerProductCatalogService } from '../../services/customerProductCatalogDialog.service';
import { AddFlowDetailDialogComponent } from '../add-flow-detail-dialog/add-flow-detail-dialog.component';

interface Customers {
    erpCustomerID: string;
    name: string;
    id: any;
}

interface RouteOption {
    id: number;
    name: string;
    customerName?: string;
    status?: string;
    frequencyType?: string;
    startDate?: string;
    endDate?: string;
    repeatCount?: number;
    weekDays?: string;
    onDay?: string;
    executionTime?: string;
}

@Component({
    selector: 'edit-flow-dialog',
    templateUrl: './edit-flow-dialog.component.html',
    styleUrls: ['./edit-flow-dialog.component.scss'],
    standalone: true,
    providers: [DatePipe],
    imports: [
        MatTableModule,
        DatePipe,
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
        TranslateModule,
        MatDialogModule
    ],
})
export class EditFlowDialogComponent implements OnInit {
    editFlowForm: FormGroup;
    customerOptions: Customers[] | undefined;
    allRouteOptions: RouteOption[] = [];
    filteredRouteOptions: RouteOption[] = [];
    configuredRouteIds: Set<number> = new Set();
    customerSearchText: string = '';
    filteredCustomerOptions: Customers[] = [];
    frequencyTypeOptions = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
    daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

    detailOnDayLists: { name: string }[][] = [];
    detailExecutionTimeLists: { name: string }[][] = [];
    isSaving: boolean = false;

    // Table columns for the details table
    detailTableColumns: string[] = ['routeName', 'status', 'frequency', 'startDate', 'nextRecurrence', 'actions'];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;

    constructor(
        public dialogRef: MatDialogRef<EditFlowDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any,
        private fb: FormBuilder,
        private flowsApi: FlowsService,
        private routeApi: RoutesService,
        private ERPApi: CustomerProductCatalogService,
        private toast: NgToastService,
        private datePipe: DatePipe,
        public languageService: LanguageService,
        private dialog: MatDialog
    ) {
        this.editFlowForm = this.fb.group({
            id: [data.id],
            customerID: [{ value: data.customerID, disabled: true }, Validators.required],
            title: [data.title, Validators.required],
            description: [data.description],
            status: [data.status, Validators.required],
            sequenceNo: [data.sequenceNo || 0],
            flowDetails: this.fb.array([])
        });
    }

    get flowDetails(): FormArray {
        return this.editFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        this.getConfiguredRoutes();
        this.getCustomersData();
        this.getRoutes();
        this.initFlowDetails();

        // Listen for customer changes to re-filter routes
        this.editFlowForm.get('customerID')?.valueChanges.subscribe(() => {
            this.filterRoutesByCustomer();
        });
    }

    getCustomersData() {
        this.ERPApi.getERPCustomers().subscribe({
            next: (res: any) => {
                this.customerOptions = res.customers;
                this.filteredCustomerOptions = this.customerOptions ? [...this.customerOptions] : [];
                // Filter routes once customers are loaded
                this.filterRoutesByCustomer();
            },
        });
    }

    filterCustomerOptions() {
        if (!this.customerOptions) {
            this.filteredCustomerOptions = [];
            return;
        }
        const search = this.customerSearchText.trim().toLowerCase();
        if (!search) {
            this.filteredCustomerOptions = [...this.customerOptions];
        } else {
            this.filteredCustomerOptions = this.customerOptions.filter(
                c => c.name.toLowerCase().includes(search) || c.erpCustomerID.toLowerCase().includes(search)
            );
        }
    }

    onCustomerSelectOpened(opened: boolean) {
        if (opened) {
            this.customerSearchText = '';
            this.filterCustomerOptions();
        }
    }

    getRoutes() {
        this.routeApi.getRouteName().subscribe({
            next: (res: any) => {
                const raw: any[] = res.routes ?? res;
                this.allRouteOptions = raw.map((r: any) => ({
                    id: r.id,
                    name: r.name,
                    customerName: r.customerName,
                    status: r.status,
                    frequencyType: r.frequencyType,
                    startDate: r.startDate,
                    endDate: r.endDate,
                    repeatCount: r.repeatCount,
                    weekDays: r.weekDays,
                    onDay: r.onDay,
                    executionTime: r.executionTime,
                }));
                // Filter routes once routes are loaded
                this.filterRoutesByCustomer();
            },
        });
    }

    getConfiguredRoutes() {
        const flowId = this.data.id;
        this.flowsApi.getConfiguredRouteIds(flowId).subscribe({
            next: (res: any) => {
                this.configuredRouteIds = new Set(res.routeIds || []);
                this.filterRoutesByCustomer();
            },
        });
    }

    filterRoutesByCustomer() {
        const selectedCustomerID = this.editFlowForm.get('customerID')?.value;
        if (!selectedCustomerID || !this.customerOptions || !this.allRouteOptions) {
            this.filteredRouteOptions = [];
            return;
        }

        const selectedCustomer = this.customerOptions.find(c => c.erpCustomerID === selectedCustomerID);
        if (!selectedCustomer) {
            this.filteredRouteOptions = [];
            return;
        }

        // Filter by customer AND exclude routes configured in OTHER flows
        this.filteredRouteOptions = this.allRouteOptions.filter(
            route => route.customerName && route.customerName.toLowerCase() === selectedCustomer.name.toLowerCase()
                && !this.configuredRouteIds.has(route.id)
        );
    }

    initFlowDetails() {
        if (this.data.flowDetails && this.data.flowDetails.length > 0) {
            this.data.flowDetails.forEach((detail: any) => {
                const weekDaysArr = detail.weekDays ? detail.weekDays.split(',') : [];
                const weekdayControls = this.daysOfWeek.map(day => this.fb.control(weekDaysArr.includes(day)));

                this.detailOnDayLists.push(
                    detail.onDay && detail.onDay !== '' ? detail.onDay.split(',').map((n: string) => ({ name: n.trim() })) : []
                );
                this.detailExecutionTimeLists.push(
                    detail.executionTime && detail.executionTime !== '' ? detail.executionTime.split(',').map((n: string) => ({ name: n.trim() })) : []
                );

                const group = this.fb.group({
                    routeId: [detail.routeId],
                    status: [detail.status],
                    in_Out: [detail.in_Out],
                    frequencyType: [detail.frequencyType],
                    startDate: [detail.startDate ? new Date(detail.startDate) : ''],
                    endDate: [detail.endDate ? new Date(detail.endDate) : ''],
                    repeatCount: [detail.repeatCount || 0],
                    selectedWeekday: this.fb.array(weekdayControls),
                });
                this.flowDetails.push(group);
            });
        }
    }

    getAvailableRoutes(): RouteOption[] {
        const selectedCustomerID = this.editFlowForm.get('customerID')?.value;
        if (!selectedCustomerID || !this.customerOptions || !this.allRouteOptions) return [];

        const selectedCustomer = this.customerOptions.find(c => c.erpCustomerID === selectedCustomerID);
        if (!selectedCustomer) return [];

        // Routes already added in the current flow detail list
        const currentRouteIds = new Set(
            this.flowDetails.controls
                .map(ctrl => (ctrl as FormGroup).get('routeId')?.value)
                .filter(id => id != null)
        );

        return this.allRouteOptions.filter(route =>
            route.customerName && route.customerName.toLowerCase() === selectedCustomer.name.toLowerCase()
            && !this.configuredRouteIds.has(route.id)
            && !currentRouteIds.has(route.id)
        );
    }

    addDetail() {
        const customerID = this.editFlowForm.get('customerID')?.value;
        if (!customerID) {
            this.toast.warning({ detail: "WARNING", summary: "Please explicitly select a Partner ID first!", duration: 3000, position: 'topRight' });
            return;
        }

        const availableRoutes = this.getAvailableRoutes();

        const dialogRef = this.dialog.open(AddFlowDetailDialogComponent, {
            width: '800px',
            disableClose: true,
            data: {
                isEdit: false,
                customerID: customerID,
                frequencyTypeOptions: this.frequencyTypeOptions,
                filteredRouteOptions: availableRoutes,
                allRouteOptions: this.allRouteOptions,
            }
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result) {
                // Check duplicate
                const isDuplicate = this.flowDetails.controls.some(
                    ctrl => (ctrl as FormGroup).get('routeId')?.value === result.routeId
                );
                if (isDuplicate) {
                    this.toast.warning({ detail: "WARNING", summary: "This route is already added!", duration: 3000, position: 'topRight' });
                    return;
                }

                const weekdayControls = this.daysOfWeek.map((_: string, i: number) => this.fb.control(result.weekDays[i] || false));

                const group = this.fb.group({
                    routeId: [result.routeId],
                    status: [result.status],
                    in_Out: [result.in_Out || ''],
                    frequencyType: [result.frequencyType || ''],
                    startDate: [result.startDate || ''],
                    endDate: [result.endDate || ''],
                    repeatCount: [result.repeatCount || 0],
                    selectedWeekday: this.fb.array(weekdayControls),
                });
                this.flowDetails.push(group);
                this.detailOnDayLists.push([...result.onDayList]);
                this.detailExecutionTimeLists.push([...result.executionTimeList]);

                if (this.detailsTable) {
                    this.detailsTable.renderRows();
                }
            }
        });
    }

    editDetail(index: number) {
        const customerID = this.editFlowForm.get('customerID')?.value;
        const detailGroup = this.flowDetails.at(index) as FormGroup;

        // Reconstruct weekdays array for mapping back to editing dialog
        const detailWeekdays = detailGroup.get('selectedWeekday') as FormArray;
        const mappedWeekdays = this.daysOfWeek.map((_, i) => detailWeekdays.at(i).value);

        const dialogRef = this.dialog.open(AddFlowDetailDialogComponent, {
            width: '800px',
            disableClose: true,
            data: {
                isEdit: true,
                customerID: customerID,
                frequencyTypeOptions: this.frequencyTypeOptions,
                filteredRouteOptions: this.filteredRouteOptions,
                allRouteOptions: this.allRouteOptions,
                detail: {
                    routeId: detailGroup.get('routeId')?.value,
                    status: detailGroup.get('status')?.value,
                    in_Out: detailGroup.get('in_Out')?.value,
                    frequencyType: detailGroup.get('frequencyType')?.value,
                    startDate: detailGroup.get('startDate')?.value,
                    endDate: detailGroup.get('endDate')?.value,
                    repeatCount: detailGroup.get('repeatCount')?.value,
                    weekDays: mappedWeekdays,
                    onDayList: [...this.detailOnDayLists[index]],
                    executionTimeList: [...this.detailExecutionTimeLists[index]]
                }
            }
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result) {
                // Check if route ID changed and duplicates another existing row
                const isDuplicate = this.flowDetails.controls.some(
                    (ctrl, i) => i !== index && (ctrl as FormGroup).get('routeId')?.value === result.routeId
                );
                if (isDuplicate) {
                    this.toast.warning({ detail: "WARNING", summary: "This route is already added in another row!", duration: 3000, position: 'topRight' });
                    return;
                }

                detailGroup.patchValue({
                    routeId: result.routeId,
                    status: result.status,
                    in_Out: result.in_Out || '',
                    frequencyType: result.frequencyType || '',
                    startDate: result.startDate || '',
                    endDate: result.endDate || '',
                    repeatCount: result.repeatCount || 0,
                });

                const existingWeekdayArray = detailGroup.get('selectedWeekday') as FormArray;
                this.daysOfWeek.forEach((_: string, i: number) => {
                    existingWeekdayArray.at(i).setValue(result.weekDays[i] || false);
                });

                this.detailOnDayLists[index] = [...result.onDayList];
                this.detailExecutionTimeLists[index] = [...result.executionTimeList];

                if (this.detailsTable) {
                    this.detailsTable.renderRows();
                }
            }
        });
    }

    removeFlowDetail(index: number) {
        this.flowDetails.removeAt(index);
        this.detailOnDayLists.splice(index, 1);
        this.detailExecutionTimeLists.splice(index, 1);
        if (this.detailsTable) {
            this.detailsTable.renderRows();
        }
    }

    getWeekdayCtrl(di: number, wi: number): FormControl | null {
        const arr = (this.flowDetails.at(di) as FormGroup).get('selectedWeekday') as FormArray;
        return arr ? arr.at(wi) as FormControl : null;
    }

    // Helper to resolve a route ID to its name for display in the table
    getRouteNameById(routeId: number): string {
        if (!this.allRouteOptions) return routeId?.toString() || '';
        const route = this.allRouteOptions.find(r => r.id === routeId);
        return route ? route.name : (routeId?.toString() || '');
    }

    onFlowStatusChange(status: string) {
        this.flowDetails.controls.forEach((ctrl) => {
            (ctrl as FormGroup).get('status')?.setValue(status);
        });
        if (this.detailsTable) {
            this.detailsTable.renderRows();
        }
    }

    getFrequencyDisplay(index: number): string {
        const detail = this.flowDetails.at(index) as FormGroup;
        const freqType = detail.get('frequencyType')?.value || '';
        const repeatCount = detail.get('repeatCount')?.value || 0;
        const weekDaysArr = detail.get('selectedWeekday') as FormArray;
        const onDayList = this.detailOnDayLists[index] || [];
        const execTimeList = this.detailExecutionTimeLists[index] || [];

        switch (freqType) {
            case 'Minutely':
                return `Every ${repeatCount} min${repeatCount > 1 ? 's' : ''}`;
            case 'Hourly':
                return `Every ${repeatCount} hr${repeatCount > 1 ? 's' : ''}`;
            case 'Daily': {
                if (execTimeList.length > 0) return `Daily at ${execTimeList.map(t => t.name).join(', ')}`;
                return 'Daily';
            }
            case 'Weekly': {
                const days = this.daysOfWeek.filter((_, i) => weekDaysArr?.at(i)?.value).map(d => d.substring(0, 3));
                let display = `Weekly on ${days.join(', ')}`;
                if (execTimeList.length > 0) display += ` at ${execTimeList.map(t => t.name).join(', ')}`;
                return display;
            }
            case 'Monthly': {
                let display = onDayList.length > 0 ? `Monthly on day${onDayList.length > 1 ? 's' : ''} ${onDayList.map(d => d.name).join(', ')}` : 'Monthly';
                if (execTimeList.length > 0) display += ` at ${execTimeList.map(t => t.name).join(', ')}`;
                return display;
            }
            default:
                return freqType || 'Not configured';
        }
    }

    getNextRecurrence(index: number): string {
        const detail = this.flowDetails.at(index) as FormGroup;
        if (detail.get('status')?.value !== 'Active') return 'Inactive';

        const freqType = detail.get('frequencyType')?.value || '';
        const repeatCount = detail.get('repeatCount')?.value || 0;
        const weekDaysArr = detail.get('selectedWeekday') as FormArray;
        const onDayList = this.detailOnDayLists[index] || [];
        const execTimeList = this.detailExecutionTimeLists[index] || [];
        const now = new Date();
        let nextDate: Date | null = null;

        try {
            switch (freqType) {
                case 'Minutely': {
                    if (repeatCount <= 0) break;
                    const next = new Date(now);
                    const nextMin = Math.ceil((next.getMinutes() + 1) / repeatCount) * repeatCount;
                    next.setMinutes(nextMin, 0, 0);
                    if (next <= now) next.setMinutes(next.getMinutes() + repeatCount);
                    nextDate = next;
                    break;
                }
                case 'Hourly': {
                    if (repeatCount <= 0) break;
                    const next = new Date(now);
                    const nextHr = Math.ceil((next.getHours() + 1) / repeatCount) * repeatCount;
                    next.setHours(nextHr, 0, 0, 0);
                    if (next <= now) next.setHours(next.getHours() + repeatCount);
                    nextDate = next;
                    break;
                }
                case 'Daily': {
                    if (execTimeList.length > 0) {
                        for (let d = 0; d <= 1; d++) {
                            const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() + d);
                            const result = this.findNextExecTimeForDate(date, now, execTimeList);
                            if (result) { nextDate = result; break; }
                        }
                    } else {
                        const next = new Date(now);
                        next.setDate(next.getDate() + 1);
                        next.setHours(0, 0, 0, 0);
                        nextDate = next;
                    }
                    break;
                }
                case 'Weekly': {
                    const activeDays: number[] = [];
                    const dayMap = [1, 2, 3, 4, 5, 6, 0];
                    this.daysOfWeek.forEach((_, i) => { if (weekDaysArr?.at(i)?.value) activeDays.push(dayMap[i]); });
                    if (activeDays.length === 0) break;
                    for (let d = 0; d <= 7; d++) {
                        const candidate = new Date(now); candidate.setDate(candidate.getDate() + d);
                        if (activeDays.includes(candidate.getDay())) {
                            if (execTimeList.length > 0) {
                                const result = this.findNextExecTimeForDate(candidate, now, execTimeList);
                                if (result) { nextDate = result; break; }
                            } else if (d > 0) {
                                candidate.setHours(0, 0, 0, 0); nextDate = candidate; break;
                            }
                        }
                    }
                    break;
                }
                case 'Monthly': {
                    if (onDayList.length === 0) break;
                    const days = onDayList.map(d => parseInt(d.name, 10)).filter(d => !isNaN(d)).sort((a, b) => a - b);
                    for (let m = 0; m <= 1; m++) {
                        const monthDate = new Date(now.getFullYear(), now.getMonth() + m, 1);
                        for (const day of days) {
                            const candidate = new Date(monthDate.getFullYear(), monthDate.getMonth(), day);
                            if (candidate.getMonth() !== monthDate.getMonth()) continue;
                            if (execTimeList.length > 0) {
                                const result = this.findNextExecTimeForDate(candidate, now, execTimeList);
                                if (result) { nextDate = result; break; }
                            } else { candidate.setHours(0, 0, 0, 0); if (candidate > now) { nextDate = candidate; break; } }
                        }
                        if (nextDate) break;
                    }
                    break;
                }
            }
        } catch (e) { return 'N/A'; }

        if (!nextDate) return 'N/A';
        return this.datePipe.transform(nextDate, 'MMM dd, yyyy hh:mm a') || 'N/A';
    }

    private findNextExecTimeForDate(date: Date, now: Date, execTimeList: { name: string }[]): Date | null {
        const times = execTimeList
            .map(t => { const p = t.name.split(':'); return p.length === 2 ? { h: parseInt(p[0], 10), m: parseInt(p[1], 10) } : null; })
            .filter(t => t !== null)
            .sort((a, b) => a!.h * 60 + a!.m - (b!.h * 60 + b!.m));
        for (const time of times) {
            const candidate = new Date(date.getFullYear(), date.getMonth(), date.getDate(), time!.h, time!.m, 0, 0);
            if (candidate > now) return candidate;
        }
        return null;
    }

    isValidDate(value: any): boolean {
        if (!value) return false;
        const date = new Date(value);
        return !isNaN(date.getTime()) && date.getFullYear() > 1900;
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onSave(): void {
        const customerID = this.editFlowForm.getRawValue().customerID;
        const title = this.editFlowForm.get('title')?.value;

        if (!customerID || customerID.toString().trim() === '') {
            this.editFlowForm.markAllAsTouched();
            this.toast.warning({ detail: "WARNING", summary: "Customer ID is required", duration: 3000, position: 'topRight' });
            return;
        }
        if (!title || title.trim() === '') {
            this.editFlowForm.markAllAsTouched();
            this.toast.warning({ detail: "WARNING", summary: "Title is required", duration: 3000, position: 'topRight' });
            return;
        }

        const status = this.editFlowForm.get('status')?.value;
        if (!status || status.trim() === '') {
            this.editFlowForm.markAllAsTouched();
            this.toast.warning({ detail: "WARNING", summary: "Status is required", duration: 3000, position: 'topRight' });
            return;
        }

        const details = this.flowDetails.controls.map((ctrl, i) => {
            const d = ctrl as FormGroup;
            const weekDays = this.daysOfWeek.filter((_, wi) => this.getWeekdayCtrl(i, wi)?.value).join(',');
            const onDay = this.detailOnDayLists[i].map(x => x.name).join(',');
            const execTime = this.detailExecutionTimeLists[i].map(x => x.name).join(',');
            return {
                routeId: d.get('routeId')?.value,
                status: d.get('status')?.value,
                in_Out: d.get('in_Out')?.value,
                frequencyType: d.get('frequencyType')?.value,
                startDate: d.get('startDate')?.value ? new Date(d.get('startDate')?.value).toISOString() : '1900-01-01T00:00:00',
                endDate: d.get('endDate')?.value ? new Date(d.get('endDate')?.value).toISOString() : '1900-01-01T00:00:00',
                repeatCount: d.get('repeatCount')?.value || 0,
                weekDays: weekDays,
                onDay: onDay,
                executionTime: execTime,
            };
        });

        const flowModel = {
            id: this.editFlowForm.get('id')?.value,
            customerID: this.editFlowForm.getRawValue().customerID,
            title: this.editFlowForm.get('title')?.value,
            description: this.editFlowForm.get('description')?.value,
            status: details.some(d => d.status === 'Active') ? 'Active' : (details.length > 0 ? 'In-Active' : 'Active'),
            sequenceNo: this.editFlowForm.get('sequenceNo')?.value || 0,
            flowDetails: details
        };

        this.isSaving = true;
        this.flowsApi.updateFlow(flowModel).subscribe({
            next: (res) => {
                this.isSaving = false;
                if (res.code === 100) {
                    this.toast.success({ detail: "SUCCESS", summary: res.description || res.message || 'Flow updated!', duration: 5000, position: 'topRight' });
                    this.dialogRef.close('updated');
                } else {
                    this.toast.error({ detail: "ERROR", summary: res.description || res.message || 'Failed to update flow', duration: 5000, position: 'topRight' });
                }
            },
            error: (err) => {
                this.isSaving = false;
                this.toast.error({ detail: "ERROR", summary: err.error?.message || err.message || 'Server error', duration: 5000, position: 'topRight' });
            }
        });
    }
}

