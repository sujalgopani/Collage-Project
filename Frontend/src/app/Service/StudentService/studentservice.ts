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

  GetLiveClasses() {
    return this.http.get<any[]>(`https://localhost:44385/api/Student/live-classes`);
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

  ReportViolation(examId: number, data: any) {
    return this.http.post(`https://localhost:44385/api/Student/exam/${examId}/report-violation`, data);
  }

  GetAttemptExams(){
    return this.http.get("https://localhost:44385/api/Student/my-attempted-exams");
  }

  GetExamResult(examId :number,attemptId :number){
    return this.http.get(`https://localhost:44385/api/Student/exam/${examId}/result/${attemptId}`)
  }

  sendSuggestion(data: any) {
    return this.http.post(`https://localhost:44385/api/Student/suggestion/post`, data);
  }

  getmyteacher(){
    return  this.http.get(`https://localhost:44385/api/Student/my-teachers`);
  }

  getmysuggestion(){
    return this.http.get(`https://localhost:44385/api/Student/studentgetsuggestion`);
  }

  // https://localhost:44385/api/Student/studentgetsuggestion

  getdashstate(){
    return this.http.get(`https://localhost:44385/api/Student/student-dashboard`);
  }

  getcourses(){
    return this.http.get(`https://localhost:44385/api/Student/student-courses`);
  }

  getexams(){
    return this.http.get(`https://localhost:44385/api/Student/student-exams`);
  }

  Get7DayExamScore(){
    return this.http.get(`https://localhost:44385/api/Student/student/last-7-days-scores`);
  }

}
