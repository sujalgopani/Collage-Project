import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-mysubscriber',
  imports: [CommonModule],
  templateUrl: './mysubscriber.html',
  styleUrl: './mysubscriber.css',
})
export class Mysubscriber {
 subscribers = [
    {
      id: 1,
      name: 'Rahul Patel',
      email: 'rahul@gmail.com',
      course: 'Angular Mastery',
      enrollDate: '2026-02-20',
      progress: 75,
      status: 'Active'
    },
    {
      id: 2,
      name: 'Priya Shah',
      email: 'priya@gmail.com',
      course: 'Advanced Mathematics',
      enrollDate: '2026-02-15',
      progress: 100,
      status: 'Completed'
    }
  ];
}
