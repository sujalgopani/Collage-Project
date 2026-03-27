import { firstValueFrom } from 'rxjs';
import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';

declare var bootstrap: any;

@Component({
  selector: 'app-teachermain',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './teachermain.html',
  styleUrls: ['./teachermain.css'],
})
export class Teachermain implements OnInit {
  registerForm!: FormGroup;
  editForm!: FormGroup;

  users: any[] = [];
  Isloading = false;
  IsAdd = false;
  isUpdating = false;
  SendOtp = false;
  otpValue = '';
  otpError = '';
  tempemail = '';
  searchTerm = '';

  editId: number | null = null;
  DeleteId: number | null = null;
  isDeleting = false;

  addModalInstance: any;
  editModalInstance: any;
  deleteModalInstance: any;

  @ViewChild('exampleModal') exampleModal!: ElementRef;
  @ViewChild('Editmodel') Editmodel!: ElementRef;
  @ViewChild('DeleteModal') DeleteModal!: ElementRef;

  constructor(
    private teacher_service: Teacherservice,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private route: ActivatedRoute,
  ) {}

  service = inject(LoginRegisterService);

  ngOnInit(): void {
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.minLength(3)]],
        middleName: [''],
        lastName: ['', [Validators.required, Validators.minLength(3)]],
        email: ['', [Validators.required, Validators.email]],
        phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        conformpassword: ['', Validators.required],
        role: ['', Validators.required],
        isActive: [true],
      },
      { validators: this.passwordMatchValidator },
    );

    this.editForm = this.fb.group({
      firstName: ['', Validators.required],
      middleName: [''],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      newpassword: ['', [Validators.minLength(6)]],
      isActive: [true],
    });

    this.route.queryParamMap.subscribe((params) => {
      this.searchTerm = (params.get('search') ?? '').trim();
      this.GetTeacher(this.searchTerm);
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const pass = control.get('password')?.value;
    const confirm = control.get('conformpassword')?.value;

    if (!pass || !confirm) {
      return null;
    }

    return pass === confirm ? null : { passwordMismatch: true };
  }

  GetTeacher(search = '') {
    this.Isloading = true;
    this.teacher_service.GetAllTeacher(search).subscribe({
      next: (res) => {
        this.users = res;
        this.Isloading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.Isloading = false;
      },
    });
  }

  onSearch() {
    this.GetTeacher(this.searchTerm);
  }

  openAddForm() {
    this.addModalInstance = new bootstrap.Modal(this.exampleModal.nativeElement);
    this.addModalInstance.show();
  }

  closeAddForm() {
    bootstrap.Modal.getInstance(this.exampleModal.nativeElement)?.hide();
  }

  AddUser() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.otpError = '';
    const formData = { ...this.registerForm.value };
    delete formData.conformpassword;

    this.IsAdd = true;
    this.SendOtp = false;

    this.service.RegisterUser(formData).subscribe({
      next: () => {
        this.IsAdd = false;
        this.tempemail = formData.email;
        this.SendOtp = true;
        this.GetTeacher();
        this.cdr.detectChanges();
      },
      error: () => {
        this.IsAdd = false;
        this.SendOtp = false;
      },
    });
  }

  VarifyOtp() {
    const otp = (this.otpValue || '').trim();
    this.otpError = '';

    if (!/^\d{6}$/.test(otp)) {
      this.otpError = 'Please enter a valid 6-digit OTP.';
      return;
    }

    const data = {
      email: this.registerForm.get('email')?.value,
      otp,
    };

    this.service.OtpVarify(data).subscribe({
      next: () => {
        this.SendOtp = false;
        this.otpValue = '';
        this.otpError = '';
        this.registerForm.reset();
        this.addModalInstance?.hide();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.otpError = err?.error?.message || 'OTP verification failed. Please try again.';
      },
    });
  }

  EditUser(user: any) {
    this.editId = user.userId;
    this.editForm.patchValue(user);
    this.openEditForm();
  }

  openEditForm() {
    this.editModalInstance = new bootstrap.Modal(this.Editmodel.nativeElement);
    this.editModalInstance.show();
  }

  closeEditForm() {
    bootstrap.Modal.getInstance(this.Editmodel.nativeElement)?.hide();
  }

  UpdateUser() {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isUpdating = true;
    this.teacher_service.UpdateTeacher(this.editId!, this.editForm.value).subscribe(
      () => {
        this.isUpdating = false;
        this.editModalInstance?.hide();
        this.GetTeacher();
        this.cdr.detectChanges();
      },
      () => {
        this.isUpdating = false;
        this.cdr.detectChanges();
      },
    );
  }

  deleteteacher(id: number) {
        this.deleteError = '';

    this.DeleteId = id;
    this.deleteModalInstance = new bootstrap.Modal(this.DeleteModal.nativeElement);
    this.deleteModalInstance.show();
  }

  deleteError = '';

  ConfirmDelete() {
    if (!this.DeleteId) return;

    this.isDeleting = true;
    this.deleteError = '';

    this.teacher_service.Deleteteacher(this.DeleteId).subscribe({
      next: (res: any) => {
        this.isDeleting = false;

        // this.notification.success(res?.message || 'Deleted successfully');

        this.deleteModalInstance?.hide();
        this.GetTeacher();
        this.cdr.detectChanges();
      },

      error: (err) => {
        this.isDeleting = false;
        let errorMsg = 'Cannot delete this teacher.';

        try {
          const parsed = JSON.parse(err.error);
          errorMsg = parsed.message;
        } catch {
          errorMsg = err?.error || err?.message;
        }

        this.deleteError = errorMsg;
        this.cdr.detectChanges();
        // this.notification.error(this.deleteError);
      },
    });
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    if (event.ctrlKey && event.key === 'e') {
      event.preventDefault();
      this.openAddForm();
    }
  }

  isInvalid(form: FormGroup, field: string) {
    const control = form.get(field);
    return !!control && control.touched && control.invalid;
  }
}
