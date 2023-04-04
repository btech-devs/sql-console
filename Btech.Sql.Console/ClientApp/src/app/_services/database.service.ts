import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {BaseService} from '../_base/base.service';
import {Database} from '../_models/responses/database/database.model';
import {Response} from '../_models/responses/base/response';
import {PaginationResponse} from '../_models/responses/base/pagination-response';
import {IdentityContext, IdentityContextTokenValue} from '../utils';
import {TableSchema} from '../_models/responses/database/tableSchema.model';

@Injectable()
export class DatabaseService extends BaseService {

    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    getDatabases(page: number = 0, perPage: number = 5, search: string = ''): Observable<PaginationResponse<Database>> {
        return this
            .requestGet<PaginationResponse<Database>>(
                '/api/databases',
                {
                    page: page,
                    perPage: perPage,
                    search: search
                },
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getDatabase(databaseName: string): Observable<Response<Database>> {
        return this
            .requestGet<Response<Database>>(
                `/api/databases/${databaseName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getTableSchema(databaseName: string, tableName: string): Observable<Response<TableSchema>> {
        return this
            .requestGet<Response<TableSchema>>(
                `/api/databases/${databaseName}/${tableName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

}