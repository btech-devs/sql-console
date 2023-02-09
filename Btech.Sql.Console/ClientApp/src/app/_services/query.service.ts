import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
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
            .requestPost<Response<Query>>('/api/query/execute', {
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
            .HttpClient.post('/api/query/execute/dsv', {
                    sql: sql,
                    databaseName: databaseName,
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

}