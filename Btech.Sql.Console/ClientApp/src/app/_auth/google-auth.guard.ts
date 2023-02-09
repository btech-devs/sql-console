import {Injectable} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {Observable} from 'rxjs';
import {LocalStorageService} from '../_services/localStorageService';
import {ID_TOKEN_KEY, PATH_CONNECTION, PATH_GOOGLE_AUTHORIZATION, SESSION_TOKEN_KEY} from '../utils';
import {JwtService} from '../_services/jwt.service';

@Injectable()
export class GoogleAuthGuard implements CanActivate {

    constructor(
        private router: Router,
        private _jwtProvider: JwtService) {
    }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot)
        : Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {

        let idToken: string | null = LocalStorageService.get(ID_TOKEN_KEY);

        if (!idToken) {
            LocalStorageService.delete(SESSION_TOKEN_KEY);
            this.router.navigate([PATH_GOOGLE_AUTHORIZATION], {queryParams: {returnUrl: state.url}});
        }

        return true;
    }
}