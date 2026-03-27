import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Adminservice {
  constructor(private http: HttpClient) {}

  GetAllCourses(search = '') {
    const params: any = {};
    if (search.trim()) {
      params.search = search.trim();
    }

    return this.http.get('https://localhost:44385/api/Admin/GetAllCourses', { params });
  }

  GetAllTeachers(search = '') {
    const params: any = {};
    if (search.trim()) {
      params.search = search.trim();
    }

    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetAllTeacher', { params });
  }

  GetAllStudents(search = '') {
    const params: any = {};
    if (search.trim()) {
      params.search = search.trim();
    }

    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetAllStudent', { params });
  }

  GlobalSearch(query: string, take = 5) {
    const params: any = { q: query.trim(), take };
    return this.http.get<any>('https://localhost:44385/api/Admin/GlobalSearch', { params });
  }

  GetSuggestions(status = '') {
    const params: any = {};
    if (status.trim()) {
      params.status = status.trim();
    }

    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetSuggestions/suggestions', {
      params,
    });
  }

  Publishcourse(courseId: number) {
    return this.http.post(`https://localhost:44385/api/Admin/PublishCourse`, courseId);
  }

  DeleteCourseById(courseId: number) {
    return this.http.post(
      `https://localhost:44385/api/Admin/DeleteCourseWiseId/courses/${courseId}/delete`,
      {},
    );
  }

  getcoursebyid(courseId: number) {
    return this.http.get(`https://localhost:44385/api/Admin/GetCourseByid/CourseById/${courseId}`);
  }

  GetLiveClasses(courseId?: number) {
    const params: any = {};
    if (courseId && courseId > 0) {
      params.courseId = courseId;
    }

    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetLiveClasses', { params });
  }

  CreateLiveClass(data: any) {
    return this.http.post<any>('https://localhost:44385/api/Admin/CreateLiveClass', data);
  }

  DeleteLiveClass(id: number) {
    return this.http.delete<any>(`https://localhost:44385/api/Admin/DeleteLiveClass/${id}`);
  }

  GetAllPayments() {
    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetAllPayments/payments');
  }

  GetPaymentDetail(id: number) {
    return this.http.get<any>(`https://localhost:44385/api/Admin/GetPaymentDetail/payments/${id}`);
  }

  CheckPayment(paymentId: string) {
    return this.http.get<any>(
      `https://localhost:44385/api/Admin/CheckPayment/payments/check/${paymentId}`,
    );
  }

  // exams
  getAllExams() {
    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetAllExams/exams');
  }

  deleteExam(id: number) {
    return this.http.delete<any>(`https://localhost:44385/api/Admin/DeleteExam/exams/${id}`);
  }

  getStudentsByExam(id: number) {
    return this.http.get<any[]>(
      `https://localhost:44385/api/Admin/GetStudentsByExam/exams/${id}/students`,
    );
  }

  // roles

  getRoles() {
    return this.http.get<any[]>('https://localhost:44385/api/Admin/GetRoles');
  }

  createRole(data: any) {
    return this.http.post('https://localhost:44385/api/Admin/CreateRole', data);
  }

  updateRole(id: number, data: any) {
    return this.http.put(`https://localhost:44385/api/Admin/UpdateRole/${id}`, data);
  }

  deleteRole(id: number) {
    return this.http.delete(`https://localhost:44385/api/Admin/DeleteRole/${id}`);
  }

  // dash
  GetDahsboardData() {
    return this.http.get(`https://localhost:44385/api/Admin/GetDashboardData/dashboarddata`);
  }

  GetAvgExamDetails() {
    return this.http.get(`https://localhost:44385/api/Admin/GetTopExams/top-exams`);
  }

  Getprofile() {
    return this.http.get('https://localhost:44385/api/Admin/GetUserProfile/MyProfile');
  }

  GetCourseWiseEarning(){
    return this.http.get(`https://localhost:44385/api/Admin/GetCourseEarnings/course-earnings`);
  }

  UpdateProfile(data: any) {
    return this.http.post(`https://localhost:44385/api/Admin/UpdateProfile/profile/update`, data);
  }

  UploadProfileImage(formData: FormData) {
    return this.http.post(
      `https://localhost:44385/api/Admin/UploadProfileImage/profile/upload-image`,
      formData,
    );
  }
}
