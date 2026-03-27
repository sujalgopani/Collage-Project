import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../Service/notification.service';

@Component({
  selector: 'app-exam-student-result',
  imports: [CommonModule],
  templateUrl: './exam-student-result.html',
  styleUrl: './exam-student-result.css',
})
export class ExamStudentResult implements OnInit {
  examData: any[] = [];
  Isloading = false;

  constructor(
    private service: Studentservice,
    private router: Router,
    private cd: ChangeDetectorRef,
    private notification: NotificationService,
  ) {}
  ngOnInit(): void {
    this.loadexam();
  }

  loadexam() {
    this.Isloading = true;
    this.service.GetAttemptExams().subscribe({
      next: (res: any) => {
        this.Isloading = false;
        const raw = Array.isArray(res) ? res : [];
        this.examData = raw.map((exam: any) => ({
          ...exam,
          isFlagged: !!exam?.isFlagged,
          status: (exam?.status || '').toString(),
        }));
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.notification.error('Unable to load exam results right now.');
        this.cd.detectChanges();
      },
    });
  }

  hasFinalizedAttempt(exam: any): boolean {
    const status = (exam?.status || '').toString().toLowerCase();
    return status === 'submitted' || status === 'submittedlate' || status === 'autoterminated';
  }

  canOpenResult(exam: any): boolean {
    return this.hasFinalizedAttempt(exam) && !!exam?.isFlagged;
  }

  GetResult(examId :number,attemptId :number, exam?: any){
    if (exam && !this.canOpenResult(exam)) {
      this.notification.info('Result is not published yet.');
      return;
    }

    this.service.GetExamResult(examId,attemptId).subscribe({
      next:(res)=>{
          this.router.navigate(['/student-dashboard/student-result', examId, attemptId]);
      },
      error:(err)=>{
        console.log(err);
        this.notification.error(
          err?.error?.message ||
          err?.error ||
          'Unable to open result right now.',
        );
      }
    })
  }

}
