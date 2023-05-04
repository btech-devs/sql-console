import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {BaseService} from '../_base/base.service';
import {Database} from '../_models/responses/database/database.model';
import {Response} from '../_models/responses/base/response';
import {PaginationResponse} from '../_models/responses/base/pagination-response';
import {IdentityContext, IdentityContextTokenValue} from '../utils';
import {Table} from '../_models/responses/database/table.model';
import {Schema} from '../_models/responses/database/schema.model';
import {View} from '../_models/responses/database/view.model';

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

    getSchemaTable(databaseName: string, schemaName: string, tableName: string): Observable<Response<Table>> {
        return this
            .requestGet<Response<Table>>(
                `/api/databases/${databaseName}/${schemaName}/tables/${tableName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getSchemaTables(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/tables`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getSchemaViews(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/views`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getViewColumns(databaseName: string, schemaName: string, viewName: string): Observable<Response<View>> {
        return this
            .requestGet<Response<View>>(
                `/api/databases/${databaseName}/${schemaName}/views/${viewName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    getSchemaRoutines(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/routines`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

}