import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { inject, PLATFORM_ID } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { minError } from '@angular/forms/signals';
import { Coursewisestudent } from '../coursewisestudent/coursewisestudent';
import { RouterLink } from '@angular/router';

declare var bootstrap: any;
@Component({
  selector: 'app-owncourse',
  imports: [CommonModule,RouterLink],
  templateUrl: './owncourse.html',
  styleUrl: './owncourse.css',
})
export class Owncourse {
  courses: any[] = [];
  isLoading = true;
  private platformId = inject(PLATFORM_ID);
  selectedCourse: any = null;
  Isloaddetail = false;
  currentCourseId!: number;
  selectedFiles: File[] = [];
  fileErrors: string[] = [];
  isUploading = false;
  readonly maxUploadSizeBytes = 20 * 1024 * 1024;
  deleteErrorMessage: string = '';

  selectedCoursedetail: any = null;
  IsLoaddetail = false;
  CurrentCourseId!: number;

  deleteCourseId!: number;
  isDeleting = false;

  @ViewChild('detailModal') detailModal!: ElementRef;
  @ViewChild('editModel') editModel!: ElementRef;
  @ViewChild('deleteModal') deleteModal!: ElementRef;

  constructor(
    private teacherService: Teacherservice,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      this.isLoading = false;
      return;
    }

    this.loadCourses();
  }

  loadCourses() {
    this.teacherService.GetOwnCourses().subscribe({
      next: (res: any) => {
        this.courses = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  editbtn(courseId: number) {
    this.currentCourseId = courseId; // ✅ store id
    this.Isloaddetail = true;

    const modal = new bootstrap.Modal(this.editModel.nativeElement);
    modal.show(); // ✅ open immediately (better UX)

    this.teacherService.GetCourseDetail(courseId).subscribe({
      next: (res: any) => {
        this.selectedCourse = res;
        this.Isloaddetail = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.Isloaddetail = false;
      },
    });
  }

  videodelete(mediaid: number) {
    this.teacherService.MediaDeleteById(mediaid).subscribe({
      next: () => {
        // ✅ REFETCH SAME COURSE DATA
        this.refreshCourseDetail();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }

  refreshCourseDetail() {
    this.Isloaddetail = true;
    this.teacherService.GetCourseDetail(this.currentCourseId).subscribe({
      next: (res: any) => {
        this.selectedCourse = res;
        this.Isloaddetail = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.Isloaddetail = false;
      },
    });
  }

  onVideoSelected(event: any) {
    this.selectedFiles = [];
    this.fileErrors = [];

    const files = Array.from(event.target.files) as File[];
    files.forEach((file) => {
      if (file.size <= 0 || file.size > this.maxUploadSizeBytes) {
        this.fileErrors.push(`${file.name} must be between 0 and 20 MB.`);
      } else {
        this.selectedFiles.push(file);
      }
    });
  }

  uploadVideos() {
    if (!this.selectedFiles.length || this.fileErrors.length > 0) return;

    this.isUploading = true;

    const formData = new FormData();

    this.selectedFiles.forEach((file) => {
      formData.append('files', file); // backend should accept 'files'
    });

    this.teacherService.UploadMedia(this.currentCourseId, formData).subscribe({
      next: (res) => {
        console.log('Uploaded', res);

        this.selectedFiles = [];
        this.isUploading = false;

        // 🔥 REFRESH DETAIL WITHOUT CLOSING MODAL
        this.refreshCourseDetail();
      },
      error: (err) => {
        console.log(err);
        this.isUploading = false;
      },
    });
  }

  openDeleteModal(courseId: number) {
    this.deleteErrorMessage = ''; // reset old error

    this.deleteCourseId = courseId;
    const modal = new bootstrap.Modal(this.deleteModal.nativeElement);
    modal.show();
  }

  confirmDelete() {
    if (!this.deleteCourseId) return;

    this.isDeleting = true;
    this.deleteErrorMessage = ''; // reset old error

    this.teacherService.MediaDelete(this.deleteCourseId).subscribe({
      next: (res) => {
        this.isDeleting = false;

        // ✅ close modal
        const modal = bootstrap.Modal.getInstance(this.deleteModal.nativeElement);
        modal.hide();

        // ✅ refresh list
        this.loadCourses();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isDeleting = false;

        // ✅ capture backend error message
        if (err.error) {
          this.deleteErrorMessage = err.error;
        } else {
          this.deleteErrorMessage = 'Something went wrong while deleting.';
        }

        this.cdr.detectChanges();
      },
    });
  }

  deletebtn(courseId: number) {
    this.teacherService.MediaDelete(courseId).subscribe({
      next: (res) => {
        console.log(res);
      },
      error: (err) => {
        console.log(err);
      },
    });
  }

  detailbtn(courseId: number) {
    this.currentCourseId = courseId;
    this.Isloaddetail = true;

    // ✅ Open modal immediately
    const modal = new bootstrap.Modal(this.detailModal.nativeElement);
    modal.show();

    this.teacherService.GetCourseDetail(courseId).subscribe({
      next: (res: any) => {
        this.selectedCourse = res;
        this.Isloaddetail = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.Isloaddetail = false;
      },
    });
  }
}
