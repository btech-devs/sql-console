import {Component, ViewChild} from '@angular/core';
import {
    ACCOUNT_PICTURE_URL_KEY,
    AlertStorage,
    getTransitionAnimation, ID_TOKEN_KEY,
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
import {ConfirmModalComponent} from './components/confirmModal/confirmModal.component';

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
    @ViewChild('confirmModalComponent') confirmModalComponent!: ConfirmModalComponent;

    constructor(
        private _router: Router,
        private _googleAuthService: GoogleAuthService,
        private _connectionService: ConnectionService) {
        this._photoUrl = null;
        this._accountAuthenticated = false;
        this._sessionAuthenticated = false;
        this._isExecuting = false;
    }

    get isExecuting(): boolean {
        return this._isExecuting ?? false;
    }

    set isExecuting(value : boolean) {
        this._isExecuting = value;
    }

    get photoUrl(): string | null {
        if (this._photoUrl == null)
            this._photoUrl = LocalStorageService.get(ACCOUNT_PICTURE_URL_KEY);

        return this._photoUrl;
    }

    get accountAuthenticated(): boolean {
        this._accountAuthenticated = LocalStorageService.get(ID_TOKEN_KEY) != undefined;

        return this._accountAuthenticated;
    }

    get sessionAuthenticated(): boolean {
        this._sessionAuthenticated = SessionStorageService.get(SESSION_TOKEN_KEY) != undefined;

        return this._sessionAuthenticated;
    }

    get errorMessage(): string | undefined {
        return AlertStorage.error;
    }

    set errorMessage(value: string | undefined) {
        AlertStorage.error = value;
    }

    get infoMessage(): string | undefined {
        return AlertStorage.info;
    }

    set infoMessage(value: string | undefined) {
        AlertStorage.info = value;
    }

    logOut(): void {
        this.isExecuting = true;

        this._googleAuthService
            .closeSession()
            .subscribe(next =>
            {
                LocalStorageService.clear();
                SessionStorageService.clear();
                this.isExecuting = false;
                this._router.navigate([PATH_GOOGLE_AUTHORIZATION]);
            });
    }

    duplicateConnection(): void {
        let newTab: Window | null = window.open(this._router.url, '_blank');

        let sessionToken: string | null = SessionStorageService.get(SESSION_TOKEN_KEY);

        if (sessionToken)
            newTab?.sessionStorage.setItem(SESSION_TOKEN_KEY, sessionToken);
    }

    newConnection(): void {
        let newTab = window.open(PATH_CONNECTION, '_blank');
        newTab?.sessionStorage.removeItem(SESSION_TOKEN_KEY);
    }

    changeInstance(): void {
        let complete: Function = () => {
            SessionStorageService.clear();
            this._router.navigate([PATH_CONNECTION]);
        };

        this.confirmModalComponent.confirm(
            () => {
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
            },
            complete,
            'Close current connection on all tabs?');
    }
}