import { Component } from '@angular/core';
import { PaymentService } from '../Service/Payment/payment-service';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-learningcourse',
  imports: [CommonModule,RouterLink],
  templateUrl: './learningcourse.html',
  styleUrl: './learningcourse.css',
})
export class Learningcourse {
constructor(
    private service: Studentservice,
    private paymentservice: PaymentService,
  ) {}

  courses: any[] = [];

  ngOnInit(): void {
    this.loadcourse();
  }

  loadcourse() {
    this.service.GetPublishedCoursesWithsubscribecheck().subscribe({
      next: (res: any) => {
        this.courses = res;
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
}
