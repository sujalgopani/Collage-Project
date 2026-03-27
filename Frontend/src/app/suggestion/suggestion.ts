import { CommonModule } from '@angular/common';
import { Studentservice } from './../Service/StudentService/studentservice';
import { ChangeDetectorRef, Component } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../Service/notification.service';

@Component({
  selector: 'app-suggestion',
  imports: [ReactiveFormsModule, FormsModule, CommonModule, RouterLink],
  templateUrl: './suggestion.html',
  styleUrl: './suggestion.css',
})
export class Suggestion {
  suggestionForm!: FormGroup;
  teachers: any[] = [];
  isSubmitted = false;
  isLoading = false;
  loadError = '';
  submitError = '';

  constructor(
    private fb: FormBuilder,
    private service: Studentservice,
    private notification: NotificationService,
    private cdr : ChangeDetectorRef
  ) {}

  noWhitespaceValidator(control: AbstractControl): ValidationErrors | null {
    const value = (control.value || '').toString();
    if (!value) {
      return null;
    }

    return value.trim().length === 0 ? { whitespace: true } : null;
  }

  ngOnInit(): void {
    this.suggestionForm = this.fb.group({
      teacherId: [null, Validators.required],
      title: [
        '',
        [
          Validators.required,
          Validators.minLength(5),
          Validators.maxLength(100),
          this.noWhitespaceValidator,
        ],
      ],
      message: [
        '',
        [
          Validators.required,
          Validators.minLength(10),
          Validators.maxLength(1000),
          this.noWhitespaceValidator,
        ],
      ],
    });

    this.loadTeachers();
  }

  loadTeachers() {
    this.loadError = '';
    this.service.getmyteacher().subscribe({
      next: (res: any) => {
        const rawTeachers = Array.isArray(res) ? res : [];
        this.teachers = rawTeachers
          .map((t: any) => ({
            userId: Number(t.userId ?? t.UserId),
            name: (t.name ?? t.Name ?? '').toString().trim(),
          }))
          .filter((t: any) => Number.isFinite(t.userId) && t.userId > 0 && !!t.name);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.teachers = [];
          this.loadError =
          err?.error || err?.error?.message || 'Unable to load teachers right now.';
          this.cdr.detectChanges();
      },
    });
  }

  get f() {
    return this.suggestionForm.controls;
  }

  submit() {
    this.isSubmitted = true;
    this.submitError = '';

    if (this.suggestionForm.invalid) {
      this.suggestionForm.markAllAsTouched();
      return;
    }

    const payload = {
      teacherId: Number(this.suggestionForm.value.teacherId),
      title: (this.suggestionForm.value.title || '').trim(),
      message: (this.suggestionForm.value.message || '').trim(),
    };

    if (!Number.isFinite(payload.teacherId) || payload.teacherId <= 0) {
      this.suggestionForm.get('teacherId')?.setErrors({ required: true });
      this.suggestionForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    this.service.sendSuggestion(payload).subscribe({
      next: () => {
        this.notification.success('Suggestion sent successfully.');
        this.suggestionForm.reset({
          teacherId: null,
          title: '',
          message: '',
        });
        this.isSubmitted = false;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.submitError =
          err?.error?.errors?.TeacherId?.[0] ||
          err?.error?.errors?.Message?.[0] ||
          err?.error ||
          err?.error?.message ||
          'Unable to submit feedback right now. Please try again.';
        this.isLoading = false;
      },
    });
  }
}
