import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Studentservice } from '../Service/StudentService/studentservice';
import { Router } from '@angular/router';

@Component({
  selector: 'app-studentexam',
  imports: [CommonModule],
  templateUrl: './studentexam.html',
  styleUrl: './studentexam.css',
})
export class Studentexam implements OnInit {
  exams: any[] = [];
  examsquestions: any[] = [];
  Isload = false;
  constructor(
    private servcie: Studentservice,
    private cd: ChangeDetectorRef,
    private router: Router,
  ) {}
  ngOnInit(): void {
    this.GetAllExams();
  }

  GetAllExams() {
    this.Isload = true;
    this.servcie.GetAllExams().subscribe({
      next: (res: any) => {
        this.Isload = false;
        console.log(res);
        this.exams = res;
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isload = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  Isstart = false;
  StartExam(ExamId: number) {
    this.servcie.GetStartExam(ExamId).subscribe({
      next: (res: any) => {
        this.Isstart = true;
        // console.log(res);
        this.examsquestions = res.questions;
        console.log(this.examsquestions);
        this.router.navigate(['/student-dashboard/student-exam-attempt'], {
          state: { examData: res },
        });
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isstart = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  IsExamEnded(endAt: string): boolean {
    return new Date(endAt) < new Date();
  }
  isExamNotStarted(startAt: string): boolean {
    return new Date(startAt) > new Date();
  }
}
