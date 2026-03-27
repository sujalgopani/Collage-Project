import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { Adminservice } from '../Service/AdminService/adminservice';

@Component({
  selector: 'app-admin-live-classes',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-live-classes.html',
  styleUrl: './admin-live-classes.css',
})
export class AdminLiveClasses implements OnInit, OnDestroy {
  courses: any[] = [];
  liveClasses: any[] = [];

  isLoadingCourses = false;
  isLoadingClasses = false;
  isSaving = false;
  deletingId: number | null = null;

  successMessage = '';
  errorMessage = '';

  form = {
    courseId: 0,
    title: '',
    agenda: '',
    meetingLink: '',
    classDate: new Date().toISOString().split('T')[0],
    startTime: '',
    endTime: '',
  };

  private readonly refreshIntervalMs = 15000;
  private refreshSubscription?: Subscription;

  constructor(
    private adminService: Adminservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadCourses();
    this.loadLiveClasses();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadCourses() {
    this.isLoadingCourses = true;
    this.adminService.GetAllCourses().subscribe({
      next: (res: any) => {
        this.courses = Array.isArray(res) ? res : [];
        this.isLoadingCourses = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.isLoadingCourses = false;
        this.errorMessage = 'Unable to load courses right now.';
        this.cd.detectChanges();
      },
    });
  }

  loadLiveClasses(showLoader = true) {
    if (showLoader) {
      this.isLoadingClasses = true;
    }

    this.adminService.GetLiveClasses().subscribe({
      next: (res: any) => {
        this.liveClasses = Array.isArray(res) ? res : [];
        this.isLoadingClasses = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.isLoadingClasses = false;
        this.errorMessage = 'Unable to load live classes right now.';
        this.cd.detectChanges();
      },
    });
  }

  createLiveClass() {
    this.successMessage = '';
    this.errorMessage = '';

    if (!this.form.courseId) {
      this.errorMessage = 'Please select a course.';
      return;
    }

    if (!this.form.classDate || !this.form.startTime || !this.form.endTime) {
      this.errorMessage = 'Please select class date and timing.';
      return;
    }

    const startAt = this.toFakeUtc(this.form.classDate, this.form.startTime);
    const endAt = this.toFakeUtc(this.form.classDate, this.form.endTime);
    if (!startAt || !endAt) {
      this.errorMessage = 'Invalid date or time selected.';
      return;
    }

    if (new Date(startAt) >= new Date(endAt)) {
      this.errorMessage = 'End time must be later than start time.';
      return;
    }

    if (!this.form.title.trim()) {
      this.errorMessage = 'Please enter class title.';
      return;
    }

    if (!this.form.meetingLink.trim()) {
      this.errorMessage = 'Please enter meeting link.';
      return;
    }

    this.isSaving = true;
    const payload = {
      courseId: this.form.courseId,
      title: this.form.title.trim(),
      agenda: this.form.agenda.trim(),
      meetingLink: this.form.meetingLink.trim(),
      startAt,
      endAt,
    };

    this.adminService.CreateLiveClass(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Live class scheduled successfully.';
        this.form.title = '';
        this.form.agenda = '';
        this.form.meetingLink = '';
        this.form.classDate = new Date().toISOString().split('T')[0];
        this.form.startTime = '';
        this.form.endTime = '';
        this.loadLiveClasses();
        this.cd.detectChanges();
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage =
          err?.error?.message || err?.error || 'Unable to schedule live class right now.';
        this.cd.detectChanges();
      },
    });
  }

  deleteLiveClass(id: number) {
    this.successMessage = '';
    this.errorMessage = '';
    this.deletingId = id;

    this.adminService.DeleteLiveClass(id).subscribe({
      next: () => {
        this.deletingId = null;
        this.successMessage = 'Live class removed successfully.';
        this.liveClasses = this.liveClasses.filter((x) => x.liveClassScheduleId !== id);
        this.cd.detectChanges();
      },
      error: (err) => {
        this.deletingId = null;
        this.errorMessage =
          err?.error?.message || err?.error || 'Unable to delete this live class right now.';
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

  private toFakeUtc(date: string, time: string): string | null {
    if (!date || !time) return null;

    return `${date}T${time}:00.000Z`;
  }

  private startAutoRefresh() {
    this.refreshSubscription = interval(this.refreshIntervalMs).subscribe(() => {
      if (this.isSaving || this.deletingId !== null) {
        return;
      }

      this.loadLiveClasses(false);
    });
  }
}
