import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Adminservice } from '../Service/AdminService/adminservice';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

declare var bootstrap: any;
@Component({
  selector: 'app-rolemanage',
  imports: [FormsModule, CommonModule],
  templateUrl: './rolemanage.html',
  styleUrl: './rolemanage.css',
})
export class Rolemanage implements OnInit {
  roles: any[] = [];
  roleData: any = { roleId: 0, roleName: '' };
  roleError = '';
  isSavingRole = false;
  modal: any;
  isload = false;
  constructor(
    private service: Adminservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.loadRoles();
  }

  loadRoles() {
    this.isload = true;
    this.service.getRoles().subscribe({
      next: (res) => {
        this.isload = false;
        this.roles = res;
        this.cd.detectChanges();
      },
      error: (err) => {
        this.isload = false;
        this.cd.detectChanges();
      },
    });
  }

  openModal(data: any = null) {
    this.roleData = data ? { ...data } : { roleId: 0, roleName: '' };
    this.roleError = '';
    this.isSavingRole = false;
    this.modal = new bootstrap.Modal(document.getElementById('roleModal'));
    this.modal.show();
  }

  saveRole() {
    if (this.isSavingRole) {
      return;
    }

    const roleName = (this.roleData.roleName || '').trim();
    this.roleError = '';

    if (!roleName) {
      this.roleError = 'Role name is required.';
      return;
    }

    if (roleName.length < 3) {
      this.roleError = 'Role name must be at least 3 characters.';
      return;
    }

    if (roleName.length > 50) {
      this.roleError = 'Role name cannot exceed 50 characters.';
      return;
    }

    this.roleData.roleName = roleName;
    this.isSavingRole = true;

    if (this.roleData.roleId === 0) {
      this.service.createRole(this.roleData).subscribe({
        next: () => {
          this.isSavingRole = false;
          this.modal.hide();
          this.loadRoles();
          this.cd.detectChanges();
        },
        error: (err) => {
          this.isSavingRole = false;
          this.roleError = this.extractErrorMessage(err, 'Unable to create role right now.');
          this.cd.detectChanges();
        },
      });
    } else {
      this.service.updateRole(this.roleData.roleId, this.roleData).subscribe({
        next: () => {
          this.isSavingRole = false;
          this.modal.hide();
          this.loadRoles();
          this.cd.detectChanges();
        },
        error: (err) => {
          this.isSavingRole = false;
          this.roleError = this.extractErrorMessage(err, 'Unable to update role right now.');
          this.cd.detectChanges();
        },
      });
    }
  }

  deleteRole(id: number) {
    if (confirm('Delete this role?')) {
      this.service.deleteRole(id).subscribe(() => {
        this.loadRoles();
        this.cd.detectChanges();
      });
    }
  }

  isRoleNameInvalid() {
    const roleName = (this.roleData?.roleName || '').trim();
    return roleName.length < 3 || roleName.length > 50;
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) {
      return err.error.trim();
    }

    if (typeof err?.error?.message === 'string' && err.error.message.trim()) {
      return err.error.message.trim();
    }

    return fallback;
  }
}
