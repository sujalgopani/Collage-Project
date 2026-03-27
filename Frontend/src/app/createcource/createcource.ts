import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import {
  AbstractControl,
  FormGroup,
  FormBuilder,
  ValidationErrors,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-createcource',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './createcource.html',
  styleUrl: './createcource.css',
})
export class Createcource implements OnInit {
  courseForm: FormGroup;
  readonly maxUploadSizeBytes = 20 * 1024 * 1024;

  selectedFiles: File[] = [];
  thumbnailFile!: File;
  thumbnailPreview: string | null = null;
  videoPreviews: string[] = [];

  thumbnailError: string = '';
  fileErrors: string[] = [];
  todayDate: string = '';


  @ViewChild('fileInput') fileInput!: ElementRef;

  isUploading = false;

  constructor(
    private fb: FormBuilder,
    private service: Teacherservice,
  ) {
    this.courseForm = this.fb.group(
      {
        title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description: [
          '',
          [Validators.required, Validators.minLength(10), Validators.maxLength(2000)],
        ],
        startDate: ['', Validators.required],
        startTime: ['', Validators.required],
        endDate: ['', Validators.required],
        endTime: ['', Validators.required],
        fees: [null, [Validators.required, Validators.min(0)]],
      },
      { validators: this.dateRangeValidator },
    );
  }
  ngOnInit(): void {
      const today = new Date();
      this.todayDate = today.toISOString().split('T')[0];
      this.courseForm.patchValue({
        startTime: '09:00',
        endTime: '17:00',
      });
  }

  dateRangeValidator(control: AbstractControl): ValidationErrors | null {
    const startDate = control.get('startDate')?.value;
    const startTime = control.get('startTime')?.value;
    const endDate = control.get('endDate')?.value;
    const endTime = control.get('endTime')?.value;

    if (!startDate || !startTime || !endDate || !endTime) return null;

    const startDateTime = new Date(`${startDate}T${startTime}:00`);
    const endDateTime = new Date(`${endDate}T${endTime}:00`);

    if (Number.isNaN(startDateTime.getTime()) || Number.isNaN(endDateTime.getTime())) {
      return { invalidDateRange: true };
    }

    return endDateTime > startDateTime ? null : { invalidDateRange: true };
  }

  futureDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;

    const selectedDate = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return selectedDate < today ? { pastDate: true } : null;
  }

  // ✅ Thumbnail Validation
  onThumbnailChange(event: any) {
    this.thumbnailError = '';

    const file = event.target.files[0];

    if (!file) return;

    if (file.size <= 0 || file.size > this.maxUploadSizeBytes) {
      this.thumbnailError = 'Thumbnail must be greater than 0 and at most 20 MB.';
      this.thumbnailFile = undefined as any;
      return;
    }

    this.thumbnailFile = file;
    this.thumbnailPreview = URL.createObjectURL(file);
  }

  // ✅ Video Validation
  onFileChange(event: any) {
    this.fileErrors = [];
    this.selectedFiles = [];
    this.videoPreviews.forEach((url) => URL.revokeObjectURL(url));
    this.videoPreviews = [];

    const files = Array.from(event.target.files) as File[];

    files.forEach((file) => {
      if (file.size <= 0 || file.size > this.maxUploadSizeBytes) {
        this.fileErrors.push(`${file.name} must be between 0 and 20 MB.`);
      } else {
        this.selectedFiles.push(file);
        this.videoPreviews.push(URL.createObjectURL(file));
      }
    });
  }

  removeVideo(index: number) {
    // remove preview URL
    const url = this.videoPreviews[index];
    if (url) {
      URL.revokeObjectURL(url); // cleanup memory
    }

    // remove from arrays
    this.videoPreviews.splice(index, 1);
    this.selectedFiles.splice(index, 1);
  }

  removeThumbnail() {
    if (this.thumbnailPreview) {
      URL.revokeObjectURL(this.thumbnailPreview);
    }

    this.thumbnailPreview = null;
    this.thumbnailFile = undefined as any;
  }

  // ✅ Submit
  onSubmit() {
    // mark all fields touched
    this.courseForm.markAllAsTouched();

    if (this.courseForm.invalid || this.thumbnailError || this.fileErrors.length > 0) {
      return;
    }

    const startDateTime = this.buildDateTime(
      this.courseForm.value.startDate,
      this.courseForm.value.startTime,
    );
    const endDateTime = this.buildDateTime(this.courseForm.value.endDate, this.courseForm.value.endTime);

    if (!startDateTime || !endDateTime) {
      return;
    }

    const formData = new FormData();

    formData.append('Title', this.courseForm.value.title);
    formData.append('Description', this.courseForm.value.description);
    formData.append('StartDate', startDateTime);
    formData.append('EndDate', endDateTime);
    formData.append('Fees', this.courseForm.value.fees);

    if (this.thumbnailFile) {
      formData.append('ThumbailUrl', this.thumbnailFile);
    }

    this.selectedFiles.forEach((file) => {
      formData.append('Files', file);
    });

    this.isUploading = true;

    this.service.CreateCourses(formData).subscribe({
      next: () => {
        this.isUploading = false;

        this.courseForm.reset();
        this.courseForm.patchValue({
          startTime: '09:00',
          endTime: '17:00',
        });
        this.selectedFiles = [];
        this.thumbnailFile = undefined as any;
        this.thumbnailError = '';
        this.fileErrors = [];
        this.thumbnailPreview = null;
        this.videoPreviews = [];
        if (this.fileInput?.nativeElement) {
          this.fileInput.nativeElement.value = '';
        }
      },

      error: (err) => {
        this.isUploading = false;
        console.error(err);
      },
    });
  }

  private buildDateTime(dateValue: string, timeValue: string): string | null {
    if (!dateValue || !timeValue) {
      return null;
    }

    const combinedValue = `${dateValue}T${timeValue}:00`;
    const parsed = new Date(combinedValue);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    return combinedValue;
  }
}
