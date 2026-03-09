import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-teacher-exam-list',
  imports: [CommonModule],
  templateUrl: './teacher-exam-list.html',
  styleUrl: './teacher-exam-list.css',
})
export class TeacherExamList {
 exams = [
    {
      id: 1,
      title: 'Math Unit Test',
      course: 'Mathematics',
      totalMarks: 100,
      duration: '60 min',
      date: '2026-03-05'
    },
    {
      id: 2,
      title: 'Angular Final Exam',
      course: 'Programming',
      totalMarks: 80,
      duration: '90 min',
      date: '2026-03-10'
    }
  ];

  editExam(exam: any) {
    console.log("Edit Exam:", exam);
  }

  deleteExam(id: number) {
    if (confirm("Are you sure you want to delete this exam?")) {
      this.exams = this.exams.filter(e => e.id !== id);
    }
  }
}
