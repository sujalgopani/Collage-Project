import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Adminservice } from '../Service/AdminService/adminservice';

@Component({
  selector: 'app-teacher-profile',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './teacher-profile.html',
  styleUrl: './teacher-profile.css',
})
export class TeacherProfile implements OnInit {
  isEditMode = false;
  profileForm!: FormGroup;
  userData: any;
  isProfileSaving = false;

  selectedProfileImage: File | null = null;
  profileImageError = '';
  isImageUploading = false;
  readonly maxProfileImageSizeBytes = 20 * 1024 * 1024;

  constructor(
    private fb: FormBuilder,
    private service: Adminservice,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      middleName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      username: [''],
      profileImageUrl: [''],
      createdAt: [''],
      lastLoginAt: [''],
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
        console.log('Error loading teacher profile', err);
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

    const payload = {
      firstName: this.profileForm.get('firstName')?.value?.trim(),
      middleName: this.profileForm.get('middleName')?.value?.trim(),
      lastName: this.profileForm.get('lastName')?.value?.trim(),
      email: this.profileForm.get('email')?.value?.trim(),
      phone: this.profileForm.get('phone')?.value?.trim(),
    };

    this.service.UpdateProfile(payload).subscribe({
      next: () => {
        this.isProfileSaving = false;
        this.isEditMode = false;
        this.loadProfile();
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

    return '/avatar-placeholder.png';
  }

  getDisplayName() {
    const firstName = this.userData?.firstName || '';
    const middleName = this.userData?.middleName || '';
    const lastName = this.userData?.lastName || '';
    return `${firstName} ${middleName} ${lastName}`.trim();
  }

  isInvalid(field: string) {
    const control = this.profileForm.get(field);
    return !!control && control.touched && control.invalid;
  }
}
