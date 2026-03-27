import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NotificationService } from '../Service/notification.service';

declare var bootstrap: any;
@Component({
  selector: 'app-teacher-exam-list',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './teacher-exam-list.html',
  styleUrl: './teacher-exam-list.css',
})
export class TeacherExamList implements OnInit {
  examForm!: FormGroup;
  courses: any[] = [];
  exams: any[] = [];
  publishingExamIds = new Set<number>();
  selectedFile: File | null = null;
  excelFileError = '';
  readonly maxUploadSizeBytes = 20 * 1024 * 1024;
  Isloadcourse = false;
  minDateTime: string = '';
  minEndDate: string = '';

  @ViewChild('examcreatemodel') examcreatemodel!: ElementRef;

  constructor(
    private service: Teacherservice,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef,
    private notification: NotificationService,
  ) {
    this.examForm = this.fb.group(
      {
        CourseId: ['', Validators.required],
        Title: ['', [Validators.required, Validators.minLength(3)]],
        Description: ['', [Validators.required, Validators.minLength(10)]],
        StartAt: ['', Validators.required],
        EndAt: ['', Validators.required],
        DurationMinutes: [null, [Validators.required, Validators.min(1)]],
        RandomQuestionCount: [null, [Validators.required, Validators.min(1)]],
      },
      { validators: this.dateValidator },
    );
  }

  ngOnInit(): void {
    this.locadcourse();
    this.loadexamdetail();
    this.setMinDateTime();
    this.setEndMinDateTime();

    this.examForm.get('StartAt')?.valueChanges.subscribe(() => {
      this.setEndMinDateTime();
    });

    this.examForm.statusChanges.subscribe(() => {
      this.cd.detectChanges();
    });
  }

  setMinDateTime() {
    this.minDateTime = this.formatDateTimeLocal(new Date());
  }

  setEndMinDateTime() {
    const now = new Date();
    const startRaw = this.examForm.get('StartAt')?.value;
    const startDate = startRaw ? new Date(startRaw) : null;

    const minEnd =
      startDate && !Number.isNaN(startDate.getTime()) && startDate > now ? startDate : now;

    this.minEndDate = this.formatDateTimeLocal(minEnd);
  }

  private formatDateTimeLocal(date: Date): string {
    const year = date.getFullYear();
    const month = ('0' + (date.getMonth() + 1)).slice(-2);
    const day = ('0' + date.getDate()).slice(-2);
    const hours = ('0' + date.getHours()).slice(-2);
    const minutes = ('0' + date.getMinutes()).slice(-2);

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  get f() {
    return this.examForm.controls;
  }

  isInvalid(field: string) {
    const control = this.f[field];
    return control.invalid && (control.touched || this.submitted);
  }

  dateValidator(group: any) {
    const start = group.get('StartAt')?.value;
    const end = group.get('EndAt')?.value;

    if (!start || !end) return null;

    const now = new Date();
    now.setSeconds(0, 0); // ✅ FIX

    const startDate = new Date(start);
    const endDate = new Date(end);

    if (startDate < now) {
      return { startPast: true };
    }

    if (endDate < now) {
      return { endPast: true };
    }

    if (endDate <= startDate) {
      return { endBeforeStart: true };
    }

    return null;
  }

  onFileSelected(event: any) {
    this.excelFileError = '';
    this.selectedFile = null;

    const file = event.target.files?.[0] as File | undefined;
    if (!file) {
      return;
    }

    if (file.size <= 0 || file.size > this.maxUploadSizeBytes) {
      this.excelFileError = 'Excel file must be between 0 and 20 MB.';
      return;
    }

    this.selectedFile = file;
  }

  locadcourse() {
    this.Isloadcourse = true;
    this.service.GetOwnCourses().subscribe({
      next: (res: any) => {
        this.Isloadcourse = false;
        this.courses = res;
        console.log(res);
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isloadcourse = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  isLoading: boolean = false;
  submitted = false;

  CreareExam() {
    this.submitted = true;

    // ✅ mark all fields touched
    this.examForm.markAllAsTouched();

    // ✅ check BOTH field + form errors
    if (this.examForm.invalid || this.examForm.errors) {
      return;
    }

    if (this.excelFileError) {
      return;
    }

    this.isLoading = true;

    const formData = new FormData();

    Object.keys(this.examForm.value).forEach((key) => {
      formData.append(key, this.examForm.value[key]);
    });

    if (this.selectedFile) {
      formData.append('ExcelFile', this.selectedFile);
    }

    this.service.CreateExam(formData).subscribe({
      next: (res) => {
        this.examForm.reset();
        this.selectedFile = null;
        this.submitted = false;

        this.setMinDateTime();
        this.setEndMinDateTime();

        this.loadexamdetail();
        this.closeModal();

        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      },
    });
  }

  loadexamdetail() {
    this.service.GetTeacherWiseExamDetail().subscribe({
      next: (res: any) => {
        const raw = Array.isArray(res) ? res : [];
        this.exams = raw.map((exam: any) => ({
          ...exam,
          examId: Number(exam?.examId),
          isflagged: !!exam?.isflagged,
        }));
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.notification.error('Unable to load exam details right now.');
        this.cd.detectChanges();
      },
    });
  }

  closeModal() {
    const modalEl = this.examcreatemodel.nativeElement;
    const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modal.hide();
  }

  getExamStatus(exam: any) {
    const now = new Date();

    const start = new Date(exam.startdate);
    const end = new Date(exam.enddate);

    if (now < start) {
      return 'notStarted';
    }

    if (now >= start && now <= end) {
      return 'ongoing';
    }

    return 'finished';
  }

  publishresult(examid: number) {
    examid = Number(examid); // ✅ ensure number

    if (!Number.isFinite(examid) || examid <= 0) {
      this.notification.error('Invalid exam selected.');
      return;
    }

    // prevent duplicate click
    if (this.publishingExamIds.has(examid)) {
      return;
    }

    // ✅ set loading state
    this.publishingExamIds.add(examid);
    this.cd.detectChanges(); // 🔥 immediately update UI

    this.service.PublishResult(examid).subscribe({
      next: (res: any) => {
        // ✅ IMMUTABLE UPDATE (IMPORTANT)
        this.exams = this.exams.map((e) =>
          Number(e.examId) === examid ? { ...e, isflagged: true } : e,
        );

        // ✅ remove loading
        this.publishingExamIds.delete(Number(examid));

        this.notification.success(res?.message || 'Result published successfully.');

        this.cd.detectChanges(); // 🔥 force refresh
      },

      error: (err) => {
        console.log(err);

        const message = err?.error?.message || err?.error || 'Unable to publish result right now.';

        this.notification.error(message);

        // ✅ remove loading
        this.publishingExamIds.delete(Number(examid));

        this.cd.detectChanges();
        this.locadcourse();
      },
    });
  }

  isPublishing(examId: number): boolean {
    return this.publishingExamIds.has(Number(examId)); // ✅ FIXED
  }
}
