import {Injectable} from '@angular/core';
import {HttpClient, HttpEvent} from '@angular/common/http';
import {Observable} from 'rxjs';
import {BaseService} from '../_base/base.service';
import {Query} from '../_models/responses/query/query.model';
import {Response} from '../_models/responses/base/response';
import {getHeaders, IdentityContext, IdentityContextTokenValue} from '../utils';

/**
 * Service for executing queries and performing data imports in the application.
 * This service provides methods to execute SQL queries, export query results to DSV format,
 * and import SQL and DSV files into databases.
 */
@Injectable()
export class QueryService extends BaseService {

    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    /**
     * Executes a SQL query on a specific database.
     * @param sql The SQL query to execute.
     * @param databaseName The name of the target database.
     * @returns An observable that emits a response containing the query result.
     */
    execute(sql: string, databaseName: string): Observable<Response<Query>> {
        return this
            .requestPost<Response<Query>>(`/api/query/execute/${databaseName}`, {
                    sql: sql,
                    databaseName: databaseName
                },
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

    /**
     * Executes a SQL query and exports the result to a DSV (Delimiter-Separated Values) file.
     * @param sql The SQL query to execute.
     * @param databaseName The name of the target database.
     * @param separator The separator character to use in the DSV file.
     * @param newLine The newline character to use in the DSV file.
     * @param includeHeader Specifies whether to include a header row in the DSV file.
     * @param addQuotes Specifies whether to enclose values in double quotes in the DSV file.
     * @param nullOutput The string representation of null values in the DSV file.
     * @returns An observable that emits an HTTP event containing the DSV file data.
     */
    executeDsv(
        sql: string,
        databaseName: string,
        separator: string,
        newLine: string,
        includeHeader: boolean,
        addQuotes: boolean,
        nullOutput: string) {
        return this
            .HttpClient.post(`/api/query/execute/${databaseName}/dsv`, {
                    sql: sql,
                    separator: separator,
                    newLine: newLine,
                    includeHeader: includeHeader,
                    addQuotes: addQuotes,
                    nullOutput: nullOutput
                },
                {
                    headers: getHeaders(),
                    reportProgress: true,
                    observe: 'events',
                    responseType: 'blob',
                    context: IdentityContext(IdentityContextTokenValue.Full)
                });
    }

    /**
     * Imports an SQL file into a database.
     * @param databaseName The name of the target database.
     * @param formData The form data containing the SQL file to import.
     * @returns An observable that emits an HTTP event containing the import result.
     */
    importSql(
        databaseName: string,
        formData: FormData): Observable<HttpEvent<Response<Query>>> {

        return this.HttpClient.post<Response<Query>>(
            `/api/query/import/${databaseName}/sql`,
            formData,
            {
                reportProgress: true,
                observe: 'events',
                responseType: 'json',
                context: IdentityContext(IdentityContextTokenValue.Full)
            });
    }

    /**
     * Imports a DSV (Delimiter-Separated Values) file into a table in a database.
     * @param database The name of the target database.
     * @param table The name of the target table.
     * @param separator The separator character used in the DSV file.
     * @param file The DSV file to import.
     * @param fileName The name of the DSV file.
     * @param chunkSize The number of rows to process in each chunk (default: 1000).
     * @param doubleQuotes Specifies whether values in the DSV file are enclosed in double quotes (default: false).
     * @param rollbackOnError Specifies whether to roll back the import operation if an error occurs (default: false).
     * @param rowsToSkip The number of rows to skip at the beginning of the DSV file (default: 0).
     * @returns An observable that emits an HTTP event containing the import result.
     */
    importDsv(
        database: string,
        table: string,
        separator: string,
        file: Blob,
        fileName?: string,
        chunkSize: number = 1000,
        doubleQuotes: boolean = false,
        rollbackOnError: boolean = false,
        rowsToSkip: number = 0): Observable<HttpEvent<Response<Query>>> {

        let formData: FormData = new FormData();
        formData.append('file', file, fileName);
        formData.append('separator', separator);
        formData.append('chunkSize', chunkSize.toString());
        formData.append('doubleQuotes', `${doubleQuotes}`);
        formData.append('rollbackOnError', `${rollbackOnError}`);
        formData.append('rowsToSkip', `${rowsToSkip}`);

        return this.HttpClient.post<Response<Query>>(
            `/api/query/import/${database}/dsv/${table}`,
            formData,
            {
                reportProgress: true,
                observe: 'events',
                responseType: 'json',
                context: IdentityContext(IdentityContextTokenValue.Full)
            });
    }
}