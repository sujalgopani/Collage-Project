import { ChangeDetectorRef, Component, ElementRef, HostListener, inject, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { LoginRegisterService } from '../Service/Login-Register/login-register-service';

declare var bootstrap: any;

@Component({
  selector: 'app-studentmain',
  imports: [ReactiveFormsModule,FormsModule],
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
  ) {}
  users: any[] = [];
  Isloading = false;
  IsSubmitting = false;

  @ViewChild('exampleModal') exampleModal!: ElementRef;
  @ViewChild('Editmodel') Editmodel!: ElementRef;
  @ViewChild('DeleteModal') DeleteModal!: ElementRef;

  ngOnInit(): void {
    this.GetTeacher();
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(3)]],
      middleName: ['', [Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      password: ['', Validators.required],
      conformpassword: ['', Validators.required],
      role: ['', Validators.required],
      isActive: [true],
    });

    this.editForm = this.fb.group({
      firstName: ['', [Validators.required]],
      middleName: [''],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required]],
      newpassword: [''],
      isActive: [true],
    });
  }

  service = inject(LoginRegisterService);

  GetTeacher() {
    this.Isloading = true;
    this.teacher_service.GetAllStudent().subscribe({
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
    const formData = { ...this.registerForm.value };
    formData.isActive = true;
    delete formData.conformpassword;
    console.log(formData);
    this.IsAdd = true;
    this.SendOtp = true;
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
        console.log('Err');
      },
    });
  }

  otpValue = '';
  VarifyOtp() {
    const OtpObj = {
      email: this.registerForm.get('email')?.value,
      otp: this.otpValue,
    };
    console.log(OtpObj);
    this.service.OtpVarify(OtpObj).subscribe({
      next: (res) => {
        console.log('Done');
        this.SendOtp = false;
        this.otpValue = '';
        this.closeAddForm();
        this.registerForm.reset();
      },
      error: (err) => {
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
    if (this.editForm.invalid) return;

    const formData = { ...this.editForm.value };

    formData.isActive = formData.isActive ? true : false;

    this.teacher_service.UpdateStudent(this.editId!, formData).subscribe({
      next: (res) => {
        console.log('Updated Successfully');
        this.closeEditForm();
        this.GetTeacher();
      },
      error: (err) => {
        console.log(err);
      },
    });
    console.log(this.editForm.value);
  }



  // Delete Model
  DeleteId: number | null = null;
  deleteteacher(Userid: number) {
    console.log(Userid);
    this.DeleteId = Userid;
    this.deleteModalInstance = new bootstrap.Modal(this.DeleteModal.nativeElement);
    this.deleteModalInstance.show();
  }

  closeDeleteModal() {
    this.deleteModalInstance?.hide();
    this.DeleteId = null; // reset
  }

  ConfirmDelete() {
    if (!this.DeleteId) return;

    this.teacher_service.DeleteStudent(this.DeleteId).subscribe({
      next: () => {
        this.closeDeleteModal();
        this.GetTeacher();
      },
    });
  }
}
