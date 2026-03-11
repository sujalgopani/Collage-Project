import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Adminservice {
  constructor(private http: HttpClient) {}

  GetAllCourses() {
    return this.http.get('https://localhost:44385/api/Admin/GetAllCourses');
  }

  Publishcourse(courseId: number) {
    return this.http.post(`https://localhost:44385/api/Admin/PublishCourse`, courseId);
  }
}
