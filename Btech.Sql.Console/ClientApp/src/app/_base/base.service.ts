import {HttpClient, HttpContext, HttpHeaders} from '@angular/common/http';
import {Observable, shareReplay} from 'rxjs';
import {getHeaders, IdentityContext} from '../utils';

export abstract class BaseService {
    protected readonly HttpClient: HttpClient;

    protected constructor(httpClient: HttpClient) {
        this.HttpClient = httpClient;
    }

    // ↓ methods

    /**
     * Sends a GET HTTP request ({@link HttpClient.get}).
     * @param url - An HTTP URL.
     * @param queryParams - A query parameter list (optional).
     * @param headers - An additional headers (optional).
     * @param context - An additional data for interceptor (e.x. 'idToken' required etc.) (optional).
     */
    protected requestGet<T>(
        url: string, queryParams?: { [key: string]: any }, headers: HttpHeaders | null = null,
        context: HttpContext = IdentityContext())
        : Observable<T> {

        let filteredQueryParams: { [key: string]: any } | undefined = undefined;

        if (queryParams) {
            filteredQueryParams = {};

            for (let queryParamKey in queryParams) {
                if (queryParams[queryParamKey] != null && queryParams[queryParamKey] == '')
                    filteredQueryParams[queryParamKey] = queryParams[queryParamKey];
            }
        }

        return this.HttpClient.get<T>(
            url,
            {
                params: queryParams,
                headers: headers != null ? headers : getHeaders(),
                context: context

            })
            .pipe(shareReplay(1));
    }

    /**
     * Sends a POST HTTP request ({@link HttpClient.post}).
     * @param url - An HTTP URL.
     * @param body - A request body object.
     * @param headers - An additional headers (optional).
     * @param context - An additional data for interceptor (e.x. 'idToken' required etc.) (optional).
     */
    protected requestPost<T>(
        url: string, body: any, headers: HttpHeaders | null = null, context: HttpContext = IdentityContext())
        : Observable<T> {
        return this.HttpClient.post<T>(
            url,
            body,
            {
                headers: headers != null ? headers : getHeaders(),
                context: context

            },)
            .pipe(shareReplay(1));
    }

    /**
     * Sends a PUT HTTP request ({@link HttpClient.put}).
     * @param url - An HTTP URL.
     * @param body - A request body object.
     * @param headers - An additional headers (optional).
     * @param context - An additional data for interceptor (e.x. 'idToken' required etc.) (optional).
     */
    protected requestPut<T>(
        url: string, body: any, headers: HttpHeaders | null = null, context: HttpContext = IdentityContext())
        : Observable<T> {
        return this.HttpClient.put<T>(
            url,
            body,
            {
                headers: headers != null ? headers : getHeaders(),
                context: context
            })
            .pipe(shareReplay(1));
    }

    // ↑ methods
}