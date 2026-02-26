import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';
import { jwtDecode } from 'jwt-decode';

@Component({
  selector: 'app-login',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  loginfrm!: FormGroup;
  Isseen = false;

  constructor(
    private fb: FormBuilder,
    private service: LoginRegisterService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loginfrm = this.fb.group({
      emailOrUsername: ['', [Validators.required, Validators.minLength(5)]],
      password: ['', Validators.required],
    });
  }

  toggleSeen(passwordInput: HTMLInputElement) {
    this.Isseen = !this.Isseen;
    setTimeout(() => {
      passwordInput.focus();
    }, 0);
  }

  IsLogin = false;

  Onlogin() {
    this.IsLogin = true;
    this.service.LoginUser(this.loginfrm.value).subscribe({
      next: (res) => {
        this.IsLogin = false;
        console.log(res);
        const token = res.token;
        localStorage.setItem('token', token);

        const decoded: any = jwtDecode(token);
        const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        const roleId = decoded['role_id'];

        localStorage.setItem('role', role);
        localStorage.setItem('roleId', roleId);

        switch (role) {
          case 'Admin':
            this.router.navigate(['/admin-dashboard']);
            break;

          case 'Teacher':
            this.router.navigate(['/teacher-dashboard']);
            break;

          case 'Student':
            this.router.navigate(['/student-dashboard']);
            break;

          default:
            this.router.navigate(['/login']);
            break;
        }
      },
      error: (err) => {
        this.IsLogin = false;
        console.log(err);
        this.loginfrm?.setErrors({ LoginFail: true });
      },
    });
    console.log(this.loginfrm.value);
  }
}
