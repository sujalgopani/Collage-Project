import { ChangeDetectorRef, Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Adminservice } from '../Service/AdminService/adminservice';
import { CommonModule } from '@angular/common';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { ActivatedRoute } from '@angular/router';

declare var bootstrap: any;

@Component({
  selector: 'app-admincoursemanage',
  imports: [ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './admincoursemanage.html',
  styleUrl: './admincoursemanage.css',
})
export class Admincoursemanage implements OnInit {
  constructor(
    private cdr: ChangeDetectorRef,
    private service: Adminservice,
    private teacherservice: Teacherservice,
    private route: ActivatedRoute,
  ) {}

  @ViewChild('deletemodel') deletemodel!: ElementRef;

  // course wise studnet
  @ViewChild('studentsModal') studentsModal!: ElementRef;
  students: any[] = [];
  isStudentsLoading = false;

  Isloading = false;
  courses: any[] = [];
  Ispublishing = false;
  searchTerm = '';

  // detail
  course: any = null;
  isLoading: boolean = false;
  modal: any;

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.searchTerm = (params.get('search') ?? '').trim();
      this.GetAllCoures(this.searchTerm);
    });
  }

  GetAllCoures(search = '') {
    this.courses = [];
    this.Isloading = true;
    this.service.GetAllCourses(search).subscribe({
      next: (res: any) => {
        this.Isloading = false;
        console.log(res);
        this.courses = res;
        console.log(res);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.cdr.detectChanges();
      },
    });
  }

  onSearch() {
    this.GetAllCoures(this.searchTerm);
  }

  PublishCourse(item: any) {
    if (item.isPublished) return; // safety

    item.isPublished = true; // 🔥 instantly update UI

    this.service.Publishcourse(item.courseId).subscribe({
      next: () => {
        // success → nothing needed
      },
      error: (err) => {
        console.log(err);

        // revert if failed
        item.isPublished = false;
      },
    });
  }

  selectedCourseId: number | null = null;
  errorMessage: string | null = null;
  isDeleting: boolean = false;

  openDeleteModal(courseId: number) {
    const modal = new bootstrap.Modal(this.deletemodel.nativeElement);
    modal.show();

    this.selectedCourseId = courseId;
    this.errorMessage = null; // ✅ reset old error
  }

  closeModal() {
    const modal = bootstrap.Modal.getInstance(this.deletemodel.nativeElement);
    modal?.hide();
  }

  confirmDelete() {
    if (!this.selectedCourseId) return;

    this.isDeleting = true;
    this.errorMessage = null;

    this.service.DeleteCourseById(this.selectedCourseId).subscribe({
      next: (res: any) => {
        this.courses = this.courses.filter((c) => c.courseId !== this.selectedCourseId);
        this.selectedCourseId = null;
        this.isDeleting = false;
        this.closeModal();
        this.cdr.detectChanges();
      },

      error: (err) => {
        this.isDeleting = false;
        if (typeof err.error === 'string') {
          this.errorMessage = err.error;
        } else if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else {
          this.errorMessage = 'Something went wrong!';
        }
        this.cdr.detectChanges();
      },
    });
  }

  // coursewise student
  openStudentsModal(courseId: number) {
    this.students = [];
    this.selectedCourseId = courseId;
    this.isStudentsLoading = true;

    new bootstrap.Modal(this.studentsModal.nativeElement).show();

    this.teacherservice.GetStudentCourseWise(courseId).subscribe({
      next: (res: any) => {
        this.students = res;
        this.isStudentsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isStudentsLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  ngAfterViewInit() {
    this.modal = new bootstrap.Modal(document.getElementById('courseModal'));
  }

  openDetails(courseId: number) {
    this.isLoading = true;

    this.service.getcoursebyid(courseId).subscribe({
      next: (res: any) => {
        this.course = res;
        this.isLoading = false;
        this.modal.show(); // open modal
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
