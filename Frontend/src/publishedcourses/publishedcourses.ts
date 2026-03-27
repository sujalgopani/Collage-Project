import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Studentservice } from '../app/Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../app/Service/Payment/payment-service';
import { NotificationService } from '../app/Service/notification.service';
import { finalize } from 'rxjs';

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
    private notification: NotificationService,
  ) {}

  courses: any[] = [];
  Isloading = false;
  processingCourseIds = new Set<number>();

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
    if (course?.isSubscribed) {
      return;
    }

    if (!course?.courseId || this.processingCourseIds.has(course.courseId)) {
      return;
    }

    this.processingCourseIds.add(course.courseId);

    const request = {
      amount: course.fees,
      courseId: course.courseId,
    };

    this.paymentservice.createOrder(request).subscribe({
      next: (res: any) => {
        const razorpayKey = (res?.key ?? '').toString().trim();
        if (!razorpayKey) {
          this.processingCourseIds.delete(course.courseId);
          this.notification.error('Payment gateway key is missing. Please contact admin.');
          return;
        }

        if (typeof Razorpay === 'undefined') {
          this.processingCourseIds.delete(course.courseId);
          this.notification.error('Payment gateway is not loaded. Please refresh and try again.');
          return;
        }

        const amountInPaise = Math.round(Number(res?.amount ?? course.fees) * 100);

        const options = {
          key: razorpayKey,
          amount: amountInPaise > 0 ? amountInPaise : undefined,
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

            this.paymentservice
              .verifyPayment(verifyData)
              .pipe(
                finalize(() => {
                  this.processingCourseIds.delete(course.courseId);
                  this.cd.detectChanges();
                }),
              )
              .subscribe({
                next: (verifyRes) => {
                  const message =
                    typeof verifyRes === 'string' && verifyRes.trim()
                      ? verifyRes
                      : 'Payment successful and subscription activated.';

                  course.isSubscribed = true;
                  this.notification.success(message);
                  this.loadcourse();
                  this.refreshPageAfterSubscribe();
                },
                error: (err) => {
                  console.log(err);
                  this.notification.error(
                    this.extractErrorMessage(err, 'Payment verification failed. Please contact support.'),
                  );
                },
              });
          },
          modal: {
            ondismiss: () => {
              this.processingCourseIds.delete(course.courseId);
              this.cd.detectChanges();
            },
          },

          theme: {
            color: '#3399cc',
          },
        };

        const rzp = new Razorpay(options);
        rzp.on('payment.failed', (_error: any) => {
          this.processingCourseIds.delete(course.courseId);
          this.notification.error('Payment failed. Please try again.');
          this.cd.detectChanges();
        });
        rzp.open();
      },
      error: (err) => {
        this.processingCourseIds.delete(course.courseId);
        console.log(err);
        this.notification.error(this.extractErrorMessage(err, 'Unable to start payment right now. Please try again.'));
      },
    });
  }

  private extractErrorMessage(error: any, fallback: string): string {
    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error.trim();
    }

    if (typeof error?.error?.message === 'string' && error.error.message.trim()) {
      return error.error.message.trim();
    }

    if (typeof error?.message === 'string' && error.message.trim()) {
      return error.message.trim();
    }

    return fallback;
  }

  private refreshPageAfterSubscribe(): void {
    if (typeof window === 'undefined') {
      return;
    }

    setTimeout(() => {
      window.location.reload();
    }, 300);
  }
}
