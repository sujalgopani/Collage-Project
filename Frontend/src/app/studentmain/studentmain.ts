import { ChangeDetectorRef, Component, ElementRef, HostListener, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';
import { ActivatedRoute } from '@angular/router';

declare var bootstrap: any;

@Component({
  selector: 'app-studentmain',
  imports: [ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './studentmain.html',
  styleUrl: './studentmain.css',
})

export class Studentmain implements OnInit{
 registerForm!: FormGroup;
  editForm!: FormGroup;
  editModalInstance: any;
  deleteModalInstance: any;

  constructor(
    private teacher_service: Teacherservice,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private route: ActivatedRoute,
  ) {}
  users: any[] = [];
  Isloading = false;
  IsSubmitting = false;
  searchTerm = '';
  otpError = '';

  @ViewChild('exampleModal') exampleModal!: ElementRef;
  @ViewChild('Editmodel') Editmodel!: ElementRef;
  @ViewChild('DeleteModal') DeleteModal!: ElementRef;

  ngOnInit(): void {
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.minLength(3)]],
        middleName: ['', [Validators.minLength(2)]],
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
      firstName: ['', [Validators.required]],
      middleName: [''],
      lastName: ['', [Validators.required]],
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

  service = inject(LoginRegisterService);

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirm = control.get('conformpassword')?.value;

    if (!password || !confirm) {
      return null;
    }

    return password === confirm ? null : { passwordMismatch: true };
  }

  GetTeacher(search = '') {
    this.Isloading = true;
    this.teacher_service.GetAllStudent(search).subscribe({
      next: (res) => {
        this.users = res;
        this.Isloading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
      },
    });
  }

  onSearch() {
    this.GetTeacher(this.searchTerm);
  }

  showAddForm = false;
  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    if (event.ctrlKey && event.key.toLowerCase() === 'e') {
      event.preventDefault(); // 🔥 IMPORTANT (default select all stop)

      this.openAddForm();
    }
  }

  openAddForm() {
    const modal = new bootstrap.Modal(this.exampleModal.nativeElement);
    modal.show();
  }

  closeAddForm() {
    const modal = bootstrap.Modal.getInstance(this.exampleModal.nativeElement);
    modal?.hide();
  }

  SendOtp = false;
  tempemail = '';
  IsAdd = false;
  AddUser() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.otpError = '';
    const formData = { ...this.registerForm.value };
    formData.isActive = true;
    delete formData.conformpassword;
    console.log(formData);
    this.IsAdd = true;
    this.SendOtp = false;
    this.service.RegisterUser(formData).subscribe({
      next: (res) => {
        this.IsAdd = false;
        this.tempemail = formData.email;
        this.SendOtp = true;
        console.log('Done');
        this.GetTeacher();
      },
      error: (err) => {
        this.IsAdd = false;
        this.SendOtp = false;
        console.log('Err');
      },
    });
  }

  otpValue = '';
  VarifyOtp() {
    const otp = (this.otpValue || '').trim();
    this.otpError = '';

    if (!/^\d{6}$/.test(otp)) {
      this.otpError = 'Please enter a valid 6-digit OTP.';
      return;
    }

    const OtpObj = {
      email: this.registerForm.get('email')?.value,
      otp,
    };
    console.log(OtpObj);
    this.service.OtpVarify(OtpObj).subscribe({
      next: (res) => {
        console.log('Done');
        this.SendOtp = false;
        this.otpValue = '';
        this.otpError = '';
        this.closeAddForm();
        this.registerForm.reset();
      },
      error: (err) => {
        this.otpError = err?.error?.message || 'OTP verification failed. Please try again.';
        console.log('Err');
      },
    });
  }

  // edit

  openEditForm() {
    const modal = new bootstrap.Modal(this.Editmodel.nativeElement);
    modal.show();
  }

  closeEditForm() {
    const modal = bootstrap.Modal.getInstance(this.Editmodel.nativeElement);
    modal?.hide();
  }
  editId: number | null = null;
  EditUser(user: any) {
    this.editId = user.userId;

    this.editForm.patchValue({
      firstName: user.firstName,
      middleName: user.middleName,
      lastName: user.lastName,
      email: user.email,
      phone: user.phone,
      newpassword: '', // empty
      isActive: user.isActive,
    });

    this.openEditForm();
  }

  UpdateUser() {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const formData = { ...this.editForm.value };

    formData.isActive = formData.isActive ? true : false;

    this.IsSubmitting = true;
    this.teacher_service.UpdateStudent(this.editId!, formData).subscribe({
      next: (res) => {
        this.IsSubmitting = false;
        console.log('Updated Successfully');
        this.closeEditForm();
        this.GetTeacher();
      },
      error: (err) => {
        this.IsSubmitting = false;
        console.log(err);
        this.cdr.detectChanges();
      },
    });
    console.log(this.editForm.value);
  }



  // Delete Model
  DeleteId: number | null = null;
  deleteErrorMessage: string | null = null;
  isDeleting = false;

  deleteteacher(Userid: number) {
    console.log(Userid);
    this.DeleteId = Userid;
    this.deleteErrorMessage = null;
    this.isDeleting = false;
    this.deleteModalInstance = new bootstrap.Modal(this.DeleteModal.nativeElement);
    this.deleteModalInstance.show();
  }

  closeDeleteModal() {
    this.deleteModalInstance?.hide();
    this.DeleteId = null; // reset
    this.deleteErrorMessage = null;
    this.isDeleting = false;
  }

  ConfirmDelete() {
    if (!this.DeleteId || this.isDeleting) return;

    this.isDeleting = true;
    this.deleteErrorMessage = null;

    this.teacher_service.DeleteStudent(this.DeleteId).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.GetTeacher();
      },
      error: (err) => {
        this.isDeleting = false;
        if (typeof err?.error === 'string' && err.error.trim()) {
          this.deleteErrorMessage = err.error;
        } else if (err?.error?.message) {
          this.deleteErrorMessage = err.error.message;
        } else {
          this.deleteErrorMessage = 'Unable to delete this student right now.';
        }
        this.cdr.detectChanges();
      },
    });
  }

  isInvalid(form: FormGroup, field: string) {
    const control = form.get(field);
    return !!control && control.touched && control.invalid;
  }
}
