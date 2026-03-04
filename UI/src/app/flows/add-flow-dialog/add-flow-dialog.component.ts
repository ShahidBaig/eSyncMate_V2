import { Component, inject, OnInit, ViewChild } from '@angular/core';
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
import { MatDialogRef, MatDialog, MatDialogModule } from '@angular/material/dialog';
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
    selector: 'add-flow-dialog',
    templateUrl: './add-flow-dialog.component.html',
    styleUrls: ['./add-flow-dialog.component.scss'],
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
export class AddFlowDialogComponent implements OnInit {
    newFlowForm: FormGroup;
    customerOptions: Customers[] | undefined;
    allRouteOptions: RouteOption[] = [];
    filteredRouteOptions: RouteOption[] = [];
    frequencyTypeOptions = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
    daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

    detailOnDayLists: { name: string }[][] = [];
    detailExecutionTimeLists: { name: string }[][] = [];

    // Table columns for the details table
    detailTableColumns: string[] = ['index', 'routeName', 'status', 'inOut', 'startDate', 'endDate', 'actions'];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;

    constructor(
        public dialogRef: MatDialogRef<AddFlowDialogComponent>,
        private fb: FormBuilder,
        private flowsApi: FlowsService,
        private routeApi: RoutesService,
        private ERPApi: CustomerProductCatalogService,
        private toast: NgToastService,
        private datePipe: DatePipe,
        public languageService: LanguageService,
        private dialog: MatDialog
    ) {
        this.newFlowForm = this.fb.group({
            customerID: ['', Validators.required],
            title: ['', Validators.required],
            description: [''],
            status: ['Active'],
            flowDetails: this.fb.array([])
        });
    }

    get flowDetails(): FormArray {
        return this.newFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        this.getCustomersData();
        this.getRoutes();

        // Listen for customer changes to re-filter routes
        this.newFlowForm.get('customerID')?.valueChanges.subscribe(() => {
            this.filterRoutesByCustomer();
        });
    }

    getCustomersData() {
        this.ERPApi.getERPCustomers().subscribe({
            next: (res: any) => {
                this.customerOptions = res.customers;
                this.filterRoutesByCustomer();
            },
        });
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
                this.filterRoutesByCustomer();
            },
        });
    }

    filterRoutesByCustomer() {
        const selectedCustomerID = this.newFlowForm.get('customerID')?.value;
        if (!selectedCustomerID || !this.customerOptions || !this.allRouteOptions) {
            this.filteredRouteOptions = [];
            return;
        }

        const selectedCustomer = this.customerOptions.find(c => c.erpCustomerID === selectedCustomerID);
        if (!selectedCustomer) {
            this.filteredRouteOptions = [];
            return;
        }

        this.filteredRouteOptions = this.allRouteOptions.filter(
            route => route.customerName && route.customerName.toLowerCase() === selectedCustomer.name.toLowerCase()
        );
    }

    addDetail() {
        const customerID = this.newFlowForm.get('customerID')?.value;
        if (!customerID) {
            this.toast.warning({ detail: "WARNING", summary: "Please explicitly select a Partner ID first!", duration: 3000, position: 'topRight' });
            return;
        }

        const dialogRef = this.dialog.open(AddFlowDetailDialogComponent, {
            width: '800px',
            disableClose: true,
            data: {
                isEdit: false,
                customerID: customerID,
                frequencyTypeOptions: this.frequencyTypeOptions,
                filteredRouteOptions: this.filteredRouteOptions,
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

    getRouteNameById(routeId: number): string {
        if (!this.allRouteOptions) return routeId?.toString() || '';
        const route = this.allRouteOptions.find(r => r.id === routeId);
        return route ? route.name : (routeId?.toString() || '');
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onSave(): void {
        const customerID = this.newFlowForm.get('customerID')?.value;
        const title = this.newFlowForm.get('title')?.value;

        if (!customerID || customerID.toString().trim() === '') {
            this.newFlowForm.markAllAsTouched();
            this.toast.warning({ detail: "WARNING", summary: "Partner ID is required", duration: 3000, position: 'topRight' });
            return;
        }
        if (title.trim() === '') {
            this.newFlowForm.markAllAsTouched();
            this.toast.warning({ detail: "WARNING", summary: "Flow Name is required", duration: 3000, position: 'topRight' });
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
            customerID: this.newFlowForm.get('customerID')?.value,
            title: this.newFlowForm.get('title')?.value,
            description: this.newFlowForm.get('description')?.value,
            status: details.some(d => d.status === 'Active') ? 'Active' : (details.length > 0 ? 'In-Active' : 'Active'),
            flowDetails: details
        };

        console.log('[DEBUG] Saving flow with payload:', JSON.stringify(flowModel, null, 2));
        this.flowsApi.createFlow(flowModel).subscribe({
            next: (res) => {
                if (res.code === 100) {
                    this.toast.success({ detail: "SUCCESS", summary: res.description || res.message || 'Flow created!', duration: 5000, position: 'topRight' });
                    this.dialogRef.close('saved');
                } else {
                    this.toast.error({ detail: "ERROR", summary: res.description || res.message || 'Failed to create flow', duration: 5000, position: 'topRight' });
                }
            },
            error: (err) => {
                this.toast.error({ detail: "ERROR", summary: err.error?.message || err.message || 'Server error', duration: 5000, position: 'topRight' });
            }
        });
    }
}

