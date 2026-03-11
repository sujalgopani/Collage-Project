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
}
