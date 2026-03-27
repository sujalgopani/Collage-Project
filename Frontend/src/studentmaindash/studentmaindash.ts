import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Studentservice } from '../app/Service/StudentService/studentservice';
import { forkJoin } from 'rxjs';
import { Chart } from 'chart.js/auto';

import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-studentmaindash',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './studentmaindash.html',
  styleUrl: './studentmaindash.css',
})
export class Studentmaindash implements OnInit {
  constructor(
    private service: Studentservice,
    private cdr: ChangeDetectorRef,
  ) {}

  username = '';

  totalCourses = 0;
  activeCourses = 0;
  totalExams = 0;
  avgScore = 0;

  courses: any[] = [];
  exams: any[] = [];
  weeklyScores: any[] = [];

  isLoadingCourses = true;
  isLoadingExams = true;

  chart: any;

  ngOnInit(): void {
    this.getUsername();
    this.loadDashboard();
    this.loadCourses();
    this.loadExams();
    this.loadWeeklyScores(); // ✅ NEW
  }

  // ✅ USERNAME FROM TOKEN
  getUsername() {
    const token = localStorage.getItem('token');

    if (token) {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.username = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
    }
  }

  // ✅ DASHBOARD STATS
  loadDashboard() {
    this.service.getdashstate().subscribe({
      next: (res: any) => {
        this.totalCourses = res.totalCourses;
        this.activeCourses = res.activeCourses;
        this.totalExams = res.totalExams;
        this.avgScore = res.avgScore;

        this.createChart();

        this.cdr.detectChanges();
      },
      error: (err) => console.log(err),
    });
  }

  // ✅ COURSES
  loadCourses() {
    this.isLoadingCourses = true;

    this.service.getcourses().subscribe({
      next: (res: any) => {
        this.courses = res;
        this.isLoadingCourses = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.isLoadingCourses = false;
      },
    });
  }

  // ✅ EXAMS
  loadExams() {
    this.isLoadingExams = true;

    this.service.getexams().subscribe({
      next: (res: any) => {
        this.exams = res;
        this.isLoadingExams = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.isLoadingExams = false;
      },
    });
  }

  loadWeeklyScores() {
    this.service.Get7DayExamScore().subscribe({
      next: (res: any) => {
        this.weeklyScores = this.generateLast7DaysData(res);
        this.createChart(); // ✅ create chart AFTER data

        this.cdr.detectChanges();
      },
      error: (err) => console.log(err),
    });
  }

  // ✅ CHART
  createChart() {
    if (this.chart) {
      this.chart.destroy();
    }

    // ✅ Prepare data
    const labels = this.weeklyScores.map((x) =>
      new Date(x.date).toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
      }),
    );

    const scores = this.weeklyScores.map((x) => x.score);

    this.chart = new Chart('studentChart', {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Score',
            data: scores,
            borderColor: '#4f46e5',
            backgroundColor: 'rgba(79, 70, 229, 0.1)',
            fill: true,
            tension: 0.4,
            pointRadius: 5,
            pointBackgroundColor: '#4f46e5',
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              stepSize: 1, // optional
            },
          },
          x: {
            grid: { display: false },
          },
        },
      },
    });
  }

  generateLast7DaysData(apiData: any[]) {
    const result: any[] = [];

    for (let i = 6; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);

      // ✅ FILTER all records for same date
      const sameDayRecords = apiData.filter(
        (x) => new Date(x.date).toDateString() === date.toDateString(),
      );

      // ✅ SUM scores
      const totalScore = sameDayRecords.reduce((sum, item) => sum + (item.score || 0), 0);

      result.push({
        date: date,
        score: totalScore,
      });
    }

    return result;
  }
}
