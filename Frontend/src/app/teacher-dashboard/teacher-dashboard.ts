import { serverRoutes } from './../app.routes.server';
import { Component, ElementRef, Inject, OnInit, PLATFORM_ID, ViewChild } from '@angular/core';
import { Router, RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { jwtDecode } from 'jwt-decode';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { SidebarService } from '../Service/sidebar.service';
import { Adminservice } from '../Service/AdminService/adminservice';
import { ChatbotWidget } from '../chatbot-widget/chatbot-widget';

declare var bootstrap: any;
@Component({
  selector: 'app-teacher-dashboard',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, ChatbotWidget],
  templateUrl: './teacher-dashboard.html',
  styleUrl: './teacher-dashboard.css',
})
export class TeacherDashboard implements OnInit {
  isSidebarOpen = false;

  @ViewChild('logoutmodel') logoutmodel!: ElementRef;
  constructor(
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object,
    private sidebarService: SidebarService,
    private adminService: Adminservice,
  ) {}

  toggleSidebar() {
    this.sidebarService.setSidebar(!this.isSidebarOpen);
  }

  private modalInstance: any;
  username = '';
  profile: any;
  ngOnInit(): void {
    this.sidebarService.syncWithViewport();
    this.sidebarService.sidebarOpen$.subscribe(isOpen => {
      this.isSidebarOpen = isOpen;
    });

    if (isPlatformBrowser(this.platformId)) {
      const token = sessionStorage.getItem('token');

      if (token) {
        const payload = this.decodeToken(token);
        // console.log('Decoded Token:', payload);

        this.username = payload?.unique_name || payload?.sub || payload?.name;
        // console.log('Username:', this.username);
      }
    }

    this.loadProfile();
  }

  decodeToken(token: string): any {
    try {
      const payload = token.split('.')[1]; // get payload
      const decoded = atob(payload); // base64 decode
      return JSON.parse(decoded);
    } catch (error) {
      console.log('Invalid token', error);
      return null;
    }
  }
  OpenLogout() {
    this.modalInstance = new bootstrap.Modal(this.logoutmodel.nativeElement);
    this.modalInstance.show();
  }

  ConformLogout() {
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
      },
      error: () => {},
    });
  }
}
