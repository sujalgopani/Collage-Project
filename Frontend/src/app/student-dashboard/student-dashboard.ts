import { Component, ElementRef, ViewChild, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarService } from '../Service/sidebar.service';
import { Adminservice } from '../Service/AdminService/adminservice';
import { ChatbotWidget } from '../chatbot-widget/chatbot-widget';

declare var bootstrap :any;
@Component({
  selector: 'app-student-dashboard',
  imports: [RouterLink, RouterOutlet, RouterLinkActive, CommonModule, ChatbotWidget],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.css',
})
export class StudentDashboard implements OnInit {
  isSidebarOpen = false;
  profile: any;

  @ViewChild('logoutmodel') logoutmodel!:ElementRef;
  constructor(
    private router:Router,
    private sidebarService: SidebarService,
    private adminService: Adminservice,
    private cdr : ChangeDetectorRef
  ){}

  toggleSidebar() {
    this.sidebarService.setSidebar(!this.isSidebarOpen);
  }

  ngOnInit() {
    this.sidebarService.syncWithViewport();
    this.sidebarService.sidebarOpen$.subscribe(isOpen => {
      this.isSidebarOpen = isOpen;
    });

    this.loadProfile();
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
 sessionStorage.removeItem('token');
 sessionStorage.removeItem('role');
 sessionStorage.removeItem('roleId');
 localStorage.removeItem('token');
 localStorage.removeItem('role');
 localStorage.removeItem('roleId');
 this.router.navigate(['/']);

}

loadProfile() {
  this.adminService.Getprofile().subscribe({
    next: (res: any) => {
      this.profile = res;
      this.cdr.detectChanges();
    },
    error: () => {},
  });
}
}
