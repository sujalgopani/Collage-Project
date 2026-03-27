import { ChangeDetectorRef, Component, OnInit, RESPONSE_INIT } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-coursewisestudent',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './coursewisestudent.html',
  styleUrl: './coursewisestudent.css',
})
export class Coursewisestudent implements OnInit {
  courses: any[] = [];
  students: any[] = [];
  selectedCourseId: number | null = null;
  loadingStudents = false;
  loadingcourse = false;

  constructor(
    private service: Teacherservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.GetOwnCourses();
  }

  // ✅ Get Courses
  GetOwnCourses() {
    this.loadingcourse = true;
    this.service.GetOwnCourses().subscribe({
      next: (res: any) => {
        this.loadingcourse = false;
        this.courses = res;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.loadingcourse = false;
        this.cd.detectChanges();
      },
    });
  }

  Loadstudent(courseId: number) {
    // ✅ Toggle behavior (Show / Hide)
    if (this.selectedCourseId === courseId) {
      this.selectedCourseId = null;
      return;
    }

    this.selectedCourseId = courseId;
    this.loadingStudents = true;
    this.students = [];

    this.service.GetStudentCourseWise(courseId).subscribe({
      next: (res: any) => {
        this.students = res;
        console.log(res)
        this.loadingStudents = false;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.loadingStudents = false;
        this.cd.detectChanges();
      },
    });
  }

}
