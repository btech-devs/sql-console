import {Injectable} from '@angular/core';
import {BaseService} from '../_base/base.service';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {AuthData} from '../_models/responses/google-authorization/auth-data.model';
import {Response} from '../_models/responses/base/response';
import {MetadataService} from './metadata.service';
import {NavigationExtras, Router} from '@angular/router';
import {
    IdentityContext, IdentityContextTokenValue,
    PATH_ERROR
} from '../utils';

@Injectable()
export class GoogleAuthService extends BaseService {

    private readonly endpoint: string = '/api/google-auth';

    private readonly defaultScopes: string = 'profile email openid';

    constructor(httpClient: HttpClient,
                private _metadataService: MetadataService,
                private _router: Router ) {
        super(httpClient);
    }

    private getClientId(): Promise<Response<string>> {

        return new Promise<Response<string>>((resolve, reject) => {
            this._metadataService
                .getClientId()
                .subscribe({
                    next: (response) => resolve(response),
                    error: (error) => reject(error)
                });
        });
    }

    public async generateCodeRequestUri(redirectUri: string, additionalScopes: string | null = null): Promise<string | undefined> {

        let response = await this.getClientId();
        let params : HttpParams | undefined = undefined;
        let navigationExtras: NavigationExtras | undefined = undefined;

        if(response.errorMessage != undefined) {
            navigationExtras = {
                queryParams: {
                    message: response.errorMessage,
                    tryAgainButton: true
                }
            };
        }
        else if(response.errorMessages != undefined) {
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
            await this._router.navigate([PATH_ERROR], navigationExtras);
        } else {

            if(response.data != undefined) {
                params = new HttpParams()
                    .set('redirect_uri', redirectUri)
                    .set('access_type', 'offline')
                    .set('prompt', 'consent')
                    .set('response_type', 'code')
                    .set('client_id', response.data)
                    .set('scope', additionalScopes == null ? this.defaultScopes : additionalScopes)
                    .set('include_granted_scopes', additionalScopes == null ? 'false' : 'true');
            }
            else {
                navigationExtras = {
                    queryParams: {
                        message: 'The server did not send the client id. Try again in a few minutes, or contact the administrator.',
                        tryAgainButton: true
                    }
                };
                await this._router.navigate([PATH_ERROR, navigationExtras]);
            }
        }

        let result = undefined;

        if(params != undefined){
            result = `https://accounts.google.com/o/oauth2/v2/auth?${params.toString()}`
        }

        return result;
    }

    public receiveAuthData(authorizationCode: string, redirectUri: string) : Observable<Response<AuthData>> {
        return super.requestPost<Response<AuthData>>(
            `${this.endpoint}/exchange-code`,
            {
                'code': authorizationCode,
                'redirect_uri': redirectUri
            }
        );
    }

    public closeSession() : Observable<Response<undefined>> {
        return super.requestGet<Response<undefined>>(
            `${this.endpoint}/close-session`,
            {},
            null,
            IdentityContext(IdentityContextTokenValue.Id)
        );
    }
}