import {Injectable} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {Observable} from 'rxjs';
import {LocalStorageService} from '../_services/localStorageService';
import {ID_TOKEN_KEY, PATH_CONNECTION, PATH_GOOGLE_AUTHORIZATION, SESSION_TOKEN_KEY} from '../utils';
import {JwtService} from '../_services/jwt.service';

/**
 * Guard that checks if the user is authorized to access a route based on the presence of a Google ID token.
 * If the token is missing, it redirects the user to the Google authorization page.
 */
@Injectable()
export class GoogleAuthGuard implements CanActivate {

    constructor(
        private router: Router,
        private _jwtProvider: JwtService) {
    }

    /**
     * Checks if the user is authorized to access the specified route.
     * @param route The activated route snapshot.
     * @param state The router state snapshot.
     * @returns A boolean or UrlTree indicating whether the user is authorized or should be redirected.
     */
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