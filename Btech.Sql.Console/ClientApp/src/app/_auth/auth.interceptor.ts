import {
    HttpErrorResponse,
    HttpEvent,
    HttpHandler,
    HttpInterceptor,
    HttpRequest,
    HttpResponse,
    HttpResponseBase
} from '@angular/common/http';
import {Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {catchError, map, Observable, throwError, EMPTY} from 'rxjs';
import {LocalStorageService} from '../_services/localStorageService';
import {
    AlertStorage,
    ID_AUTH_FAILED_ERROR,
    ID_TOKEN_KEY,
    IDENTITY_CONTEXT_TOKEN,
    IDENTITY_ERROR_HEADER_KEY, IdentityContextTokenValue,
    JWT_PUBLIC_KEY_KEY,
    PATH_CONNECTION,
    PATH_GOOGLE_AUTHORIZATION,
    REFRESH_TOKEN_KEY,
    REFRESHED_ID_TOKEN_HEADER_KEY,
    REFRESHED_REFRESH_TOKEN_HEADER_KEY,
    REFRESHED_SESSION_TOKEN_HEADER_KEY,
    SESSION_AUTH_FAILED_ERROR,
    SESSION_TOKEN_KEY
} from '../utils';
import {JwtService} from '../_services/jwt.service';
import {SessionStorageService} from '../_services/sessionStorageService';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(
        private _router: Router,
        private _jwtProvider: JwtService) {
    }

    private handleError(error: any) {
        if (error instanceof HttpErrorResponse) {

            this.updateTokens(error);

            if (error.status === 401) {
                if (error.headers.has(IDENTITY_ERROR_HEADER_KEY)) {
                    switch (error.headers.get(IDENTITY_ERROR_HEADER_KEY)) {
                        case SESSION_AUTH_FAILED_ERROR:
                            SessionStorageService.clear();
                            this._router.navigate([PATH_CONNECTION]);
                            break;

                        case ID_AUTH_FAILED_ERROR:
                        default:
                            SessionStorageService.clear();
                            LocalStorageService.clear();
                            this._router.navigate([PATH_GOOGLE_AUTHORIZATION]);
                            break;
                    }
                }
            } else if (error.status === 403) {
                if (error.headers.has(IDENTITY_ERROR_HEADER_KEY)) {
                    AlertStorage.error = error.headers.get(IDENTITY_ERROR_HEADER_KEY)!;
                } else {
                    AlertStorage.error = 'Access forbidden. Please, call the administrator.';
                }
            }

            return throwError(() => {
                return error;
            });

        } else {
            return throwError(error);
        }
    }

    private updateTokens(response: HttpResponseBase): void {
        if (response.headers.has(REFRESHED_ID_TOKEN_HEADER_KEY)) {
            LocalStorageService.set(ID_TOKEN_KEY, response.headers.get(REFRESHED_ID_TOKEN_HEADER_KEY));
        }

        if (response.headers.has(REFRESHED_SESSION_TOKEN_HEADER_KEY)) {
            SessionStorageService.set(SESSION_TOKEN_KEY, response.headers.get(REFRESHED_SESSION_TOKEN_HEADER_KEY));
        }

        if (response.headers.has(REFRESHED_REFRESH_TOKEN_HEADER_KEY)) {
            SessionStorageService.set(REFRESH_TOKEN_KEY, response.headers.get(REFRESHED_REFRESH_TOKEN_HEADER_KEY));
        }
    }

    private addAuthTokenHeader(request: HttpRequest<any>, tokenKey: string, token: string): HttpRequest<any> {
        return request
            .clone({
                headers: request.headers.set(tokenKey, token)
            });
    }

    private addIdentityData(request: HttpRequest<any>)
        : { request: HttpRequest<any>, isValidIdData: boolean, isValidSessionData: boolean } {

        let isValidIdData: boolean = false;
        let isValidSessionData: boolean = false;
        let identitySchemeNumber = request.context.get(IDENTITY_CONTEXT_TOKEN) ?? IdentityContextTokenValue.Default;

        if (identitySchemeNumber > IdentityContextTokenValue.Default) {
            let idToken = LocalStorageService.get(ID_TOKEN_KEY);

            if (idToken) {
                isValidIdData = true;
                request = this.addAuthTokenHeader(request, ID_TOKEN_KEY, idToken);

                if (identitySchemeNumber > IdentityContextTokenValue.Id) {
                    let sessionToken = SessionStorageService.get(SESSION_TOKEN_KEY);
                    let refreshToken = SessionStorageService.get(REFRESH_TOKEN_KEY);

                    if (sessionToken && refreshToken) {
                        if (LocalStorageService.get(JWT_PUBLIC_KEY_KEY) != undefined) {
                            let sessionValidationResult: { isValid: boolean, isExpired: boolean } =
                                this._jwtProvider.validateToken(sessionToken);

                            if (sessionValidationResult.isValid) {
                                isValidSessionData = true;
                                request = this.addAuthTokenHeader(request, SESSION_TOKEN_KEY, sessionToken);

                                if (sessionValidationResult.isExpired) {
                                    isValidSessionData = false;

                                    let refreshValidationResult: { isValid: boolean, isExpired: boolean } =
                                        this._jwtProvider.validateToken(refreshToken);

                                    if (refreshValidationResult.isValid) {
                                        if (!refreshValidationResult.isExpired) {
                                            isValidSessionData = true;
                                            request = this.addAuthTokenHeader(request, REFRESH_TOKEN_KEY, refreshToken);
                                        } else {
                                            AlertStorage.info = `Session is expired.`;
                                        }
                                    } else {
                                        AlertStorage.error = `Session validation failed.`;
                                    }
                                }
                            }
                            else {
                                AlertStorage.error = `Session validation failed.`;
                            }
                        }
                        else {
                            AlertStorage.error = `'${JWT_PUBLIC_KEY_KEY}' was not found. Session authentication needed.`;
                        }
                    } else {
                        AlertStorage.error = `Session validation failed. Data is lost.`;
                    }
                }
                else {
                    isValidSessionData = true;
                }
            }
        }
        else {
            isValidIdData = true;
            isValidSessionData = true;
        }

        return { request: request, isValidIdData: isValidIdData, isValidSessionData: isValidSessionData };
    }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        let result: Observable<HttpEvent<any>> | null = EMPTY;
        let requestIdentityDetails = this.addIdentityData(request);

        if (requestIdentityDetails.isValidSessionData && requestIdentityDetails.isValidIdData) {
            result = next.handle(requestIdentityDetails.request)
                .pipe(
                    map((event: HttpEvent<any>) => {
                        if (event instanceof HttpResponse) {
                            this.updateTokens(event);
                        }

                        return event;
                    }))
                .pipe(catchError(this.handleError.bind(this)));
        } else {
            SessionStorageService.clear();
            let redirectPath: string = PATH_CONNECTION;

            if (!requestIdentityDetails.isValidIdData) {
                LocalStorageService.clear();
                redirectPath = PATH_GOOGLE_AUTHORIZATION;
            }

            this._router.routeReuseStrategy.shouldReuseRoute = () => false;
            this._router.onSameUrlNavigation = 'reload';
            this._router.navigate([redirectPath], {  });
        }

        return result;
    }
}