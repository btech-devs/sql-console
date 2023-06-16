import {Injectable} from '@angular/core';
import {BaseService} from '../_base/base.service';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {SessionTokensResponse} from '../_models/session-tokens-response';
import {Response} from '../_models/responses/base/response';
import {IdentityContext, IdentityContextTokenValue} from '../utils';

/**
 * Service for managing connections to the API.
 * It extends the `BaseService` class to inherit common request methods.
 */
@Injectable()
export class ConnectionService extends BaseService {

    /**
     * The endpoint URL for connection-related API requests.
     */
    readonly endpoint: string = '/api/connection';

    /**
     * Creates an instance of `ConnectionService`.
     * @param httpClient The HTTP client to send requests.
     */
    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    /**
     * Opens a new connection with the provided credentials.
     * @param credentials The credentials for opening the connection.
     * @returns An observable of the API response containing session tokens.
     */
    open(credentials: any): Observable<Response<SessionTokensResponse>> {
        return this.requestPost(
            `${this.endpoint}/open`,
            credentials,
            null,
            IdentityContext(IdentityContextTokenValue.Id));
    }

    /**
     * Closes the current connection.
     * @returns An observable of the API response.
     */
    close(): Observable<Response<undefined>> {
        return this.requestGet(
            `${this.endpoint}/close`,
            {},
            null,
            IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves session tokens for a static connection.
     * @returns An observable of the API response containing session tokens.
     */
    getStaticConnection(): Observable<Response<SessionTokensResponse>> {
        return this.requestGet(
            `${this.endpoint}/static-connection`,
            {},
            null,
            IdentityContext(IdentityContextTokenValue.Id));
    }
}