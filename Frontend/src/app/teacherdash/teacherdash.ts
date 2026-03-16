import { ChangeDetectorRef, Component } from '@angular/core';
import { Teacherservice } from '../Service/TeacherService/teacherservice';

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
    { label: 'Monthly Earnings', value: '$2,400', icon: 'payments' },
  ];

  courses = [
    {
      id: 1,
      title: 'Advanced Angular Patterns',
      students: 120,
      status: 'Published',
      revenue: 1200,
    },
    { id: 2, title: 'Web Security 101', students: 85, status: 'Published', revenue: 850 },
    { id: 3, title: 'Database Design', students: 0, status: 'Draft', revenue: 0 },
  ];

  displayedColumns: string[] = ['title', 'students', 'status', 'revenue', 'actions'];
  totalcourse: any;
  totalStudents:any;
  totalexam: any;
  totalEarning: any;

  constructor(private service: Teacherservice,private cd : ChangeDetectorRef) {}

  ngOnInit(): void {
    this.Gettotalcourses();
    this.GetTotalEarnings();
    this.GetTotalExam();
    this.GetTotalStudent();
  }

  publishCourse(id: number) {
    console.log('Publishing course:', id);
  }

  Gettotalcourses() {
    this.service.Gettotalcourses().subscribe({
      next: (res: any) => {
        console.log('total course ', res);
        this.totalcourse = res.totalCourses;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
  GetTotalStudent() {
    this.service.GetTotalStudent().subscribe({
      next: (res: any) => {
        console.log('total Student ', res);
        this.totalStudents = res.totalStudents;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
  GetTotalExam() {
    this.service.GetTotalExam().subscribe({
      next: (res: any) => {
        console.log('total GetTotalExamExam ', res);
        this.totalexam = res.totalexam;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
  GetTotalEarnings() {
    this.service.GetTotalEarnings().subscribe({
      next: (res: any) => {
        console.log('total Eaning ', res);
        this.totalEarning = res.totalEarning;
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
      },
    });
  }
}
