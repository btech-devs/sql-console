import {Component, ViewContainerRef} from '@angular/core';
import {
    ACCOUNT_PICTURE_URL_KEY,
    AlertStorage,
    getTransitionAnimation, ID_TOKEN_KEY, IS_STATIC_CONNECTION_KEY,
    PATH_CONNECTION,
    PATH_GOOGLE_AUTHORIZATION,
    SESSION_TOKEN_KEY
} from './utils';
import {LocalStorageService} from './_services/localStorageService';
import {Router} from '@angular/router';
import {SessionStorageService} from './_services/sessionStorageService';
import {GoogleAuthService} from './_services/google-auth.service';
import {map} from 'rxjs';
import {Response} from './_models/responses/base/response';
import {ConnectionService} from './_services/connection.service';
import {ConfirmModalService} from './components/confirm-modal/confirm-modal.service';

/**
 * Root component of the application.
 */
@Component({
    selector: 'app-root',
    animations: [getTransitionAnimation()],
    templateUrl: './app.component.html',
    styleUrls: ['../assets/main.less']
})
export class AppComponent {
    private _photoUrl: string | null;
    private _accountAuthenticated: boolean;
    private _sessionAuthenticated: boolean;
    private _isExecuting: boolean;

    constructor(
        public viewContainerRef: ViewContainerRef,
        private _router: Router,
        private _googleAuthService: GoogleAuthService,
        private _connectionService: ConnectionService,
        private _confirmService: ConfirmModalService) {

        this._photoUrl = null;
        this._accountAuthenticated = false;
        this._sessionAuthenticated = false;
        this._isExecuting = false;
    }

    /**
     * Gets the value indicating if there is an ongoing execution.
     */
    get isExecuting(): boolean {
        return this._isExecuting ?? false;
    }

    /**
     * Gets the value indicating if the connection is static.
     */
    get isStaticConnection(): boolean {
        return SessionStorageService.get(IS_STATIC_CONNECTION_KEY) == 'true'
    }

    /**
     * Sets the value indicating if there is an ongoing execution.
     */
    set isExecuting(value: boolean) {
        this._isExecuting = value;
    }

    /**
     * Gets the URL of the account's photo.
     */
    get photoUrl(): string | null {
        if (this._photoUrl == null)
            this._photoUrl = LocalStorageService.get(ACCOUNT_PICTURE_URL_KEY);

        return this._photoUrl;
    }

    /**
     * Gets the value indicating if the account is authenticated.
     */
    get accountAuthenticated(): boolean {
        this._accountAuthenticated = LocalStorageService.get(ID_TOKEN_KEY) != undefined;

        return this._accountAuthenticated;
    }

    /**
     * Gets the value indicating if the session is authenticated.
     */
    get sessionAuthenticated(): boolean {
        this._sessionAuthenticated = SessionStorageService.get(SESSION_TOKEN_KEY) != undefined;

        return this._sessionAuthenticated;
    }

    /**
     * Gets the error message from the AlertStorage service.
     */
    get errorMessage(): string | undefined {
        return AlertStorage.error;
    }

    /**
     * Sets the error message in the AlertStorage service.
     */
    set errorMessage(value: string | undefined) {
        AlertStorage.error = value;
    }

    /**
     * Gets the information message from the AlertStorage service.
     */
    get infoMessage(): string | undefined {
        return AlertStorage.info;
    }

    /**
     * Sets the information message in the AlertStorage service.
     */
    set infoMessage(value: string | undefined) {
        AlertStorage.info = value;
    }

    /**
     * Logs out the user.
     */
    logOut(): void {
        this.isExecuting = true;

        this._googleAuthService
            .closeSession()
            .subscribe(next => {
                LocalStorageService.clear();
                SessionStorageService.clear();
                this.isExecuting = false;
                this._router.navigate([PATH_GOOGLE_AUTHORIZATION]);
            });
    }

    /**
     * Duplicates the current connection by opening a new tab with the same URL and copying the session token if available.
     */
    duplicateConnection(): void {
        let newTab: Window | null = window.open(this._router.url, '_blank');

        let sessionToken: string | null = SessionStorageService.get(SESSION_TOKEN_KEY);

        if (sessionToken)
            newTab?.sessionStorage.setItem(SESSION_TOKEN_KEY, sessionToken);
    }

    /**
     * Opens a new connection in a new tab, removing any existing session token.
     */
    newConnection(): void {
        let newTab = window.open(PATH_CONNECTION, '_blank');
        newTab?.sessionStorage.removeItem(SESSION_TOKEN_KEY);
    }

    /**
     * Changes the connection instance by closing the current connection on all tabs and navigating to the connection path.
     * If the user confirms the action, the current connection is closed on the server.
     */
    changeInstance(): void {
        let complete: Function = () => {
            SessionStorageService.clear();
            this._router.navigate([PATH_CONNECTION]);
        };

        this._confirmService.confirm('Close current connection on all tabs?')
            .subscribe(result => {
                if (result) {
                    this.isExecuting = true;

                    this._connectionService
                        .close()
                        .pipe(
                            map((event: Response<undefined>) => {
                                if (event.errorMessage == undefined) {
                                    complete();
                                } else {
                                    AlertStorage.error = event.errorMessage;
                                }

                                return event;
                            }))
                        .subscribe(next => {
                            this.isExecuting = false;
                        });
                } else {
                    complete();
                }
            });
    }
}