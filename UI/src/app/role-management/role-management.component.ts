import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ApiService } from '../services/api.service';
import { NgToastService } from 'ng-angular-popup';
import { TranslateModule } from '@ngx-translate/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RoleHelpDialogComponent } from './role-help-dialog/role-help-dialog.component';

interface RoleItem {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  createdDate: string;
  createdBy: number;
}

interface MenuAssignment {
  menuId: number;
  menuName: string;
  moduleName: string;
  route: string;
  canView: boolean;
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canResubmit: boolean;
  canReTransmit: boolean;
}

@Component({
  selector: 'app-role-management',
  templateUrl: './role-management.component.html',
  styleUrls: ['./role-management.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatSelectModule,
    MatExpansionModule,
    MatCardModule,
    MatTabsModule,
    MatSlideToggleModule,
    TranslateModule,
    MatTooltipModule
  ],
})
export class RoleManagementComponent implements OnInit {
  roles: RoleItem[] = [];
  modules: any[] = [];
  menus: any[] = [];
  selectedRole: RoleItem | null = null;
  menuAssignments: MenuAssignment[] = [];
  displayedColumns: string[] = ['name', 'description', 'isActive', 'actions'];

  // New/Edit role form
  editingRole: RoleItem = { id: 0, name: '', description: '', isActive: true, createdDate: '', createdBy: 0 };
  showRoleForm = false;

  // Config-driven (live) visibility of the Resubmit / Re-Transmit permission columns.
  // Toggled via ApplicationSettings (script) — no redeploy needed.
  showResubmitCol = false;
  showReTransmitCol = false;

  constructor(private api: ApiService, private toast: NgToastService, private dialog: MatDialog) {}

  ngOnInit(): void {
    this.loadRoles();
    this.loadModulesAndMenus();
    this.loadActionColumnVisibility();
  }

  loadActionColumnVisibility(): void {
    this.api.getActionColumnVisibility().subscribe({
      next: (res: any) => {
        this.showResubmitCol = !!res?.showResubmit;
        this.showReTransmitCol = !!res?.showReTransmit;
      },
      error: () => { /* default hidden */ }
    });
  }

  loadRoles(): void {
    this.api.getRoles().subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.roles = res.roles || [];
        }
      },
      error: (err: any) => console.error('Error loading roles', err)
    });
  }

  loadModulesAndMenus(): void {
    this.api.getModules().subscribe({
      next: (res: any) => {
        if (res.code === 200) this.modules = res.modules || [];
      }
    });
    this.api.getMenusDef().subscribe({
      next: (res: any) => {
        if (res.code === 200) this.menus = res.menus || [];
      }
    });
  }

  openNewRole(): void {
    this.editingRole = { id: 0, name: '', description: '', isActive: true, createdDate: '', createdBy: 0 };
    this.showRoleForm = true;
  }

  editRole(role: RoleItem): void {
    this.editingRole = { ...role };
    this.showRoleForm = true;
  }

  saveRole(): void {
    this.api.saveRole(this.editingRole).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.toast.success({ detail: 'Success', summary: res.message, duration: 3000, position: 'topRight' });
          this.showRoleForm = false;
          this.loadRoles();
        } else {
          this.toast.error({ detail: 'Error', summary: res.message, duration: 3000, position: 'topRight' });
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: 'Error', summary: 'Failed to save role', duration: 3000, position: 'topRight' });
      }
    });
  }

  cancelEdit(): void {
    this.showRoleForm = false;
  }

  deleteRole(role: RoleItem): void {
    if (confirm(`Are you sure you want to delete role "${role.name}"?`)) {
      this.api.deleteRole({ id: role.id }).subscribe({
        next: (res: any) => {
          if (res.code === 200) {
            this.toast.success({ detail: 'Success', summary: res.message, duration: 3000, position: 'topRight' });
            this.loadRoles();
          } else {
            this.toast.error({ detail: 'Error', summary: res.message, duration: 3000, position: 'topRight' });
          }
        }
      });
    }
  }

  selectRole(role: RoleItem): void {
    this.selectedRole = role;
    this.loadRoleMenuAssignments(role.id);
  }

  loadRoleMenuAssignments(roleId: number): void {
    this.api.getRoleMenus(roleId).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          const roleMenus = res.roleMenus || [];
          this.menuAssignments = this.menus.map(menu => {
            const existing = roleMenus.find((rm: any) => rm.menuId === menu.id);
            const mod = this.modules.find(m => m.id === menu.moduleId);
            return {
              menuId: menu.id,
              menuName: menu.name,
              moduleName: mod?.name || '',
              route: menu.route || '',
              canView: existing ? existing.canView : false,
              canAdd: existing ? existing.canAdd : false,
              canEdit: existing ? existing.canEdit : false,
              canDelete: existing ? existing.canDelete : false,
              canResubmit: existing ? existing.canResubmit : false,
              canReTransmit: existing ? existing.canReTransmit : false
            };
          });
        }
      }
    });
  }

  getMenusByModule(moduleName: string): MenuAssignment[] {
    return this.menuAssignments.filter(m => m.moduleName === moduleName);
  }

  // Resubmit / Re-Transmit permissions apply only to the Orders menu.
  isOrdersMenu(menu: MenuAssignment): boolean {
    return (menu.route || '').replace(/^\//, '') === 'edi/all-orders';
  }

  // Show the Resubmit / Re-Transmit columns only for the module that contains the Orders menu.
  moduleHasOrders(moduleName: string): boolean {
    return this.getMenusByModule(moduleName).some(m => this.isOrdersMenu(m));
  }

  getUniqueModules(): string[] {
    return [...new Set(this.menuAssignments.map(m => m.moduleName))];
  }

  toggleSelectAll(moduleName: string, checked: boolean): void {
    this.menuAssignments
      .filter(m => m.moduleName === moduleName)
      .forEach(m => {
        m.canView = checked;
        m.canAdd = checked;
        m.canEdit = checked;
        m.canDelete = checked;
        m.canResubmit = checked;
        m.canReTransmit = checked;
      });
  }

  openHelp(): void {
    this.dialog.open(RoleHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  saveMenuAssignments(): void {
    if (!this.selectedRole) return;

    const menusToSave = this.menuAssignments
      .filter(m => m.canView || m.canAdd || m.canEdit || m.canDelete || m.canResubmit || m.canReTransmit)
      .map(m => ({
        menuId: m.menuId,
        canView: m.canView,
        canAdd: m.canAdd,
        canEdit: m.canEdit,
        canDelete: m.canDelete,
        canResubmit: m.canResubmit,
        canReTransmit: m.canReTransmit
      }));

    this.api.saveRoleMenus({ roleId: this.selectedRole.id, menus: menusToSave }).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.toast.success({ detail: 'Success', summary: res.message, duration: 3000, position: 'topRight' });
        } else {
          this.toast.error({ detail: 'Error', summary: res.message, duration: 3000, position: 'topRight' });
        }
      },
      error: () => {
        this.toast.error({ detail: 'Error', summary: 'Failed to save menu assignments', duration: 3000, position: 'topRight' });
      }
    });
  }
}
