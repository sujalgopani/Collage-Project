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
import { Routes } from '@angular/router';

export const routes: Routes = [
  // public paths
  { path: '', component: Login },
  { path: 'login', component: Login },
  { path: 'emailvarify', component: Emailverify },
  { path: 'registration', component: Register },

  // admin side
  {
    path: 'admin-dashboard',
    component: AdminDashboard,
    children: [
      { path: '', component: Admindashboard },
      { path: 'Admin-Dashboard', component: Admindashboard },
      { path: 'main-teacher', component: Teachermain },
      { path: 'main-student', component: Studentmain },
      { path: 'course-manage', component: Admincoursemanage },
    ],
  },
  // teacher side
  {
    path: 'teacher-dashboard',
    component: TeacherDashboard,
    children: [
      { path: '', component: Teacherdash },
      { path: 'create-couse', component: Createcource },
      { path: 'your-course', component: Owncourse },
      { path: 'exam-list', component: TeacherExamList },
      { path: 'my-subscriber', component: Mysubscriber },
      { path: 'profile', component: TeacherProfile },
    ],
  },
  // admin side
  {
    path: 'student-dashboard',
    component: StudentDashboard,
    children: [
      { path: '', component: Studentmaindash },
      { path: 'published-courses', component: Publishedcourses },
      { path: 'learn-courses', component: Learningcourse },
      { path: 'video-serve', component: Videostreaming },
      { path: 'student-exam', component: Studentexam },
      {path: 'student-exam-attempt',component: StudentExamAttempt},
      {path: 'student-exam-result',component: ExamStudentResult},
      {path: 'student-result/:examId/:attemptId',component: Resultsheet}
    ],
  },
];
