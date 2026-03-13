import { Login } from './../login/login';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';
import { rmSync } from 'fs';
import { ImplicitReceiver } from '@angular/compiler';
import { Console, log } from 'console';

@Component({
  selector: 'app-exam-student-result',
  imports: [],
  templateUrl: './exam-student-result.html',
  styleUrl: './exam-student-result.css',
})
export class ExamStudentResult implements OnInit {
  examData: any;
  Isloading = false;

  constructor(
    private service: Studentservice,
    private router: Router,
    private cd: ChangeDetectorRef,
  ) {}
  ngOnInit(): void {
    this.loadexam();
  }

  loadexam() {
    this.Isloading = true;
    this.service.GetAttemptExams().subscribe({
      next: (res) => {
        this.Isloading = false;
        this.examData = res;
        console.log(res);
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  GetResult(examId :number,attemptId :number){
    this.service.GetExamResult(examId,attemptId).subscribe({
      next:(res)=>{
        console.log(res);
      },
      error:(err)=>{
        console.log(err);
      }
    })
  }

}
