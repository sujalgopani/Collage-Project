import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { SidebarService } from '../Service/sidebar.service';
import { Adminservice } from '../Service/AdminService/adminservice';
import { forkJoin } from 'rxjs';

declare var bootstrap: any;
@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  isSidebarOpen = false;
  profile: any;
  globalSearchTerm = '';
  isGlobalSearching = false;
  isGlobalSearchOpen = false;
  globalSearchError = '';
  globalSearchResults: any = {
    query: '',
    courses: [],
    teachers: [],
    students: [],
    exams: [],
    totalMatches: 0,
  };

  constructor(
    private router: Router,
    private sidebarService: SidebarService,
    private adminService: Adminservice,
    private cdr: ChangeDetectorRef,
  ) {}

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
  isMainOpen = false;
  isStudentOpen = false;
  isTeacherOpen = false;

  @ViewChild('logoutmodel') logoutmodel!: ElementRef;
  @ViewChild('globalSearchWrapper') globalSearchWrapper?: ElementRef;

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

  onGlobalSearch() {
    const query = this.globalSearchTerm.trim();

    if (!query) {
      this.resetGlobalSearchResults();
      this.isGlobalSearchOpen = false;
      this.globalSearchError = '';
      this.cdr.detectChanges();
      return;
    }

    this.isGlobalSearching = true;
    this.globalSearchError = '';
    this.isGlobalSearchOpen = true;
    this.cdr.detectChanges();

    this.adminService.GlobalSearch(query, 5).subscribe({
      next: (res: any) => {
        this.globalSearchResults = res ?? {
          query,
          courses: [],
          teachers: [],
          students: [],
          exams: [],
          totalMatches: 0,
        };
        this.isGlobalSearching = false;
        this.isGlobalSearchOpen = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        if (err?.status === 404) {
          this.runFallbackGlobalSearch(query);
          return;
        }

        this.isGlobalSearching = false;
        this.globalSearchError =
          err?.status === 401 || err?.status === 403
            ? 'Session expired. Please login again.'
            : 'Unable to search right now. Please try again.';
        this.resetGlobalSearchResults();
        this.isGlobalSearchOpen = true;
        this.cdr.detectChanges();
      },
    });
  }

  onGlobalSearchFocus() {
    if (this.isGlobalSearching || this.hasGlobalResults() || this.globalSearchError) {
      this.isGlobalSearchOpen = true;
    }
  }

  hasGlobalResults() {
    return (
      (this.globalSearchResults?.courses?.length ?? 0) > 0 ||
      (this.globalSearchResults?.teachers?.length ?? 0) > 0 ||
      (this.globalSearchResults?.students?.length ?? 0) > 0 ||
      (this.globalSearchResults?.exams?.length ?? 0) > 0
    );
  }

  openGlobalSearchResult(type: 'course' | 'teacher' | 'student' | 'exam', item: any) {
    this.isGlobalSearchOpen = false;

    if (type === 'course') {
      this.router.navigate(['/admin-dashboard/course-manage'], {
        queryParams: { search: item?.courseId ?? this.globalSearchTerm.trim() },
      });
      return;
    }

    if (type === 'teacher') {
      const teacherSearch =
        typeof item?.fullName === 'string' && item.fullName.trim()
          ? item.fullName.trim()
          : this.globalSearchTerm.trim();

      this.router.navigate(['/admin-dashboard/main-teacher'], {
        queryParams: {
          roleId: item?.roleId ?? 2,
          search: teacherSearch || undefined,
        },
      });
      return;
    }

    if (type === 'student') {
      const studentSearch =
        typeof item?.fullName === 'string' && item.fullName.trim()
          ? item.fullName.trim()
          : this.globalSearchTerm.trim();

      this.router.navigate(['/admin-dashboard/main-student'], {
        queryParams: {
          roleId: item?.roleId ?? 3,
          search: studentSearch || undefined,
        },
      });
      return;
    }

    this.router.navigate(['/admin-dashboard/exams-manage'], {
      queryParams: { search: item?.examId ?? this.globalSearchTerm.trim() },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.isGlobalSearchOpen || !this.globalSearchWrapper?.nativeElement) {
      return;
    }

    const target = event.target as Node | null;
    if (target && !this.globalSearchWrapper.nativeElement.contains(target)) {
      this.isGlobalSearchOpen = false;
    }
  }

  private resetGlobalSearchResults() {
    this.globalSearchResults = {
      query: '',
      courses: [],
      teachers: [],
      students: [],
      exams: [],
      totalMatches: 0,
    };
  }

  private runFallbackGlobalSearch(query: string) {
    forkJoin({
      courses: this.adminService.GetAllCourses(query),
      teachers: this.adminService.GetAllTeachers(query),
      students: this.adminService.GetAllStudents(query),
      exams: this.adminService.getAllExams(),
    }).subscribe({
      next: (res: any) => {
        const term = query.toLowerCase();
        const parsedId = Number(query);
        const isNumeric = Number.isFinite(parsedId);

        const mappedCourses = (res?.courses ?? []).slice(0, 5);

        const mappedTeachers = (res?.teachers ?? []).slice(0, 5).map((u: any) => ({
          userId: u.userId,
          roleId: u.roleId ?? 2,
          fullName: `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim(),
          email: u.email,
        }));

        const mappedStudents = (res?.students ?? []).slice(0, 5).map((u: any) => ({
          userId: u.userId,
          roleId: u.roleId ?? 3,
          fullName: `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim(),
          email: u.email,
        }));

        const mappedExams = (res?.exams ?? [])
          .filter((e: any) => {
            const title = String(e?.title ?? '').toLowerCase();
            const description = String(e?.description ?? '').toLowerCase();
            const examId = Number(e?.examId);
            return (
              (isNumeric && Number.isFinite(examId) && examId === parsedId) ||
              title.includes(term) ||
              description.includes(term)
            );
          })
          .slice(0, 5)
          .map((e: any) => ({
            examId: e.examId,
            title: e.title,
            courseName: e.courseId ? `Course #${e.courseId}` : 'Course',
          }));

        this.globalSearchResults = {
          query,
          courses: mappedCourses,
          teachers: mappedTeachers,
          students: mappedStudents,
          exams: mappedExams,
          totalMatches:
            mappedCourses.length + mappedTeachers.length + mappedStudents.length + mappedExams.length,
        };

        this.isGlobalSearching = false;
        this.globalSearchError = '';
        this.isGlobalSearchOpen = true;
        this.cdr.detectChanges();
      },
      error: (fallbackErr) => {
        this.isGlobalSearching = false;
        this.resetGlobalSearchResults();
        this.globalSearchError =
          fallbackErr?.status === 401 || fallbackErr?.status === 403
            ? 'Session expired. Please login again.'
            : 'Unable to search right now. Please try again.';
        this.isGlobalSearchOpen = true;
        this.cdr.detectChanges();
      },
    });
  }

  
}
