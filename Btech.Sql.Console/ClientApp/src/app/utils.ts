import {animate, AnimationTriggerMetadata, style, transition, trigger} from '@angular/animations';
import {Routes} from '@angular/router';
import {ErrorComponent} from './components/error/error.component';
import {QueryConsoleComponent} from './components/queryConsole/queryConsole.component';
import {ConnectionComponent} from './components/connection/connection.component';
import {GoogleAuthorizationComponent} from './components/google-authorization/google-authorization.component';
import {GoogleAuthGuard} from './_auth/google-auth.guard';
import {SessionAuthGuard} from './_auth/session-auth.guard';
import {HttpContext, HttpContextToken} from '@angular/common/http';

export const ID_TOKEN_KEY = 'sql-console-id-token';
export const ACCOUNT_PICTURE_URL_KEY = 'sql-console-account-picture-url';
export const SESSION_TOKEN_KEY = 'sql-console-session-token';
export const REFRESH_TOKEN_KEY = 'sql-console-refresh-token';
export const JWT_PUBLIC_KEY_KEY = 'sql-console-jwt-public-key';

export const REFRESHED_ID_TOKEN_HEADER_KEY = 'refreshed-id-token';
export const REFRESHED_SESSION_TOKEN_HEADER_KEY = 'refreshed-session-token';
export const REFRESHED_REFRESH_TOKEN_HEADER_KEY = 'refreshed-refresh-token';
export const IDENTITY_ERROR_HEADER_KEY = 'identity-error';

export const ID_AUTH_FAILED_ERROR = 'IdAuthenticationFailed';
export const SESSION_AUTH_FAILED_ERROR = 'SessionAuthenticationFailed';

export const PATH_ERROR = 'error';
export const PATH_CONSOLE = 'console';
export const PATH_CONNECTION = 'connection';
export const PATH_GOOGLE_AUTHORIZATION = 'google-auth';

export const IDENTITY_CONTEXT_TOKEN = new HttpContextToken<number>(() => IdentityContextTokenValue.Default);

export function IdentityContext(tokenValue: number = IdentityContextTokenValue.Default) : HttpContext {
    return new HttpContext().set(IDENTITY_CONTEXT_TOKEN, tokenValue);
}

export enum IdentityContextTokenValue {
    Default,
    Id,
    Full
}

export function getRoutes(): Routes {
    return [
        {
            path: PATH_ERROR,
            component: ErrorComponent
        },
        {
            path: PATH_CONSOLE,
            component: QueryConsoleComponent,
            canActivate: [GoogleAuthGuard, SessionAuthGuard]
        },
        {
            path: PATH_CONNECTION,
            component: ConnectionComponent,
            canActivate: [GoogleAuthGuard]
        },
        {
            path: PATH_GOOGLE_AUTHORIZATION,
            component: GoogleAuthorizationComponent
        },
        {
            path: '*',
            redirectTo: PATH_CONSOLE

        },
        {
            path: '**',
            redirectTo: PATH_CONSOLE
        }
    ];
}

export function getTransitionAnimation(): AnimationTriggerMetadata {
    return trigger(
        'transitionAnimation', [
            transition(':enter', [
                style({transform: 'translate(0)', opacity: 0}),
                animate('200ms', style({transform: 'translate(0)', opacity: 1}))
            ]),
            transition(':leave', [
                style({transform: 'translate(0)', opacity: 1}),
                animate('200ms', style({transform: 'translate(0)', opacity: 0}))
            ])
        ]
    );
}

/** Sets a prototype for an object or each list element. */
export function setPrototype(o: any, prototype: object): void {
    if (Array.isArray(o))
        o.forEach(e => Object.setPrototypeOf(e, prototype));
    else
        Object.setPrototypeOf(o, prototype);
}

export function getHeaders(): { [_: string]: string } {
  return {
    'Content-Type': 'application/json'
  };
}

export class AlertStorage {
    private static readonly _errorMessageMaxLength: number = 180;
    private static readonly _infoMessageMaxLength: number = 120;
    private static readonly _errorTimeout: number = 8000; // 8 seconds
    private static readonly _infoTimeout: number = 5000; // 5 seconds

    private static _timeoutId?: NodeJS.Timeout;
    private static _error?: string;
    private static _info?: string;

    private constructor() {
    }

    // ↓ methods

    static get error(): string | undefined {
        return this._error;
    }

    static set error(value: string | undefined) {
        if (value && value.length > this._errorMessageMaxLength)
            value = value.substring(0, this._errorMessageMaxLength);

        this._error = value;
        this._info = undefined;

        if (value == undefined) {
            if (AlertStorage._timeoutId != undefined) {
                clearTimeout(AlertStorage._timeoutId);
                AlertStorage._timeoutId = undefined;
            }
        } else {
            if (AlertStorage._timeoutId != undefined)
                clearTimeout(AlertStorage._timeoutId);

            AlertStorage._timeoutId = setTimeout(
                () => this.error = undefined,
                this._errorTimeout);
        }
    }

    static get info(): string | undefined {
        return this._info;
    }

    static set info(value: string | undefined) {
        if (value && value.length > this._infoMessageMaxLength)
            value = value.substring(0, this._infoMessageMaxLength);

        this._info = value;
        this._error = undefined;

        if (value == undefined) {
            if (AlertStorage._timeoutId != undefined) {
                clearTimeout(AlertStorage._timeoutId);
                AlertStorage._timeoutId = undefined;
            }
        } else {
            if (AlertStorage._timeoutId != undefined)
                clearTimeout(AlertStorage._timeoutId);

            AlertStorage._timeoutId = setTimeout(
                () => this.info = undefined,
                this._infoTimeout);
        }
    }

    static processHttpErrorResponse(response: any): void {
        console.error(response);

        // if (response.hasOwnProperty('name') && response.name == 'HttpErrorResponse')
        if (response.hasOwnProperty('ok') && response.ok == false && response.hasOwnProperty('error'))
            AlertStorage.error = 'Please, repeat your action in a few seconds.';
        else
            AlertStorage.error = response;
    }

    // ↑ methods
}