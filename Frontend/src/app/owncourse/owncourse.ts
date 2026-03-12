import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component } from '@angular/core';
import { inject, PLATFORM_ID } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-owncourse',
  imports: [CommonModule],
  templateUrl: './owncourse.html',
  styleUrl: './owncourse.css',
})
export class Owncourse {
  courses: any[] = [];
  isLoading = true;
  private platformId = inject(PLATFORM_ID);

  constructor(private teacherService: Teacherservice) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      this.isLoading = false;
      return;
    }

    this.loadCourses();
  }

  loadCourses() {
    this.teacherService.GetOwnCourses().subscribe({
      next: (res: any) => {
        this.courses = res;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      },
    });
  }
}
