import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-createcource',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './createcource.html',
  styleUrl: './createcource.css',
})
export class Createcource {

  courseForm: FormGroup;

  selectedFiles: File[] = [];
  thumbnailFile!: File;

  @ViewChild('fileInput') fileInput!: ElementRef;

  isUploading = false;

  constructor(
    private fb: FormBuilder,
    private service: Teacherservice
  ) {

    this.courseForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      fees: [0, [Validators.required, Validators.min(0)]],
    });

  }


  onFileChange(event: any) {

    if (event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }

  }


  onThumbnailChange(event: any) {

    if (event.target.files.length > 0) {
      this.thumbnailFile = event.target.files[0];
    }

  }


  onSubmit() {

    if (this.courseForm.invalid) return;

    const formData = new FormData();

    formData.append('Title', this.courseForm.value.title);
    formData.append('Description', this.courseForm.value.description);
    formData.append('StartDate', this.courseForm.value.startDate);
    formData.append('EndDate', this.courseForm.value.endDate);
    formData.append('Fees', this.courseForm.value.fees);

    if (this.thumbnailFile) {
      formData.append('ThumbailUrl', this.thumbnailFile);
    }

    this.selectedFiles.forEach(file => {
      formData.append('Files', file);
    });

    this.isUploading = true;

    this.service.CreateCourses(formData).subscribe({

      next: (res) => {

        this.isUploading = false;

        this.courseForm.reset();
        this.selectedFiles = [];
        this.thumbnailFile = undefined as any;

        this.fileInput.nativeElement.value = '';

      },

      error: (err) => {

        this.isUploading = false;
        console.error(err);

      }

    });

  }

}
