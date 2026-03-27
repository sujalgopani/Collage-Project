import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root',
})
export class TokenService {

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  decodeToken(): any {
    const token = this.getToken();

    if (!token) return null;

    try {
      return jwtDecode(token);
    } catch (e) {
      return null;
    }
  }

  // ✅ Check token expiry
  isTokenValid(): boolean {
    const decoded = this.decodeToken();

    if (!decoded) return false;

    const currentTime = Math.floor(Date.now() / 1000);

    return decoded.exp > currentTime;
  }

  // ✅ Get role from token
  getUserRole(): string | null {
    const decoded = this.decodeToken();

    if (!decoded) return null;

    return decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
  }

  // ✅ Optional
  getUserId(): string | null {
    const decoded = this.decodeToken();
    return decoded?.sub || null;
  }
}
