import { ChangeDetectorRef, Component } from '@angular/core';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-learningcourse',
  imports: [CommonModule, RouterLink],
  templateUrl: './learningcourse.html',
  styleUrl: './learningcourse.css',
})
export class Learningcourse {
  constructor(
    private service: Studentservice,
    private cd: ChangeDetectorRef,
  ) {}



  courses: any[] = [];
  Isloading = false;

  ngOnInit(): void {
    this.loadcourse();
  }

  loadcourse() {
    this.Isloading = true;
    this.service.GetPublishedCoursesWithsubscribecheck().subscribe({
      next: (res: any) => {
        this.Isloading = false;
        const allCourses = Array.isArray(res) ? res : [];
        this.courses = allCourses.filter((c: any) => this.isSubscribedCourse(c) && this.isPublishedCourse(c));
        this.cd.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  private isSubscribedCourse(course: any): boolean {
    return course?.isSubscribed === true || course?.issubscribed === true;
  }

  private isPublishedCourse(course: any): boolean {
    return course?.ispublished === true || course?.isPublished === true;
  }
}
