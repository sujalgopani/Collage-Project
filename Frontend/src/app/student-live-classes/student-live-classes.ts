import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { Studentservice } from '../Service/StudentService/studentservice';

@Component({
  selector: 'app-student-live-classes',
  imports: [CommonModule],
  templateUrl: './student-live-classes.html',
  styleUrl: './student-live-classes.css',
})
export class StudentLiveClasses implements OnInit, OnDestroy {
  liveClasses: any[] = [];
  isLoading = false;
  errorMessage = '';

  private readonly refreshIntervalMs = 15000;
  private refreshSubscription?: Subscription;

  constructor(
    private studentService: Studentservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadLiveClasses();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadLiveClasses(showLoader = true) {
    if (showLoader) {
      this.isLoading = true;
      this.errorMessage = '';
    }

    this.studentService.GetLiveClasses().subscribe({
      next: (res: any) => {
        this.liveClasses = Array.isArray(res) ? res : [];
        this.isLoading = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load live classes right now.';
        this.cd.detectChanges();
      },
    });
  }

  getStatus(item: any): string {
    const now = new Date();
    const start = new Date(item.startAt);
    const end = new Date(item.endAt);

    if (now >= start && now <= end) return 'Live Now';
    if (now < start) return 'Upcoming';
    return 'Completed';
  }

  getMaterialFileUrl(path: string | null | undefined): string {
    if (!path) return '';
    return path.startsWith('http') ? path : `https://localhost:44385${path}`;
  }

  private startAutoRefresh() {
    this.refreshSubscription = interval(this.refreshIntervalMs).subscribe(() => {
      this.loadLiveClasses(false);
    });
  }
}
