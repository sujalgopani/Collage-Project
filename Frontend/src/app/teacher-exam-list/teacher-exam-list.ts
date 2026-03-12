import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

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
  selectedfile?: File;
  Isloadcourse = false;
  @ViewChild('examcreatemodel') examcreatemodel!: ElementRef;

  constructor(
    private service: Teacherservice,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef,
  ) {
    this.examForm = this.fb.group({
      CourseId: ['', Validators.required],
      Title: ['', Validators.required],
      Description: ['', Validators.required],
      StartAt: ['', Validators.required],
      EndAt: ['', Validators.required],
      DurationMinutes: [0],
      RandomQuestionCount: [0],
    });
  }

  ngOnInit(): void {
    this.locadcourse();
    this.loadexamdetail();
  }

  onFileSelected(event: any) {
    console.log(event.target.files);
    this.selectedfile = event.target.files[0];
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

  CreareExam() {
    console.log(this.examForm.value);
    if (this.examForm.invalid) {
      this.examForm.markAllAsTouched();
      return;
    }
    const formData = new FormData();
    Object.keys(this.examForm.value).forEach((key) => {
      formData.append(key, this.examForm.value[key]);
    });

    if (this.selectedfile) {
      formData.append('ExcelFile', this.selectedfile);
    }

    this.service.CreateExam(formData).subscribe({
      next: (res) => {
        console.log('Exam created successfully', res);
        // Reset form if needed
        this.examForm.reset();
        this.loadexamdetail();
        this.selectedfile = undefined;
        this.closeModal();
      },
      error: (err) => {
        console.error('Error creating exam', err);
      },
    });
  }

  loadexamdetail() {
    this.service.GetTeacherWiseExamDetail().subscribe({
      next: (res: any) => {
        console.log(res);
        this.exams = res;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  closeModal() {
    const modalEl = this.examcreatemodel.nativeElement;
    const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modal.hide();
  }


}
