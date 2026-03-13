import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-resultsheet',
  imports: [CommonModule],
  templateUrl: './resultsheet.html',
  styleUrl: './resultsheet.css',
})
export class Resultsheet {
exam = {
    examId: 17,
    attemptId: 105,
    examName: "Artificial Intelligence & Machine Learning",
    studentName: "Nirbhay Patel",
    courseName: "AI & ML Master Course",
    conductedBy: "ExamNest",
    username: "nirbhay123"
  };

  subjects = [
    {
      subjectName: "Machine Learning Basics",
      passingMarks: 40,
      obtainedMarks: 78,
      percentage: 78,
      grade: "A"
    },
    {
      subjectName: "Artificial Intelligence",
      passingMarks: 40,
      obtainedMarks: 65,
      percentage: 65,
      grade: "B"
    },
    {
      subjectName: "Deep Learning",
      passingMarks: 40,
      obtainedMarks: 72,
      percentage: 72,
      grade: "A"
    },
    {
      subjectName: "Neural Networks",
      passingMarks: 40,
      obtainedMarks: 60,
      percentage: 60,
      grade: "B"
    }
  ];

  totalMarks = 275;
  percentage = 68.75;
  resultGrade = "First Class";

}
