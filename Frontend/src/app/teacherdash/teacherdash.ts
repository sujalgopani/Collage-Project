import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';
import { CommonModule } from '@angular/common';
import { Chart } from 'chart.js/auto';
import { forkJoin } from 'rxjs';

import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-teacherdash',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './teacherdash.html',
  styleUrl: './teacherdash.css',
})
export class Teacherdash implements OnInit {
  recentExams: any[] = [];
  recentSubscribers: any[] = [];

  isLoadingExams = true;
  isLoadingSubscribers = true;

  totalcourse: any = 0;
  totalStudents: any = 0;
  totalexam: any = 0;
  totalEarning: any = 0;

  chart: any;
  username = '';

  constructor(
    private service: Teacherservice,
    private cd: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
    this.loadRecentExams();
    this.loadRecentSubscribers();
    this.getUsernameFromToken();
  }

  // ✅ GET ALL DATA TOGETHER (BEST PRACTICE)
  loadDashboardData() {
    forkJoin({
      courses: this.service.Gettotalcourses(),
      students: this.service.GetTotalStudent(),
      exams: this.service.GetTotalExam(),
      earnings: this.service.GetTotalEarnings(),
    }).subscribe({
      next: (res: any) => {
        this.totalcourse = res.courses.totalCourses;
        this.totalStudents = res.students.totalStudents;
        this.totalexam = res.exams.totalexam;
        this.totalEarning = res.earnings.totalEarning;

        this.createChart();

        this.cd.detectChanges();
      },
      error: (err) => console.log(err),
    });
  }

  // ✅ CHART
  createChart() {
    if (this.chart) {
      this.chart.destroy();
    }
    this.chart = new Chart('dashboardChart', {
      type: 'line',
      data: {
        labels: ['Courses', 'Students', 'Exams', 'Earnings'],
        datasets: [
          {
            label: 'Platform Performance',
            data: [this.totalcourse, this.totalStudents, this.totalexam, this.totalEarning],
            borderColor: '#3498db',
            backgroundColor: 'rgba(52, 152, 219, 0.1)',
            fill: true,
            tension: 0.4,
            pointBackgroundColor: '#3498db',
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
            pointRadius: 5,
            pointHoverRadius: 7
          },
        ],
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            display: false
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: {
              display: false
            }
          },
          x: {
            grid: {
              display: false
            }
          }
        }
      },
    });
  }

  // ✅ TOKEN USERNAME
  getUsernameFromToken() {
    const token = sessionStorage.getItem('token');

    if (token) {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.username = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
    }
  }

  // ✅ RECENT EXAMS
  loadRecentExams() {
    this.isLoadingExams = true;

    this.service.getRecentExams().subscribe({
      next: (res: any) => {
        this.recentExams = res;
        this.isLoadingExams = false;
        this.cd.detectChanges();
      },
      error: () => (this.isLoadingExams = false),
    });
  }

  // ✅ RECENT SUBSCRIBERS
  loadRecentSubscribers() {
    this.isLoadingSubscribers = true;

    this.service.getRecentSubscribers().subscribe({
      next: (res: any) => {
        this.recentSubscribers = res;
        this.isLoadingSubscribers = false;
        this.cd.detectChanges();
      },
      error: () => (this.isLoadingSubscribers = false),
    });
  }

  getShortName(name: string): string {
    if (!name) return '';

    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase();
  }
}
