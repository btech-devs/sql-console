import {animate, AnimationTriggerMetadata, style, transition, trigger} from '@angular/animations';
import {Routes} from '@angular/router';
import {ErrorComponent} from './components/error/error.component';
import {ConnectionComponent} from './components/connection/connection.component';
import {GoogleAuthorizationComponent} from './components/google-authorization/google-authorization.component';
import {GoogleAuthGuard} from './_auth/google-auth.guard';
import {SessionAuthGuard} from './_auth/session-auth.guard';
import {HttpContext, HttpContextToken} from '@angular/common/http';
import {QueryConsoleComponent} from './components/query-console/query-console.component';

/**
 * Key for storing the ID token in local storage.
 */
export const ID_TOKEN_KEY = 'sql-console-id-token';

/**
 * Key for storing the account picture URL in local storage.
 */
export const ACCOUNT_PICTURE_URL_KEY = 'sql-console-account-picture-url';

/**
 * Key for storing the session token in local storage.
 */
export const SESSION_TOKEN_KEY = 'sql-console-session-token';

/**
 * Key for storing the refresh token in local storage.
 */
export const REFRESH_TOKEN_KEY = 'sql-console-refresh-token';

/**
 * Key for storing the value indicating if the connection is static in local storage.
 */
export const IS_STATIC_CONNECTION_KEY = 'is-static-connection';

/**
 * Key for storing the JWT public key in local storage.
 */
export const JWT_PUBLIC_KEY_KEY = 'sql-console-jwt-public-key';

/**
 * Key for the refreshed ID token header.
 */
export const REFRESHED_ID_TOKEN_HEADER_KEY = 'refreshed-id-token';

/**
 * Key for the refreshed session token header.
 */
export const REFRESHED_SESSION_TOKEN_HEADER_KEY = 'refreshed-session-token';

/**
 * Key for the refreshed refresh token header.
 */
export const REFRESHED_REFRESH_TOKEN_HEADER_KEY = 'refreshed-refresh-token';

/**
 * Key for the identity error header.
 */
export const IDENTITY_ERROR_HEADER_KEY = 'identity-error';

/**
 * Error constant for failed ID authentication.
 */
export const ID_AUTH_FAILED_ERROR = 'IdAuthenticationFailed';

/**
 * Error constant for failed session authentication.
 */
export const SESSION_AUTH_FAILED_ERROR = 'SessionAuthenticationFailed';

/**
 * Path constant for the error component.
 */
export const PATH_ERROR = 'error';

/**
 * Path constant for the query console component.
 */
export const PATH_CONSOLE = 'console';

/**
 * Path constant for the connection component.
 */
export const PATH_CONNECTION = 'connection';

/**
 * Path constant for the Google authorization component.
 */
export const PATH_GOOGLE_AUTHORIZATION = 'google-auth';

/**
 * Token for identity context in HTTP requests.
 */
export const IDENTITY_CONTEXT_TOKEN = new HttpContextToken<number>(() => IdentityContextTokenValue.Default);

/**
 * Creates an `HttpContext` with the specified token value.
 * @param tokenValue - The value of the identity context token.
 * @returns The created `HttpContext`.
 */
export function IdentityContext(tokenValue: number = IdentityContextTokenValue.Default): HttpContext {
    return new HttpContext().set(IDENTITY_CONTEXT_TOKEN, tokenValue);
}

/**
 * Enum for the possible values of the identity context token.
 */
export enum IdentityContextTokenValue {
    Default,
    Id,
    Full
}

/**
 * Returns the routes configuration for the application.
 * @returns The routes configuration.
 */
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

/**
 * Returns the animation trigger metadata for transition animations.
 * @returns The animation trigger metadata.
 */
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

/**
 * Sets a prototype for an object or each list element.
 * @param o - The object or list to set the prototype for.
 * @param prototype - The prototype object.
 */
export function setPrototype(o: any, prototype: object): void {
    if (Array.isArray(o))
        o.forEach(e => Object.setPrototypeOf(e, prototype));
    else
        Object.setPrototypeOf(o, prototype);
}

/**
 * Returns the headers object for HTTP requests.
 * @returns The headers object.
 */
export function getHeaders(): { [_: string]: string } {
    return {
        'Content-Type': 'application/json'
    };
}

/**
 * Formats the given size in bytes to a human-readable string.
 * @param size - The size in bytes.
 * @returns The formatted size string.
 */
export function formatBytes(size: number): string {
    const units = ['B', 'KB', 'MB', 'GB'];

    let length = 0;

    while (size >= 1024) {
        length++;
        size = size / 1024;
    }

    return (size.toFixed(size < 10 && size > 0 ? 1 : 0) + ' ' + units[length]);
}

/**
 * Formats the given duration in milliseconds to a human-readable string.
 * @param duration - The duration in milliseconds.
 * @returns The formatted duration string.
 */
export function formatMilliSeconds(duration: number): string {
    const units = ['ms', 's', 'm'];

    let length = 0;

    if (duration > 1000) {
        duration /= 1000;
        length++;
    }

    if (duration > 60) {
        duration /= 60;
        length++;
    }

    return (duration.toFixed(duration < 10 && duration > 0 ? 1 : 0) + units[length]);
}

/**
 * Array of separators for data import/export.
 */
export const separators: {
    name: string,
    value: string,
    extension: string,
    mimeType: string
}[] = [
    {
        name: 'Comma (,)',
        value: ',',
        extension: 'csv',
        mimeType: 'text/csv'
    },
    {
        name: 'Tab (\t)',
        value: '\t',
        extension: 'tsv',
        mimeType: 'text/tab-separated-values'
    },
    {
        name: 'Semicolon (;)',
        value: ';',
        extension: 'ssv',
        mimeType: 'text/csv'
    }
];

/**
 * Utility class for storing error and information messages.
 */
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