import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { TeacherDashboard } from '../teacher-dashboard/teacher-dashboard';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { nextTick } from 'process';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ExamStudentResult } from '../exam-student-result/exam-student-result';

@Component({
  selector: 'app-studentexamwise',
  imports: [CommonModule],
  templateUrl: './studentexamwise.html',
  styleUrl: './studentexamwise.css',
})
export class Studentexamwise implements OnInit {
  constructor(
    private service: Teacherservice,
    private cd: ChangeDetectorRef,
  ) {}

  exams: any[] = [];
  isload = false;

  ngOnInit(): void {
    this.loadexam();
  }

  loadexam() {
    this.isload = true;
    this.service.GetTeacherWiseExamDetail().subscribe({
      next: (res: any) => {
        this.isload = false;
        console.log(res);
        this.exams = res;
        this.cd.detectChanges();
      },
      error: (err) => {
        this.isload = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  getdetail(examId: any) {
    const exam = this.exams.find((x) => x.examId === examId);
    if (!exam) return;

    exam.isLoadingStudents = true;
    exam.showStudents = true;

    this.service.GetStudentByExam(examId).subscribe({
      next: (res: any) => {
        console.log(res);

        // if API returns single object → wrap in array
        exam.students = Array.isArray(res) ? res : [res];

        exam.isLoadingStudents = false;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        exam.isLoadingStudents = false;
        this.cd.detectChanges();
      },
    });
  }
}
