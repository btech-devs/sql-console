import {Injectable} from '@angular/core';
import {BaseService} from '../_base/base.service';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {SessionTokensResponse} from '../_models/session-tokens-response';
import {Response} from '../_models/responses/base/response';
import {IdentityContext, IdentityContextTokenValue} from '../utils';

@Injectable()
export class ConnectionService extends BaseService {

    readonly endpoint: string = '/api/connection';

    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    open(credentials: any): Observable<Response<SessionTokensResponse>> {
        return this.requestPost(
            `${this.endpoint}/open`,
            credentials,
            null,
            IdentityContext(IdentityContextTokenValue.Id));
    }

    close(): Observable<Response<undefined>> {
        return this.requestGet(
            `${this.endpoint}/close`,
            {},
            null,
            IdentityContext(IdentityContextTokenValue.Full));
    }

    getStaticConnection(): Observable<Response<SessionTokensResponse>> {
        return this.requestGet(
            `${this.endpoint}/static-connection`,
            {},
            null,
            IdentityContext(IdentityContextTokenValue.Id));
    }
}