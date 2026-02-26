import { validate } from '@angular/forms/signals';
import { captureError } from 'rxjs/internal/util/errorContext';
import { config } from './../app.config.server';
import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';

@Component({
  selector: 'app-emailverify',
  imports: [ReactiveFormsModule],
  templateUrl: './emailverify.html',
  styleUrl: './emailverify.css',
})
export class Emailverify implements OnInit {
  otpForm!: FormGroup;
  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private service: LoginRegisterService,
  ) {}
  @ViewChildren('otpInput') otpInputs!: QueryList<ElementRef>;
  email: string = '';

  ngOnInit(): void {
    this.otpForm = this.fb.group({
      otp0: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp1: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp2: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp3: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp4: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp5: ['', [Validators.required, Validators.pattern('[0-9]')]],
    });
    this.route.queryParams.subscribe((params) => {
      this.email = params['email'];
    });
    if (!this.email) {
      this.router.navigate(['/login']);
    }
  }

  onOtpInput(event: any, index: number) {
    // curretn element
    const input = event.target;

    // only number allow
    input.value = input.value.replace(/[^0-9]/g, '');

    // user enter1 then move next element at the ends of element
    if (input.value && index < this.otpInputs.length - 1) {
      this.otpInputs.toArray()[index + 1].nativeElement.focus();
    }
  }

  onKeyDown(event: KeyboardEvent, index: number) {
    // Backspace → move previous
    // for press Backspace but not for the first box
    if (event.key === 'Backspace' && index > 0) {
      const current = this.otpInputs.toArray()[index].nativeElement;

      // check the current box is not a empty
      if (!current.value) {
        // it's moved in the preview box by - index
        this.otpInputs.toArray()[index - 1].nativeElement.focus();
      }
    }
  }

  IsVarify = false;

  verifyOtp() {
    this.IsVarify = true;
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    const otp = Object.values(this.otpForm.value).join('');
    const OtpObj = {
      email: this.email,
      otp: otp,
    };

    this.service.OtpVarify(OtpObj).subscribe({
      next: (res) => {
        this.IsVarify = false;

        this.router.navigate(['/login']);

        // console.log('Otp Done');
      },
      error: (err) => {
        this.IsVarify = false;
        // console.log('Otp SIde Error');

        this.otpForm.setErrors({ invalidOtp: true });
      },
    });
    // console.log(OtpObj);
  }
}
