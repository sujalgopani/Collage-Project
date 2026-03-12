import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
declare var bootstrap: any;
@Component({
  selector: 'app-admindashboard',
  imports: [CommonModule],
  templateUrl: './admindashboard.html',
  styleUrl: './admindashboard.css',
})
export class Admindashboard {
@ViewChild('logoutmodel') logoutmodel!: ElementRef;
  private modalInstance: any;

  stats = [
    { title: 'Total Students', value: '12,450', percent: '12', isUp: true },
    { title: 'Active Exams', value: '48', percent: '5', isUp: true },
    { title: 'Passed Today', value: '890', percent: '2', isUp: false },
    { title: 'Avg Score', value: '76%', percent: '8', isUp: true }
  ];

  exams = [
    { name: 'Final Mathematics', category: 'Science', date: '20 Oct 2023', status: 'Live', statusClass: 'bg-success' },
    { name: 'UI/UX Principles', category: 'Design', date: '21 Oct 2023', status: 'Pending', statusClass: 'bg-warning text-dark' },
    { name: 'React Advanced', category: 'IT', date: '19 Oct 2023', status: 'Completed', statusClass: 'bg-secondary' }
  ];

  constructor(private router: Router) {}

  OpenLogout() {
    this.modalInstance = new bootstrap.Modal(this.logoutmodel.nativeElement);
    this.modalInstance.show();
  }

  ConformLogout() {
    if (this.modalInstance) {
      this.modalInstance.hide();
    }

    // Cleanup backdrop manually to prevent login page "freeze"
    const backdrop = document.querySelector('.modal-backdrop');
    if (backdrop) backdrop.remove();
    document.body.classList.remove('modal-open');

    localStorage.clear();
    this.router.navigate(['/']);
  }
}
