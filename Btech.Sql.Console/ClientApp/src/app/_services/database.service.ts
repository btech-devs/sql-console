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

/**
 * Service for managing database-related operations.
 * It extends the `BaseService` class to inherit common request methods.
 */
@Injectable()
export class DatabaseService extends BaseService {

    /**
     * Creates an instance of `DatabaseService`.
     * @param httpClient The HTTP client to send requests.
     */
    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    /**
     * Retrieves a paginated list of databases.
     * @param page The page number.
     * @param perPage The number of databases per page.
     * @param search The search string to filter databases.
     * @returns An observable of the API response containing a paginated list of databases.
     */
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

    /**
     * Retrieves information about a specific database.
     * @param databaseName The name of the database.
     * @returns An observable of the API response containing database information.
     */
    getDatabase(databaseName: string): Observable<Response<Database>> {
        return this
            .requestGet<Response<Database>>(
                `/api/databases/${databaseName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves information about a specific table in a schema of a database.
     * @param databaseName The name of the database.
     * @param schemaName The name of the schema.
     * @param tableName The name of the table.
     * @returns An observable of the API response containing table information.
     */
    getSchemaTable(databaseName: string, schemaName: string, tableName: string): Observable<Response<Table>> {
        return this
            .requestGet<Response<Table>>(
                `/api/databases/${databaseName}/${schemaName}/tables/${tableName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves information about all tables in a schema of a database.
     * @param databaseName The name of the database.
     * @param schemaName The name of the schema.
     * @returns An observable of the API response containing schema information with tables.
     */
    getSchemaTables(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/tables`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves information about all views in a schema of a database.
     * @param databaseName The name of the database.
     * @param schemaName The name of the schema.
     * @returns An observable of the API response containing schema information with views.
     */
    getSchemaViews(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/views`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves information about the columns of a specific view in a schema of a database.
     * @param databaseName The name of the database.
     * @param schemaName The name of the schema.
     * @param viewName The name of the view.
     * @returns An observable of the API response containing view information with columns.
     */
    getViewColumns(databaseName: string, schemaName: string, viewName: string): Observable<Response<View>> {
        return this
            .requestGet<Response<View>>(
                `/api/databases/${databaseName}/${schemaName}/views/${viewName}`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Retrieves information about all routines (procedures and functions) in a schema of a database.
     * @param databaseName The name of the database.
     * @param schemaName The name of the schema.
     * @returns An observable of the API response containing schema information with routines.
     */
    getSchemaRoutines(databaseName: string, schemaName: string): Observable<Response<Schema>> {
        return this
            .requestGet<Response<Schema>>(
                `/api/databases/${databaseName}/${schemaName}/routines`,
                {},
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

}