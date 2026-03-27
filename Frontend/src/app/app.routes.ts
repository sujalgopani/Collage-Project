import { Suggestion } from './suggestion/suggestion';
import { Login } from './login/login';
import { Register } from './register/register';
import { Emailverify } from './emailverify/emailverify';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { TeacherDashboard } from './teacher-dashboard/teacher-dashboard';
import { StudentDashboard } from './student-dashboard/student-dashboard';
import { Teachermain } from './teachermain/teachermain';
import { Studentmain } from './studentmain/studentmain';
import { Admindashboard } from './admindashboard/admindashboard';
import { Teacherdash } from './teacherdash/teacherdash';
import { Createcource } from './createcource/createcource';
import { Owncourse } from './owncourse/owncourse';
import { TeacherExamList } from './teacher-exam-list/teacher-exam-list';
import { Mysubscriber } from './mysubscriber/mysubscriber';
import { TeacherProfile } from './teacher-profile/teacher-profile';
import { Studentmaindash } from '../studentmaindash/studentmaindash';
import { Publishedcourses } from '../publishedcourses/publishedcourses';
import { Learningcourse } from './learningcourse/learningcourse';
import { Videostreaming } from './videostreaming/videostreaming';
import { Admincoursemanage } from './admincoursemanage/admincoursemanage';
import { Studentexam } from './studentexam/studentexam';
import { StudentExamAttempt } from './student-exam-attempt/student-exam-attempt';
import { ExamStudentResult } from './exam-student-result/exam-student-result';
import { Resultsheet } from './resultsheet/resultsheet';
import { Coursewisestudent } from './coursewisestudent/coursewisestudent';
import { Studentexamwise } from './studentexamwise/studentexamwise';
import { ManagePayment } from './manage-payment/manage-payment';
import { ManageExmas } from './manage-exmas/manage-exmas';
import { Rolemanage } from './rolemanage/rolemanage';
import { Adminprofile } from './adminprofile/adminprofile';
import { Studentsuggestion } from './studentsuggestion/studentsuggestion';
import { Teachergetsuggestion } from './teachergetsuggestion/teachergetsuggestion';
import { AdminLiveClasses } from './admin-live-classes/admin-live-classes';
import { TeacherLiveClasses } from './teacher-live-classes/teacher-live-classes';
import { StudentLiveClasses } from './student-live-classes/student-live-classes';

import { Routes } from '@angular/router';
import { AuthGuard } from './Service/guard/auth/auth-guard-guard';
import { RoleGuard } from './Service/guard/role/role-guard';

export const routes: Routes = [

  // 🌐 Public
  { path: '', component: Login },
  { path: 'login', component: Login },
  { path: 'emailvarify', component: Emailverify },
  { path: 'registration', component: Register },

  // 🔴 ADMIN
  {
    path: 'admin-dashboard',
    component: AdminDashboard,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] },
    children: [
      { path: '', component: Admindashboard },
      { path: 'Admin-Dashboard', component: Admindashboard },
      { path: 'main-teacher', component: Teachermain },
      { path: 'main-student', component: Studentmain },
      { path: 'course-manage', component: Admincoursemanage },
      { path: 'payment-manage', component: ManagePayment },
      { path: 'exams-manage', component: ManageExmas },
      { path: 'live-classes', component: AdminLiveClasses },
      { path: 'role-manage', component: Rolemanage },
      { path: 'adminprofile', component: Adminprofile },
    ],
  },

  // 🟢 TEACHER
  {
    path: 'teacher-dashboard',
    component: TeacherDashboard,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Teacher'] },
    children: [
      { path: '', component: Teacherdash },
      { path: 'create-couse', component: Createcource },
      { path: 'your-course', component: Owncourse },
      { path: 'exam-list', component: TeacherExamList },
      { path: 'my-subscriber', component: Mysubscriber },
      { path: 'profile', component: TeacherProfile },
      { path: 'studentcoursewise', component: Coursewisestudent },
      { path: 'studentexamwise', component: Studentexamwise },
      { path: 'teacherprofile', component: Adminprofile },
      { path: 'teachersuggestion', component: Teachergetsuggestion },
      { path: 'live-classes', component: TeacherLiveClasses },
    ],
  },

  // 🔵 STUDENT
  {
    path: 'student-dashboard',
    component: StudentDashboard,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Student'] },
    children: [
      { path: '', component: Studentmaindash },
      { path: 'published-courses', component: Publishedcourses },
      { path: 'learn-courses', component: Learningcourse },
      { path: 'video-serve', component: Videostreaming },
      { path: 'student-exam', component: Studentexam },
      { path: 'student-exam-attempt', component: StudentExamAttempt },
      { path: 'student-exam-result', component: ExamStudentResult },
      { path: 'student-result/:examId/:attemptId', component: Resultsheet },
      { path: 'studentprofile', component: Adminprofile },
      { path: 'studentsuggestion', component: Suggestion },
      { path: 'mysuggestion', component: Studentsuggestion },
      { path: 'live-classes', component: StudentLiveClasses },
    ],
  },

  // ❌ fallback
  { path: '**', redirectTo: 'login' },
];
