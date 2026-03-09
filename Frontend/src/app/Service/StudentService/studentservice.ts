import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Studentservice {
  constructor(private http: HttpClient) {}

  GetPublishedCoursesWithsubscribecheck(){
    return this.http.get('https://localhost:44385/api/Student/Get-Subscribed-course');
  }

  getCourseVideos(){
    return this.http.get('')
  }
  
}
