import { ChangeDetectorRef, Component } from '@angular/core';
import { PaymentService } from '../Service/Payment/payment-service';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { setThrowInvalidWriteToSignalError } from '@angular/core/primitives/signals';

@Component({
  selector: 'app-learningcourse',
  imports: [CommonModule, RouterLink],
  templateUrl: './learningcourse.html',
  styleUrl: './learningcourse.css',
})
export class Learningcourse {
  constructor(
    private service: Studentservice,
    private paymentservice: PaymentService,
    private cd: ChangeDetectorRef,
  ) {}



  courses: any[] = [];
  Isloading = false;

  ngOnInit(): void {
    this.loadcourse();
  }

  loadcourse() {
    this.Isloading = true;
    this.service.GetPublishedCoursesWithsubscribecheck().subscribe({
      next: (res: any) => {
        this.Isloading = false;
        console.log(res);
        this.courses = res.filter((c: any) => c.ispublished);
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }
}
