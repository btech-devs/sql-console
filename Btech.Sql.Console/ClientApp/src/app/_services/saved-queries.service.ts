import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {BaseService} from '../_base/base.service';
import {Response, ResponseBase} from '../_models/responses/base/response';
import {IdentityContext, IdentityContextTokenValue} from '../utils';
import {SavedQuery} from '../_models/responses/saved-query/saved-query.model';

/**
 * Service for managing saved queries in the application.
 * This service provides methods to retrieve, create, update, and delete saved queries.
 */
@Injectable()
export class SavedQueriesService extends BaseService {

    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    /**
     * Retrieves all saved queries.
     * @param includeQuery Specifies whether to include the query text in the response (default: false).
     * @returns An observable that emits a response containing an array of saved queries.
     */
    getAll(includeQuery: boolean = false): Observable<Response<SavedQuery[]>> {
        return this
            .requestGet<Response<SavedQuery[]>>(
                `/api/saved-queries`,
                {
                    includeQuery: includeQuery
                },
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves a saved query by its ID.
     * @param id The ID of the saved query.
     * @returns An observable that emits a response containing the saved query.
     */
    get(id: number): Observable<Response<SavedQuery>> {
        return this
            .requestGet<Response<SavedQuery>>(
                `/api/saved-queries/${id}`,
                undefined,
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Creates a new saved query.
     * @param savedQuery The partial saved query object to create.
     * @returns An observable that emits a response base indicating the success of the operation.
     */
    post(savedQuery: Partial<SavedQuery>): Observable<ResponseBase> {
        return this
            .requestPost<ResponseBase>(
                `/api/saved-queries`,
                savedQuery,
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Updates an existing saved query.
     * @param savedQuery The saved query object to update.
     * @returns An observable that emits a response base indicating the success of the operation.
     */
    put(savedQuery: SavedQuery): Observable<ResponseBase> {
        return this
            .requestPut<ResponseBase>(
                `/api/saved-queries/${savedQuery.id}`,
                savedQuery,
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Deletes a saved query by its ID.
     * @param id The ID of the saved query to delete.
     * @returns An observable that emits a response base indicating the success of the operation.
     */
    delete(id: number): Observable<ResponseBase> {
        return this
            .requestDelete<ResponseBase>(
                `/api/saved-queries/${id}`,
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }
}