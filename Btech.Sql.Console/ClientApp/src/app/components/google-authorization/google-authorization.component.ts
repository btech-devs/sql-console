import {Component} from '@angular/core';
import {ActivatedRoute, Router, NavigationExtras} from '@angular/router';
import {BaseComponent} from '../../_base/base.component';
import {GoogleAuthService} from '../../_services/google-auth.service';
import {LocalStorageService} from '../../_services/localStorageService';
import {
    ACCOUNT_PICTURE_URL_KEY,
    ID_TOKEN_KEY,
    PATH_CONNECTION, PATH_CONSOLE, PATH_ERROR,
    PATH_GOOGLE_AUTHORIZATION,
} from '../../utils';
import {SessionStorageService} from '../../_services/sessionStorageService';

@Component({
    selector: 'app-google-auth',
    templateUrl: './google-authorization.component.html'
})
export class GoogleAuthorizationComponent extends BaseComponent {
    private readonly _router: Router;
    private readonly _activatedRoute: ActivatedRoute;
    private readonly _googleAuthService: GoogleAuthService;

    private code: string | null = null;

    constructor(router: Router, activatedRoute: ActivatedRoute, googleAuthService: GoogleAuthService) {
        super();
        this._router = router;
        this._activatedRoute = activatedRoute;
        this._googleAuthService = googleAuthService;
    }

    async ngOnInit() {
        if (LocalStorageService.get(ID_TOKEN_KEY) != null) {
            await this._router.navigate([PATH_CONSOLE]);
        } else {
            LocalStorageService.clear();
            SessionStorageService.clear();

            this._activatedRoute.queryParams.subscribe({
                next: async params => {
                    this.code = params['code'];

                    let redirectUri: string = `${window.location.origin}/${PATH_GOOGLE_AUTHORIZATION}`;
                    let loginUrl = await this._googleAuthService.generateCodeRequestUri(redirectUri);

                    if (this.code == null) {
                        if(loginUrl != undefined){
                            window.location.href = loginUrl;
                        }
                    } else {
                        this._googleAuthService
                            .receiveAuthData(this.code, redirectUri)
                            .subscribe(response => {

                                let navigationExtras: NavigationExtras | undefined = undefined;

                                if (response.validationErrorMessages != undefined) {
                                    navigationExtras = {
                                        queryParams: {
                                            message: 'Authentication failed. Please, contact the administrator.',
                                            tryAgainButton: true
                                        }
                                    };

                                } else if (response.errorMessage != undefined) {
                                    navigationExtras = {
                                        queryParams: {
                                            message: response.errorMessage,
                                            tryAgainButton: true
                                        }
                                    };
                                } else if (response.errorMessages != undefined) {
                                    let errorMessage: string = '';

                                    response.errorMessages.forEach((error) => {
                                        errorMessage += `${error}\n`;
                                    });

                                    navigationExtras = {
                                        queryParams: {
                                            message: errorMessage,
                                            tryAgainButton: true
                                        }
                                    };
                                }

                                if (navigationExtras != undefined) {
                                    this._router.navigate([PATH_ERROR], navigationExtras);
                                } else {

                                    if (response.data?.idToken != undefined) {
                                        LocalStorageService.set(ID_TOKEN_KEY, response.data.idToken);

                                        if (response.data?.pictureUrl != undefined) {
                                            LocalStorageService.set(ACCOUNT_PICTURE_URL_KEY, response.data.pictureUrl);
                                        }

                                        this._router.navigate([PATH_CONNECTION]);
                                    } else {
                                        navigationExtras = {
                                            queryParams: {
                                                message: 'Authentication failed. Unexpected content. Please, contact the administrator.',
                                                tryAgainButton: true
                                            }
                                        };
                                        this._router.navigate([PATH_ERROR, navigationExtras]);
                                    }
                                }
                            });
                    }
                }
            });
        }
    }

}