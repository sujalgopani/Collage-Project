import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-teacher-live-classes',
  imports: [CommonModule, FormsModule],
  templateUrl: './teacher-live-classes.html',
  styleUrl: './teacher-live-classes.css',
})
export class TeacherLiveClasses implements OnInit, OnDestroy {
  liveClasses: any[] = [];
  materialDrafts: Record<number, any> = {};
  selectedFiles: Record<number, File | null> = {};

  isLoading = false;
  uploadingId: number | null = null;

  successMessage = '';
  errorMessage = '';

  private readonly refreshIntervalMs = 15000;
  private refreshSubscription?: Subscription;

  constructor(
    private teacherService: Teacherservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadLiveClasses();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadLiveClasses(showLoader = true) {
    if (showLoader) {
      this.isLoading = true;
      this.errorMessage = '';
    }

    this.teacherService.GetLiveClasses().subscribe({
      next: (res: any) => {
        const list = Array.isArray(res) ? res : [];
        const previousById = new Map(
          this.liveClasses.map((item: any) => [item.liveClassScheduleId, item]),
        );

        this.liveClasses = list.map((item: any) => {
          const previous = previousById.get(item.liveClassScheduleId);
          return { ...item, isEditing: previous?.isEditing ?? false };
        });

        this.liveClasses.forEach((item: any) => {
          const liveClassId = item.liveClassScheduleId;
          const previous = previousById.get(liveClassId);

          if (!previous?.isEditing || !this.materialDrafts[liveClassId]) {
            this.materialDrafts[liveClassId] = {
              materialTitle: item.materialTitle || '',
              materialDescription: item.materialDescription || '',
              materialLink: item.materialLink || '',
            };
          }

          if (!previous?.isEditing || !(liveClassId in this.selectedFiles)) {
            this.selectedFiles[liveClassId] = null;
          }
        });

        const activeIds = new Set(this.liveClasses.map((item: any) => item.liveClassScheduleId));

        Object.keys(this.materialDrafts).forEach((id) => {
          if (!activeIds.has(Number(id))) {
            delete this.materialDrafts[Number(id)];
          }
        });

        Object.keys(this.selectedFiles).forEach((id) => {
          if (!activeIds.has(Number(id))) {
            delete this.selectedFiles[Number(id)];
          }
        });

        this.isLoading = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load live class schedule right now.';
        this.cd.detectChanges();
      },
    });
  }

  toggleEdit(item: any) {
    item.isEditing = !item.isEditing;
    this.successMessage = '';
    this.errorMessage = '';
  }

  onFileSelected(event: any, liveClassId: number) {
    const file = event?.target?.files?.[0] || null;
    this.selectedFiles[liveClassId] = file;
  }

  uploadMaterial(item: any) {
    const liveClassId = item.liveClassScheduleId;
    const draft = this.materialDrafts[liveClassId] || {};
    const selectedFile = this.selectedFiles[liveClassId];

    this.successMessage = '';
    this.errorMessage = '';

    if (!selectedFile) {
      this.errorMessage = 'Material file is required (PDF, DOC, or DOCX).';
      this.cd.detectChanges();
      return;
    }

    const fileName = selectedFile.name?.toLowerCase() || '';
    const allowedExtensions = ['.pdf', '.doc', '.docx'];
    const hasAllowedExtension = allowedExtensions.some((extension) => fileName.endsWith(extension));
    if (!hasAllowedExtension) {
      this.errorMessage = 'Only PDF, DOC, and DOCX files are allowed.';
      this.cd.detectChanges();
      return;
    }

    const formData = new FormData();
    const materialTitle = (draft.materialTitle || '').trim();
    const materialDescription = (draft.materialDescription || '').trim();
    const materialLink = (draft.materialLink || '').trim();

    if (materialTitle) {
      formData.append('materialTitle', materialTitle);
    }

    if (materialDescription) {
      formData.append('materialDescription', materialDescription);
    }

    if (materialLink) {
      formData.append('materialLink', materialLink);
    }

    formData.append('materialFile', selectedFile);

    this.uploadingId = liveClassId;
    this.teacherService.UploadLiveClassMaterial(liveClassId, formData).subscribe({
      next: (res: any) => {
        this.uploadingId = null;
        this.successMessage = 'Material updated successfully.';

        const updated = res?.liveClass;
        if (updated) {
          item.materialTitle = updated.materialTitle;
          item.materialDescription = updated.materialDescription;
          item.materialLink = updated.materialLink;
          item.materialFilePath = updated.materialFilePath;
          item.updatedAt = updated.updatedAt;
        }

        this.selectedFiles[liveClassId] = null;
        item.isEditing = false;
        this.cd.detectChanges();
      },
      error: (err) => {
        this.uploadingId = null;
        this.errorMessage =
          err?.error?.message || err?.error || 'Unable to upload live class material right now.';
        this.cd.detectChanges();
      },
    });
  }

  getStatus(item: any): string {
    if (item?.isCancelled) return 'Cancelled';

    const now = new Date();
    const start = new Date(item.startAt);
    const end = new Date(item.endAt);

    if (now >= start && now <= end) return 'Live Now';
    if (now < start) return 'Upcoming';
    return 'Completed';
  }

  getMaterialFileUrl(path: string | null | undefined): string {
    if (!path) return '';
    return path.startsWith('http') ? path : `https://localhost:44385${path}`;
  }

  private startAutoRefresh() {
    this.refreshSubscription = interval(this.refreshIntervalMs).subscribe(() => {
      if (this.uploadingId !== null) {
        return;
      }

      const isAnyClassEditing = this.liveClasses.some((item: any) => item.isEditing);
      if (isAnyClassEditing) {
        return;
      }

      this.loadLiveClasses(false);
    });
  }
}
