import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Adminservice } from '../Service/AdminService/adminservice';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-adminprofile',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './adminprofile.html',
  styleUrls: ['./adminprofile.css'],
})
export class Adminprofile implements OnInit {
  profileForm!: FormGroup;
  userData: any;
  isEditMode = false;
  isProfileSaving = false;

  selectedProfileImage: File | null = null;
  profileImageError = '';
  isImageUploading = false;
  readonly maxProfileImageSizeBytes = 20 * 1024 * 1024;

  constructor(
    private service: Adminservice,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
  ) {}

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      userId: [''],
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      middleName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      username: [''],
      profileImageUrl: [''],
      roleId: [''],
      isActive: [''],
      createdAt: [''],
      lastLoginAt: [''],
      updatedAt: [''],
    });

    this.loadProfile();
  }

  loadProfile() {
    this.service.Getprofile().subscribe({
      next: (res: any) => {
        this.userData = res;
        this.profileForm.patchValue(res);
        this.selectedProfileImage = null;
        this.profileImageError = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.log('Error loading profile', err);
      },
    });
  }

  toggleEdit() {
    this.isEditMode = !this.isEditMode;
  }

  saveProfile() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isProfileSaving = true;

    const updateformobj = {
      firstName: this.profileForm.get('firstName')?.value?.trim(),
      middleName: this.profileForm.get('middleName')?.value?.trim(),
      lastName: this.profileForm.get('lastName')?.value?.trim(),
      email: this.profileForm.get('email')?.value?.trim(),
      phone: this.profileForm.get('phone')?.value?.trim(),
    };

    this.service.UpdateProfile(updateformobj).subscribe({
      next: () => {
        this.isProfileSaving = false;
        this.isEditMode = !this.isEditMode;
        this.loadProfile();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isProfileSaving = false;
        console.log(err);
      },
    });
  }

  onProfileImageSelected(event: Event) {
    this.profileImageError = '';
    this.selectedProfileImage = null;

    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.profileImageError = 'Please select a valid image file.';
      return;
    }

    if (file.size > this.maxProfileImageSizeBytes) {
      this.profileImageError = 'Profile image must be at most 20 MB.';
      return;
    }

    this.selectedProfileImage = file;
  }

  uploadProfileImage() {
    if (!this.selectedProfileImage || this.isImageUploading) {
      return;
    }

    const formData = new FormData();
    formData.append('ProfileImage', this.selectedProfileImage);
    this.isImageUploading = true;

    this.service.UploadProfileImage(formData).subscribe({
      next: () => {
        this.isImageUploading = false;
        this.loadProfile();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isImageUploading = false;
        this.profileImageError = err?.error || err?.error?.message || 'Failed to upload profile image.';
        this.cdr.detectChanges();
      },
    });
  }

  getProfileImageUrl() {
    if (this.userData?.profileImageUrl) {
      return `https://localhost:44385${this.userData.profileImageUrl}`;
    }

    return '';
  }

  get fullName() {
    const f = this.profileForm.value;
    return `${f.firstName} ${f.middleName || ''} ${f.lastName}`.trim();
  }

  isInvalid(field: string) {
    const control = this.profileForm.get(field);
    return !!control && control.touched && control.invalid;
  }
}
