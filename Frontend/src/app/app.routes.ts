import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Register } from './register/register';
import { Emailverify } from './emailverify/emailverify';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { TeacherDashboard } from './teacher-dashboard/teacher-dashboard';
import { StudentDashboard } from './student-dashboard/student-dashboard';
import { Teachermain } from './teachermain/teachermain';
import { Studentmain } from './studentmain/studentmain';

export const routes: Routes = [

  // public paths
  {path:'',component:Login},
  {path:'login',component:Login},
  {path:'emailvarify',component:Emailverify},
  {path:'registration',component:Register},

  // admin side
  {
    path:'admin-dashboard',
    component:AdminDashboard,
    children:[
      {path:'main-teacher',component:Teachermain},
      {path:'main-student',component:Studentmain}
    ]
  },
  // teacher side
  {
    path:'teacher-dashboard',
    component:TeacherDashboard,
  },
  // admin side
  {
    path:'student-dashboard',
    component:StudentDashboard,
  }
];

