import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Register } from './register/register';
import { Emailverify } from './emailverify/emailverify';

export const routes: Routes = [
  {path:'',component:Login},
  {path:'login',component:Login},
  {path:'emailvarify',component:Emailverify},
  {path:'registration',component:Register}
];
