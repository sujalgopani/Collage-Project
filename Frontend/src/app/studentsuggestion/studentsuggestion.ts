import { ChangeDetectorRef, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../Service/notification.service';

@Component({
  selector: 'app-studentsuggestion',
  imports: [CommonModule, RouterLink],
  templateUrl: './studentsuggestion.html',
  styleUrl: './studentsuggestion.css',
})
export class Studentsuggestion {
  suggestions: any[] = [];
  Isloading = false;

  constructor(
    private service: Studentservice,
    private cdr : ChangeDetectorRef,
    private notification: NotificationService,
  ) {}

  ngOnInit(): void {
    this.loadSuggestions();
  }

  loadSuggestions() {
    this.Isloading = true;

    this.service.getmysuggestion().subscribe({
      next: (res: any) => {
        const raw = Array.isArray(res) ? res : [];
        this.suggestions = raw.map((item: any) => {
          const teacherName = (
            item?.teacherName ||
            item?.TeacherName ||
            item?.teacherUsername ||
            item?.TeacherUsername ||
            ''
          )
            .toString()
            .trim();

          const status = (item?.status || item?.Status || 'Pending').toString().trim();
          const reply = (item?.reply || item?.Reply || '').toString().trim();

          return {
            ...item,
            teacherName: teacherName || 'Teacher',
            status: status || 'Pending',
            reply,
          };
        });
        this.Isloading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.notification.error(
          err?.error?.message ||
            err?.error ||
            'Unable to load your suggestions right now. Please try again.',
        );
        this.Isloading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
