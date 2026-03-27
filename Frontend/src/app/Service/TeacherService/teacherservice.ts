import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable } from '@angular/core';




@Injectable({
  providedIn: 'root',
})
export class Teacherservice {
  constructor(private http: HttpClient) {}


  GetAllTeacher(search = '') {
    const params: any = {};
    if (search.trim()) {
      params.search = search.trim();
    }

    return this.http.get<any>('https://localhost:44385/api/Admin/GetAllTeacher', { params });
  }

  GetAllStudent(search = '') {
    const params: any = {};
    if (search.trim()) {
      params.search = search.trim();
    }

    return this.http.get<any>('https://localhost:44385/api/Admin/GetAllStudent', { params });
  }

  UpdateTeacher(Userid: number, Data: any) {
    return this.http.put<any>(`https://localhost:44385/api/Admin/UpdateUser/${Userid}`, Data, {
      responseType: 'text' as 'json',
    });
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

  GetOwnCourses() {
    return this.http.get('https://localhost:44385/api/Teacher/GetMyCourses/mycourses');
  }

  // Exam
  CreateExam(ExamData: any) {
    return this.http.post(
      `https://localhost:44385/api/Teacher/UploadExamFromExcel/upload-exam-excel`,
      ExamData,
    );
  }

  GetTeacherWiseExamDetail() {
    return this.http.get('https://localhost:44385/api/Exam/GetTeacherWiseCourse');
  }

  PublishResult(examId: number) {
    return this.http.put(
      `https://localhost:44385/api/Exam/PublishResult/publish-result/${examId}`,
      {},
    );
  }

  // dashboard side
  Gettotalcourses() {
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalCourses/GetTotalCourses');
  }
  GetTotalStudent() {
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalStudent/GetTotalStudent');
  }
  GetTotalExam() {
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalExam/GetTotalExam');
  }
  GetTotalEarnings() {
    return this.http.get('https://localhost:44385/api/Teacher/GetTotalEarnings/GetTotalEarnings');
  }

  getRecentExams() {
    return this.http.get(`https://localhost:44385/api/Teacher/GetRecentExams/recent-exams`);
  }

  getRecentSubscribers() {
    return this.http.get(`https://localhost:44385/api/Teacher/GetRecentSubscribers/recent-subscribers`);
  }

  //
  GetStudentCourseWise(courseId: number) {
    return this.http.get(
      `https://localhost:44385/api/Teacher/GetStudentsByCourse/course/${courseId}/students`,
    );
  }

  GetStudentByExam(examId: number) {
    return this.http.get(
      `https://localhost:44385/api/Teacher/GetStudentsByExam/exams/${examId}/students`,
    );
  }

  // course manage
  GetCourseDetail(courseId: number) {
    return this.http.get(`https://localhost:44385/api/Teacher/GetCourseDetail/${courseId}`);
  }

  MediaDeleteById(courseId: number) {
    return this.http.delete(`https://localhost:44385/api/Teacher/DeleteMedia/media/${courseId}`);
  }

  UploadMedia(courseId: number, formData: FormData) {
    return (
      this,
      this.http.post(
        `https://localhost:44385/api/Teacher/UploadMedia/media/upload/${courseId}`,
        formData,
      )
    );
  }

  MediaDelete(courseId: number) {
    return this.http.delete(`https://localhost:44385/api/Teacher/DeleteCourse/delete-course/${courseId}`);
  }

  GetLiveClasses() {
    return this.http.get<any[]>(`https://localhost:44385/api/Teacher/GetLiveClasses/live-classes`);
  }

  UploadLiveClassMaterial(liveClassId: number, formData: FormData) {
    return this.http.post<any>(
      `https://localhost:44385/api/Teacher/UploadLiveClassMaterial/live-classes/${liveClassId}/material`,
      formData,
    );
  }

  getstudentsuggestionforteacher(){
    return this.http.get(`https://localhost:44385/api/Teacher/GetTeacherSuggestions/teachergetsuggestion`);
  }

  replysuggestion(data : any){
    return this.http.post(`https://localhost:44385/api/Teacher/ReplySuggestion/reply`,data);
  }

  deleteSuggestion(id: number){
    return  this.http.delete(`https://localhost:44385/api/Teacher/DeleteSuggestion/delete/${id}`);
  }
}
