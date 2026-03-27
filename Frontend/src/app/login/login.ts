import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';
import { jwtDecode } from 'jwt-decode';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';

declare global {
  interface Window {
    google?: any;
  }
}

// Custom password validator
export function customPasswordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null; // required validator will handle empty

    // Prevent simple sequences
    const sequences = ['12345', 'abcdef', 'qwerty', 'password'];
    for (const seq of sequences) {
      if (value.toLowerCase().includes(seq)) {
        return { sequenceNotAllowed: true };
      }
    }

    // Minimum complexity: uppercase, number, special char
    const pattern = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).+$/;
    if (!pattern.test(value)) {
      return { weakPassword: true };
    }

    return null; // valid
  };
}

@Component({
  selector: 'app-login',
  imports: [RouterLink, ReactiveFormsModule, CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login implements OnInit, OnDestroy {
  loginfrm!: FormGroup;
  Isseen = false;
  IsLogin = false;
  IsGoogleLogin = false;
  authError = '';
  googleClientId = '';
  isGoogleInitialized = false;
  googleScriptLoaded = false;
  googleInitPromise: Promise<void> | null = null;

  constructor(
    private fb: FormBuilder,
    private service: LoginRegisterService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loginfrm = this.fb.group({
      emailOrUsername: ['', [Validators.required, Validators.minLength(5)]],
      password: ['', [Validators.required,
        //customPasswordValidator()
      ]],
    });

    this.setupGoogleSignIn().catch(() => {
      // Ignore init failure here; user can retry by clicking Google Sign-In button.
    });
  }

  ngOnDestroy(): void {
    this.googleInitPromise = null;
  }

  toggleSeen(passwordInput: HTMLInputElement) {
    this.Isseen = !this.Isseen;
    setTimeout(() => passwordInput.focus(), 0);
  }

  Onlogin() {
    this.authError = '';
    if (this.loginfrm.invalid) {
      this.loginfrm.markAllAsTouched();
      return;
    }

    this.IsLogin = true;
    this.service.LoginUser(this.loginfrm.value).subscribe({
      next: (res) => {
        this.IsLogin = false;
        this.handleAuthSuccess(res.token);
      },
      error: (err) => {
        this.IsLogin = false;
        this.authError =
          err?.error?.message ||
          err?.error ||
          'Invalid username or password. Please try again.';
        this.loginfrm.setErrors({ LoginFail: true });
      },
    });
  }

  async onGoogleSignIn() {
    this.authError = '';

    try {
      await this.setupGoogleSignIn();
    } catch {
      this.authError = 'Google Sign-In is not available right now. Please try again.';
      return;
    }

    if (!window.google?.accounts?.id) {
      this.authError = 'Google Sign-In script failed to load.';
      return;
    }

    this.IsGoogleLogin = true;

    try {
      window.google.accounts.id.prompt((notification: any) => {
        if (
          notification?.isNotDisplayed?.() ||
          notification?.isSkippedMoment?.() ||
          notification?.isDismissedMoment?.()
        ) {
          this.IsGoogleLogin = false;
        }
      });
    } catch {
      this.IsGoogleLogin = false;
      this.authError = 'Unable to start Google Sign-In popup.';
    }
  }

  private async setupGoogleSignIn() {
    if (this.isGoogleInitialized) {
      return;
    }

    if (this.googleInitPromise) {
      return this.googleInitPromise;
    }

    this.googleInitPromise = (async () => {
      const clientConfig = await firstValueFrom(this.service.GetGoogleClientId());
      this.googleClientId = clientConfig?.clientId?.trim() || '';

      if (!this.googleClientId) {
        throw new Error('Missing Google client id');
      }

      await this.loadGoogleScript();
      this.initializeGoogleClient();
    })();

    try {
      await this.googleInitPromise;
    } catch (error) {
      this.googleInitPromise = null;
      throw error;
    }
  }

  private loadGoogleScript(): Promise<void> {
    if (window.google?.accounts?.id) {
      this.googleScriptLoaded = true;
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const existing = document.querySelector<HTMLScriptElement>('script[src="https://accounts.google.com/gsi/client"]');

      if (existing) {
        existing.addEventListener('load', () => {
          this.googleScriptLoaded = true;
          resolve();
        });
        existing.addEventListener('error', () => reject(new Error('Google SDK load failed')));
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        this.googleScriptLoaded = true;
        resolve();
      };
      script.onerror = () => reject(new Error('Google SDK load failed'));
      document.head.appendChild(script);
    });
  }

  private initializeGoogleClient() {
    if (this.isGoogleInitialized || !window.google?.accounts?.id) {
      return;
    }

    window.google.accounts.id.initialize({
      client_id: this.googleClientId,
      callback: (response: any) => this.handleGoogleCredential(response),
      auto_select: false,
      cancel_on_tap_outside: true,
    });

    this.isGoogleInitialized = true;
  }

  private handleGoogleCredential(response: any) {
    const idToken = response?.credential;

    if (!idToken) {
      this.IsGoogleLogin = false;
      this.authError = 'Google did not return a valid credential.';
      return;
    }

    this.service.GoogleLogin(idToken).subscribe({
      next: (res) => {
        this.IsGoogleLogin = false;
        this.handleAuthSuccess(res.token);
      },
      error: (err) => {
        this.IsGoogleLogin = false;
        this.authError =
          err?.error?.message ||
          err?.error ||
          'Google Sign-In failed. Please try again.';
      },
    });
  }

  private handleAuthSuccess(token: string) {
    sessionStorage.setItem('token', token);

    const decoded: any = jwtDecode(token);
    const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const roleId = decoded['role_id'];

    sessionStorage.setItem('role', role);
    sessionStorage.setItem('roleId', roleId);

    // Remove legacy shared storage values so one login does not override other tabs.
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('roleId');

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
  }
}
