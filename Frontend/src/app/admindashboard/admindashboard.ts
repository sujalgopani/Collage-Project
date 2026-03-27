import { CommonModule, isPlatformBrowser } from '@angular/common';
import Chart from 'chart.js/auto';

import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  Inject,
  OnInit,
  PLATFORM_ID,
  ViewChild,
  AfterViewInit,
} from '@angular/core';

import { Router, RouterLink } from '@angular/router';
import { Adminservice } from '../Service/AdminService/adminservice';
import { jwtDecode } from 'jwt-decode';

declare var bootstrap: any;

@Component({
  selector: 'app-admindashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admindashboard.html',
  styleUrl: './admindashboard.css',
})
export class Admindashboard implements OnInit, AfterViewInit {
  @ViewChild('logoutmodel') logoutmodel!: ElementRef;
  private modalInstance: any;

  username!: string; // ✅ no default value
  chart: any;
  courseChart: any;

  stats: any[] = [];
  examsdetail: any[] = [];

  isLoading: boolean = true;
  isloadexamdetail: boolean = true;

  constructor(
    private router: Router,
    private service: Adminservice,
    private cd: ChangeDetectorRef,
    @Inject(PLATFORM_ID) private platformId: Object,
  ) {}

  // ✅ ONLY data load here
  ngOnInit() {
    this.loadDashboard();
    this.loadTopExams();
  }

  ngAfterViewInit() {
    this.username = this.getUsername() || '';
  }

  // ================= LOGOUT =================
  OpenLogout() {
    this.modalInstance = new bootstrap.Modal(this.logoutmodel.nativeElement);
    this.modalInstance.show();
  }

  ConformLogout() {
    if (this.modalInstance) this.modalInstance.hide();

    const backdrop = document.querySelector('.modal-backdrop');
    if (backdrop) backdrop.remove();
    document.body.classList.remove('modal-open');

    sessionStorage.clear();
    localStorage.clear();

    this.router.navigate(['/']);
  }

  // ================= USER =================
  getUsername(): string {
    const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';

    if (isPlatformBrowser(this.platformId)) {
      try {
        const token = sessionStorage.getItem('token');
        if (!token) return '';

        const decoded: any = jwtDecode(token);
        return decoded?.[NAME_CLAIM] || '';
      } catch {
        return '';
      }
    }
    this.cd.detectChanges();
    return '';
  }

  getShortName(name: string): string {
    if (!name) return '';
    const parts = name.trim().split(' ');
    return parts.length === 1
      ? parts[0].substring(0, 2).toUpperCase()
      : (parts[0][0] + parts[1][0]).toUpperCase();
  }

  // ================= DASHBOARD =================
  loadDashboard() {
    this.isLoading = true;

    this.service.GetDahsboardData().subscribe({
      next: (res: any) => {
        this.stats = [
          { title: 'Students', value: res.totalStudents, percent: 10, isUp: true },
          { title: 'Teachers', value: res.totalTeachers, percent: 5, isUp: true },
          { title: 'Exams', value: res.totalExams, percent: 8, isUp: true },
          { title: 'Submitted', value: res.submittedAttempts, percent: 12, isUp: true },
          { title: 'Pending', value: res.pendingAttempts, percent: 3, isUp: false },
          {
            title: 'Earnings',
            value: '₹' + this.formatNumber(res.totalEarnings),
            percent: 15,
            isUp: true,
          },
          { title: 'Courses', value: res.totalcourse, percent: 15, isUp: true },
          { title: 'Published Courses', value: res.publishedcourse, percent: 15, isUp: true },
        ];

        this.isLoading = false;
        this.cd.detectChanges();

        setTimeout(() => this.loadCourseChart(), 50);
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  formatNumber(value: number): string {
    if (value >= 1_000_000) return (value / 1_000_000).toFixed(1).replace('.0', '') + 'M';
    if (value >= 1_000) return (value / 1_000).toFixed(1).replace('.0', '') + 'K';
    return value ? value.toString() : '0';
  }

  // ================= TOP EXAMS =================
  loadTopExams() {
    this.isloadexamdetail = true;

    this.service.GetAvgExamDetails().subscribe({
      next: (res: any) => {
        this.examsdetail = res;
        this.isloadexamdetail = false;

        this.cd.detectChanges();
        setTimeout(() => this.loadChart(), 50);
      },
      error: () => {
        this.isloadexamdetail = false;
      },
    });
  }

  // ================= CHARTS =================
  loadChart() {
    this.service.GetCourseWiseEarning().subscribe((data:any) => {
      const labels = data.map((x: { title: any; }) => x.title);
      const earnings = data.map((x: { totalEarning: any; }) => x.totalEarning);

      if (this.chart) {
        this.chart.destroy();
      }

      const ctx: any = document.getElementById('courseChart');

      const gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 200);
      gradient.addColorStop(0, 'rgba(54, 162, 235, 0.5)');
      gradient.addColorStop(1, 'rgba(54, 162, 235, 0.02)');

      this.chart = new Chart(ctx, {
        type: 'line',
        data: {
          labels,
          datasets: [
            {
              data: earnings,
              borderColor: '#36A2EB',
              backgroundColor: gradient,
              fill: true,
              tension: 0.4,

              // 🔥 make it minimal
              pointRadius: 2,
              borderWidth: 2,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false, // ✅ IMPORTANT for small height

          plugins: {
            legend: { display: false }, // 🔥 hide legend
            tooltip: {
              enabled: true,
            },
          },

          scales: {
            x: {
              grid: { display: false }, // 🔥 remove grid
              ticks: {
                maxTicksLimit: 4, // 🔥 fewer labels
                font: { size: 10 },
              },
            },
            y: {
              grid: {
                color: '#eee', // light grid
              },
              ticks: {
                font: { size: 10 },
              },
              beginAtZero: true,
            },
          },
        },
      });
    });
  }
  loadCourseChart() {
    const data = [5000, 8000, 3000];

    if (this.courseChart) this.courseChart.destroy();

    this.courseChart = new Chart('courseChart', {
      type: 'doughnut',
      data: {
        labels: ['Angular', 'React', 'Node'],
        datasets: [
          {
            data,
            backgroundColor: ['#3498db', '#2ecc71', '#f1c40f'],
          },
        ],
      },
    });
  }
}
