import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Studentservice } from '../app/Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../app/Service/Payment/payment-service';

declare var Razorpay: any;

@Component({
  selector: 'app-publishedcourses',
  imports: [CommonModule],
  templateUrl: './publishedcourses.html',
  styleUrl: './publishedcourses.css',
})
export class Publishedcourses implements OnInit {
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

  subscribe(course: any) {
    const request = {
      amount: course.fees,
      courseId: course.courseId,
    };

    this.paymentservice.createOrder(request).subscribe({
      next: (res: any) => {
        const options = {
          key: 'rzp_test_SNq5y6rEG7NEHW',
          amount: res.amount * 100,
          currency: 'INR',
          name: 'ExamNest',
          description: course.title,
          order_id: res.orderId,

          handler: (response: any) => {
            const verifyData = {
              razorpay_order_id: response.razorpay_order_id,
              razorpay_payment_id: response.razorpay_payment_id,
              razorpay_signature: response.razorpay_signature,
            };

            this.paymentservice.verifyPayment(verifyData).subscribe({
              next: (verifyRes) => {
                alert('Payment Successful 🎉 ' + verifyRes);
              },
              error: (err) => {
                console.log(err);
              },
            });
          },

          theme: {
            color: '#3399cc',
          },
        };

        const rzp = new Razorpay(options);
        rzp.open();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
}
