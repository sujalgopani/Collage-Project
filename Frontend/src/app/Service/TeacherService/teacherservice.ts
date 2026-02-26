import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Teacherservice {
  constructor(private http:HttpClient){}

  GetAllTeacher(){
    return this.http.get<any>("https://localhost:44385/api/Admin/GetAllTeacher");
  }
}
