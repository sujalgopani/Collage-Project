import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { TokenService } from '../../tokenservice';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {

  constructor(
    private router: Router,
    private tokenService: TokenService
  ) {}

  canActivate(): boolean {

    if (this.tokenService.isTokenValid()) {
      return true;
    }

    // ❌ invalid or expired
    sessionStorage.clear();
    this.router.navigate(['/login']);
    return false;
  }
}
