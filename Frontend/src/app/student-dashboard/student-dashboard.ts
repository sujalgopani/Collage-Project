import { Component, ElementRef, ViewChild } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

declare var bootstrap :any;
@Component({
  selector: 'app-student-dashboard',
  imports: [RouterLink,RouterOutlet],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.css',
})
export class StudentDashboard {
 @ViewChild('logoutmodel') logoutmodel!:ElementRef;
constructor(private router:Router){}
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
