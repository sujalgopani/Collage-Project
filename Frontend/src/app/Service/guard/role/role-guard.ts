import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { TokenService } from '../../tokenservice';

@Injectable({
  providedIn: 'root',
})
export class RoleGuard implements CanActivate {

  constructor(
    private router: Router,
    private tokenService: TokenService
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {

    const userRole = this.tokenService.getUserRole();
    const allowedRoles = route.data['roles'] as Array<string>;

    console.log('ROLE FROM TOKEN:', userRole);

    if (userRole && allowedRoles?.includes(userRole)) {
      return true;
    }

    // ❌ unauthorized
    this.router.navigate(['/login']);
    return false;
  }
}
