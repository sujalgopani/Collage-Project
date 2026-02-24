import { validate } from '@angular/forms/signals';
import { captureError } from 'rxjs/internal/util/errorContext';
import { config } from './../app.config.server';
import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-emailverify',
  imports: [ReactiveFormsModule],
  templateUrl: './emailverify.html',
  styleUrl: './emailverify.css',
})
export class Emailverify implements OnInit {
  otpForm!: FormGroup;
  constructor(private fb: FormBuilder) {}
  @ViewChildren('otpInput') otpInputs!: QueryList<ElementRef>;

  ngOnInit(): void {
    this.otpForm = this.fb.group({
      otp0: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp1: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp2: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp3: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp4: ['', [Validators.required, Validators.pattern('[0-9]')]],
      otp5: ['', [Validators.required, Validators.pattern('[0-9]')]],
    });
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

  verifyOtp() {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    const otp = Object.values(this.otpForm.value).join('');
    console.log('OTP:', otp);
  }
}
