import {Injectable} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {Observable} from 'rxjs';
import {PATH_CONNECTION, REFRESH_TOKEN_KEY, SESSION_TOKEN_KEY} from '../utils';
import {JwtService} from '../_services/jwt.service';
import {SessionStorageService} from '../_services/sessionStorageService';

/**
 * Guard that checks if the user's session is valid and authenticated before accessing a route.
 * If the session is invalid or expired, it redirects the user to the connection page.
 */
@Injectable()
export class SessionAuthGuard implements CanActivate {

    constructor(
        private router: Router,
        private _jwtProvider: JwtService) {
    }

    /**
     * Checks if the user's session is valid and authenticated before allowing access to the specified route.
     * @param route The activated route snapshot.
     * @param state The router state snapshot.
     * @returns A boolean or UrlTree indicating whether the user's session is valid or should be redirected.
     */
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