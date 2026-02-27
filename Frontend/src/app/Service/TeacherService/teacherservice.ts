import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Teacherservice {
  constructor(private http: HttpClient) {}

  GetAllTeacher() {
    return this.http.get<any>('https://localhost:44385/api/Admin/GetAllTeacher');
  }

  UpdateTeacher(Userid: number, Data: any) {
    return this.http.put<any>(`https://localhost:44385/api/Admin/UpdateTeacher/${Userid}`, Data);
  }
 
  Deleteteacher(Userid: number) {
    return this.http.delete(`https://localhost:44385/api/Admin/DeleteTeacher/${Userid}`, {
      responseType: 'text',
    });
  }
}
