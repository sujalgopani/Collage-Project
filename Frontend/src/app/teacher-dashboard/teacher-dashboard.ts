import { serverRoutes } from './../app.routes.server';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
declare var bootstrap: any;
@Component({
  selector: 'app-teacher-dashboard',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './teacher-dashboard.html',
  styleUrl: './teacher-dashboard.css',
})
export class TeacherDashboard implements OnInit {
  @ViewChild('logoutmodel') logoutmodel!: ElementRef;
  constructor(
    private router: Router,
  ) {}

  private modalInstance: any;

  ngOnInit(): void {

  }

  OpenLogout() {
    this.modalInstance = new bootstrap.Modal(this.logoutmodel.nativeElement);
    this.modalInstance.show();
  }

  ConformLogout() {
    if (this.modalInstance) {
      this.modalInstance.hide();
    }
    localStorage.clear();
    this.router.navigate(['/']);
  }


}
