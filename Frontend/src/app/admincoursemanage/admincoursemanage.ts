import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Adminservice } from '../Service/AdminService/adminservice';
import { CommonModule } from '@angular/common';

declare var bootstrap: any;

@Component({
  selector: 'app-admincoursemanage',
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './admincoursemanage.html',
  styleUrl: './admincoursemanage.css',
})
export class Admincoursemanage implements OnInit {
  constructor(
    private cdr: ChangeDetectorRef,
    private service: Adminservice,
  ) {}
  Isloading = false;
  courses: any[] = [];
  Ispublishing = false;

  ngOnInit(): void {
    this.GetAllCoures();
  }

  GetAllCoures() {
    this.courses = [];
    this.Isloading = true;
    this.service.GetAllCourses().subscribe({
      next: (res: any) => {
        this.Isloading = false;
        console.log(res);
        this.courses = res;
        console.log(res);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.Isloading = false;
        console.log(err);
        this.cdr.detectChanges();
      },
    });
  }

  PublishCourse(courseId: number) {
    this.Ispublishing = true;
    console.log(courseId);
    this.service.Publishcourse(courseId).subscribe({
      next: (res) => {
        this.Ispublishing = false;
        console.log(res);
        this.GetAllCoures();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.Ispublishing = false;
        console.log(err);
        this.cdr.detectChanges();
      },
    });
  }
}
