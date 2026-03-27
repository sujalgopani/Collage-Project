import { Login } from './../../login/login';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class LoginRegisterService {
  http = inject(HttpClient);

  RegisterUser(User:any){
    return this.http.post<any>('https://localhost:44385/api/Auth/register',User,{responseType:'text' as 'json'});
  }

  OtpVarify(OtpDetail:any){
    return this.http.post<any>('https://localhost:44385/api/Auth/verify-email-otp',OtpDetail);
  }

  LoginUser(LoginData : any){
    return this.http.post<any>('https://localhost:44385/api/Auth/login',LoginData);
  }

  GoogleLogin(idToken: string) {
    return this.http.post<any>('https://localhost:44385/api/Auth/google-login', { idToken });
  }

  GetGoogleClientId() {
    return this.http.get<{ clientId: string }>('https://localhost:44385/api/Auth/google-client-id');
  }



}
