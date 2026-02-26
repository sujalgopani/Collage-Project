import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

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

}
