import { Injectable } from '@angular/core';
import { Router, ActivatedRoute, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { CanActivate } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable()
export class AuthGuardService implements CanActivate {
  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    //  console.log('canActivate');
    // If user is not logged in we'll send them to the homepage
    if (!this.auth.loggedIn()) {
      // console.log('redirect to login');
      this.auth.setPostLoginUrl(state.url);
      this.router.navigate(['/login']);
      return false;
    }

    // console.log('logged in - read-only rights:' + this.auth.readOnly);
    return true;
  }
}
