import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';
import { email } from '@angular/forms/signals';

@Component({
  selector: 'app-register',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register implements OnInit {
  registerForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private cd: ChangeDetectorRef,
    private route: Router,
  ) {}
  service = inject(LoginRegisterService);

  ngOnInit(): void {
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.minLength(3)]],
        middleName: ['', [Validators.minLength(2)]],
        lastName: ['', [Validators.required, Validators.minLength(3)]],
        email: ['', [Validators.required, Validators.email, this.gmailValidator]],
        phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
        password: ['', Validators.required],
        conformpassword: ['', Validators.required],
        role: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  //email validator
  gmailValidator(control: any) {
    const email = control.value;

    if (!email) return null;

    return email.endsWith('@gmail.com') ? null : { gmailInvalid: true };
  }

  // password check cpass or pass
  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('conformpassword');

    if (!password || !confirmPassword) return null;

    if (password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ ...confirmPassword.errors, passwordMismatch: true });
    } else {
      if (confirmPassword.errors) {
        delete confirmPassword.errors['passwordMismatch'];
        if (!Object.keys(confirmPassword.errors).length) {
          confirmPassword.setErrors(null);
        }
      }
    }

    return null;
  }

  IsSubmitting = false;
  submitted = false;
  errorMessage = '';

  onSubmit() {
    this.submitted = true;

    if (this.registerForm.invalid) {
      return;
    }

    this.IsSubmitting = true;

    const formData = { ...this.registerForm.value };
    delete formData.conformpassword;

    this.service.RegisterUser(formData).subscribe({
      next: (res) => {
        console.log('Success:', res);
        this.IsSubmitting = false;
        this.registerForm.reset();
        this.submitted = false; // reset submitted
        this.errorMessage = '';
        // sessionStorage.setItem("UserEmail",res.Email);
        this.route.navigate(['/emailvarify'], {
          queryParams: { email: formData.email },
        });
      },
      error: (err) => {
        this.IsSubmitting = false;
        if (err.status === 409) {
          this.registerForm.get('email')?.setErrors({ emailTaken: true });
          this.errorMessage = err.error.message;
        } else if (err.status === 400) {
          this.errorMessage = 'Invalid data submitted';
        } else {
          this.errorMessage = 'Server error. Try again later.';
        }
      },
    });
  }
}
