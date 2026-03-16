import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Teacherservice {
  constructor(private http: HttpClient) {}

  GetAllTeacher() {
    return this.http.get<any>('https://localhost:44385/api/Admin/GetAllTeacher');
  }

  GetAllStudent() {
    return this.http.get<any>('https://localhost:44385/api/Admin/GetAllStudent');
  }

  UpdateTeacher(Userid: number, Data: any) {
    return this.http.put<any>(`https://localhost:44385/api/Admin/UpdateUser/${Userid}`, Data);
  }
  UpdateStudent(Userid: number, Data: any) {
    return this.http.put<any>(`https://localhost:44385/api/Admin/UpdateStudent/${Userid}`, Data);
  }

  Deleteteacher(Userid: number) {
    return this.http.delete(`https://localhost:44385/api/Admin/DeleteUser/${Userid}`, {
      responseType: 'text',
    });
  }
  DeleteStudent(Userid: number) {
    return this.http.delete(`https://localhost:44385/api/Admin/DeleteStudent/${Userid}`, {
      responseType: 'text',
    });
  }
  CreateCourses(formData: FormData) {
    return this.http.post<any>('https://localhost:44385/api/Teacher/CreateCourse/create', formData);
  }

  GetOwnCourses(){
    return this.http.get('https://localhost:44385/api/Teacher/GetMyCourses/mycourses');
  }


  // Exam
  CreateExam(ExamData : any){
    return this.http.post(`https://localhost:44385/api/Teacher/UploadExamFromExcel/upload-exam-excel`,ExamData);
  }

  GetTeacherWiseExamDetail(){
    return this.http.get("https://localhost:44385/api/Exam/GetTeacherWiseCourse");
  }

  PublishResult(examId: number){
    return this.http.put(`https://localhost:44385/api/Exam/PublishResult/publish-result/${examId}`,{},{ responseType: 'text' });
  }


  // dashboard side
  Gettotalcourses(){
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalCourses/GetTotalCourses');
  }
  GetTotalStudent(){
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalStudent/GetTotalStudent');
  }
  GetTotalExam(){
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalExam/GetTotalExam');
  }
  GetTotalEarnings(){
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalEarnings/GetTotalEarnings');
  }
}
