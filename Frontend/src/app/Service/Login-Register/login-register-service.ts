import { Login } from './../../login/login';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { userInfo } from 'os';

@Injectable({
  providedIn: 'root',
})
export class LoginRegisterService {
  http = inject(HttpClient);

  RegisterUser(User:any){
    return this.http.post<any>('https://localhost:44385/api/Auth/register',User);
  }

  OtpVarify(OtpDetail:any){
    return this.http.post<any>('https://localhost:44385/api/Auth/verify-email-otp',OtpDetail);
  }

  LoginUser(LoginData : any){
    return this.http.post<any>('',LoginData);
  }


}
