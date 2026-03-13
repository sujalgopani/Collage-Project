import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Studentservice } from '../Service/StudentService/studentservice';
import { error } from 'console';
import { Router } from '@angular/router';

@Component({
  selector: 'app-student-exam-attempt',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './student-exam-attempt.html',
  styleUrl: './student-exam-attempt.css',
})
export class StudentExamAttempt {
  examData: any;

  // store selected answers
  selectedAnswers: any = {};

  constructor(
    private service: Studentservice,
    private router: Router,
  ) {}

  ngOnInit() {
    this.examData = history.state.examData;
    console.log(this.examData);
  }

  // when user selects option
  selectAnswer(questionId: number, optionId: string) {
    this.selectedAnswers[questionId] = optionId;
  }

  SubmitExam(ExamID: number) {
    const payload = {
      examAttemptId: this.examData.attemptId,
      answers: Object.keys(this.selectedAnswers).map((qId) => ({
        examQuestionId: Number(qId),
        selectedOption: this.selectedAnswers[qId],
      })),
    };

    console.log('Submit Object:', payload);

    this.service.SubmitExam(ExamID, payload).subscribe({
      next: (res) => {
        console.log(res);
        this.router.navigate(['/student-dashboard/student-exam']);
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
}
