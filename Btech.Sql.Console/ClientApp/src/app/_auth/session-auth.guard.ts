import {Injectable} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {Observable} from 'rxjs';
import {PATH_CONNECTION, REFRESH_TOKEN_KEY, SESSION_TOKEN_KEY} from '../utils';
import {JwtService} from '../_services/jwt.service';
import {SessionStorageService} from '../_services/sessionStorageService';

@Injectable()
export class SessionAuthGuard implements CanActivate {

    constructor(
        private router: Router,
        private _jwtProvider: JwtService) {
    }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot)
        : Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {

        let sessionToken: string | null = SessionStorageService.get(SESSION_TOKEN_KEY);
        let refreshToken: string | null = SessionStorageService.get(REFRESH_TOKEN_KEY);

        if (!sessionToken || !this._jwtProvider.validateToken(sessionToken).isValid ||
            !refreshToken || !this._jwtProvider.validateToken(refreshToken).isValid ||
            this._jwtProvider.validateToken(refreshToken).isExpired) {
            SessionStorageService.delete(SESSION_TOKEN_KEY);
            SessionStorageService.delete(REFRESH_TOKEN_KEY);
            this.router.navigate([PATH_CONNECTION], {queryParams: {returnUrl: state.url}});
        }

        return true;
    }
}