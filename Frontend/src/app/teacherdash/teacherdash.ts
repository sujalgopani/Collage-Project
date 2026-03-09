import { Component } from '@angular/core';

@Component({
  selector: 'app-teacherdash',
  imports: [],
  templateUrl: './teacherdash.html',
  styleUrl: './teacherdash.css',
})
export class Teacherdash {
// Mock Data
  stats = [
    { label: 'Total Courses', value: 12, icon: 'book' },
    { label: 'Active Students', value: 450, icon: 'people' },
    { label: 'Monthly Earnings', value: '$2,400', icon: 'payments' }
  ];

  courses = [
    { id: 1, title: 'Advanced Angular Patterns', students: 120, status: 'Published', revenue: 1200 },
    { id: 2, title: 'Web Security 101', students: 85, status: 'Published', revenue: 850 },
    { id: 3, title: 'Database Design', students: 0, status: 'Draft', revenue: 0 }
  ];

  displayedColumns: string[] = ['title', 'students', 'status', 'revenue', 'actions'];

  constructor() {}

  ngOnInit(): void {}

  publishCourse(id: number) {
    console.log('Publishing course:', id);
  }
}
