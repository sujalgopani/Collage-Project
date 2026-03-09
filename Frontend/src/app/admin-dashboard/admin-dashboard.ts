import { bootstrapApplication } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild, viewChild } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
declare var bootstrap: any;
@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterOutlet,RouterLink],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard {
  constructor(private router:Router){}
  isMainOpen = false;
isStudentOpen = false;
isTeacherOpen = false;

@ViewChild('logoutmodel') logoutmodel!:ElementRef;
// Main menu toggle
toggleMainMenu() {
  this.isMainOpen = !this.isMainOpen;
}

// Student submenu
toggleStudent() {
  this.isStudentOpen = !this.isStudentOpen;
}

// Teacher submenu
toggleTeacher() {
  this.isTeacherOpen = !this.isTeacherOpen;
}


private modalInstance: any;

OpenLogout() {
  this.modalInstance = new bootstrap.Modal(this.logoutmodel.nativeElement);
  this.modalInstance.show();
}

ConformLogout(){
  if (this.modalInstance) {
    this.modalInstance.hide();
  }
 localStorage.clear();
  this.router.navigate(['/']);

}

}
