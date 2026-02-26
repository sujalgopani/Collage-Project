import { Component, OnInit } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

@Component({
  selector: 'app-teachermain',
  imports: [],
  templateUrl: './teachermain.html',
  styleUrl: './teachermain.css',
})
export class Teachermain implements OnInit {
  constructor(private teacher_service: Teacherservice) {}
  users = [];

  ngOnInit(): void {
    this.GetTeacher(); // 🔥 auto call
  }

  GetTeacher() {
    this.teacher_service.GetAllTeacher().subscribe({
      next: (res) => {
        console.log(res);
        this.users = res; // 🔥 IMPORTANT
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
}
