import {Injectable} from '@angular/core';
import {HttpClient, HttpEvent} from '@angular/common/http';
import {Observable} from 'rxjs';
import {BaseService} from '../_base/base.service';
import {Query} from '../_models/responses/query/query.model';
import {Response} from '../_models/responses/base/response';
import {getHeaders, IdentityContext, IdentityContextTokenValue} from '../utils';

@Injectable()
export class QueryService extends BaseService {

    constructor(httpClient: HttpClient) {
        super(httpClient);
    }

    execute(sql: string, databaseName: string): Observable<Response<Query>> {
        return this
            .requestPost<Response<Query>>(`/api/query/execute/${databaseName}`, {
                    sql: sql,
                    databaseName: databaseName
                },
                null,
                IdentityContext(IdentityContextTokenValue.Full));
    }

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