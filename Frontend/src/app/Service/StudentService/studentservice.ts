import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Studentservice {
  constructor(private http: HttpClient) {}

  GetPublishedCoursesWithsubscribecheck() {
    return this.http.get('https://localhost:44385/api/Student/Get-Subscribed-course');
  }

  GetCheckAccess(courseId: number) {
    return this.http.get(`https://localhost:44385/api/Student/course/${courseId}/access`);
  }

  GetCourseById(CourseId: number) {
    return this.http.get(`https://localhost:44385/api/Student/GetCourseById?courseId=${CourseId}`);
  }

  GetCourseVideoById(courseId: number): Observable<any> {
    return this.http.get(`https://localhost:44385/api/Student/course/${courseId}/videos`);
  }

  // exam

  GetAllExams(){
    return this.http.get('https://localhost:44385/api/Student/my-exams');
  }

  GetStartExam(examId: number){
    return this.http.post(`https://localhost:44385/api/Student/exam/${examId}/start`,{});
  }

  SubmitExam(examId : number,ExamData : any){
    return this.http.post(`https://localhost:44385/api/Student/exam/${examId}/submit`,ExamData);
  }

  GetAttemptExams(){
    return this.http.get("https://localhost:44385/api/Student/my-attempted-exams");
  }

  GetExamResult(examId :number,attemptId :number){
    return this.http.get(`https://localhost:44385/api/Student/exam/${examId}/result/${attemptId}`)
  }
  
}
