import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
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

  constructor(private teacherService: Teacherservice) {}

  ngOnInit(): void {
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
