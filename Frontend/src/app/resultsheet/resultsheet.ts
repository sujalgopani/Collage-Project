import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';

@Component({
  selector: 'app-resultsheet',
  imports: [CommonModule, RouterOutlet],
  templateUrl: './resultsheet.html',
  styleUrl: './resultsheet.css',
})
export class Resultsheet implements OnInit {
  constructor(
    private router: ActivatedRoute,
    private service: Studentservice,
    private cd: ChangeDetectorRef,
    private navigate: Router,
  ) {}

  resultdata: any;
  percentage = 0;
  grade = '';

  ngOnInit(): void {
    const examId = Number(this.router.snapshot.paramMap.get('examId'));
    const attemptId = Number(this.router.snapshot.paramMap.get('attemptId'));

    this.service.GetExamResult(examId, attemptId).subscribe({
      next: (res: any) => {
        console.log(res);
        this.resultdata = res;
        // calculate percentage
        this.percentage = (res.totalScore / res.maxScore) * 100;

        // grade logic
        if (this.percentage >= 90) this.grade = 'A+';
        else if (this.percentage >= 80) this.grade = 'A';
        else if (this.percentage >= 70) this.grade = 'B';
        else if (this.percentage >= 60) this.grade = 'C';
        else if (this.percentage >= 50) this.grade = 'D';
        else this.grade = 'F';
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  backbtn() {
    this.navigate.navigate(['/student-dashboard/student-exam-result']);
  }

  
}
