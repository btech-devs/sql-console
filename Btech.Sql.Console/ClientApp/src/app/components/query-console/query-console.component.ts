import {
    AfterViewInit,
    Component,
    ElementRef,
    HostListener,
    OnDestroy,
    OnInit,
    TemplateRef,
    ViewChild
} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {Console} from './models/console';
import {ResultTable} from './models/resultTable';
import * as CodeMirror from 'codemirror';
import {Editor, LineHandle} from 'codemirror';
import {Annotation} from 'codemirror/addon/lint/lint';
import {AST, Option, Select} from 'node-sql-parser/types';
import {Parser} from 'node-sql-parser';
import {QueryService} from '../../_services/query.service';
import {
    catchError,
    concatMap,
    filter,
    finalize,
    from,
    Observable,
    of,
    Subject,
    takeUntil,
    tap,
    throwError
} from 'rxjs';
import {Query} from '../../_models/responses/query/query.model';
import {BaseComponent} from '../../_base/base.component';
import 'codemirror/mode/sql/sql';
import 'codemirror/addon/hint/show-hint';
import 'codemirror/addon/hint/sql-hint';
import 'codemirror/addon/lint/lint';
import 'codemirror/addon/comment/comment';
import 'codemirror/addon/edit/closebrackets';
import 'codemirror/addon/edit/matchbrackets';
import {Database} from '../../_models/responses/database/database.model';
import {DatabaseService} from '../../_services/database.service';
import {Table} from '../../_models/responses/database/table.model';
import {Response} from '../../_models/responses/base/response';
import {DsvExporterComponent} from './data-exporters/dsv-exporter/dsv-exporter.component';
import {InstanceType} from '../../_models/instance-type';
import {SESSION_TOKEN_KEY} from '../../utils';
import {SessionStorageService} from '../../_services/sessionStorageService';
import {ConfirmModalService} from '../confirm-modal/confirm-modal.service';
import {DatabaseViewerComponent} from './database-viewer/database-viewer.component';
import {Schema} from '../../_models/responses/database/schema.model';

declare var $: any;

type QueryViewModel = {
    lines: LineHandle[],
    queryAst?: AST[],
    rawQuery?: string
}

const CHARACTER_VALIDATION_LIMIT: number = 5000;

type TableSchemaViewModel = Table & { schema: string };

@Component({
    selector: 'app-query',
    templateUrl: './query-console.component.html',
    styleUrls: ['./query-console.component.less']
})
export class QueryConsoleComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy {

    constructor(
        private _queryService: QueryService,
        private _databaseService: DatabaseService,
        private _confirmService: ConfirmModalService,
        private _titleService: Title) {
        super();
    }

    // region Private Fields

    private _sqlParser: Parser = new Parser();
    private _codeEditor?: Editor;
    private _hintOptions: CodeMirror.ShowHintOptions = {
        hint: CodeMirror.hint.sql
    };

    private _schema: { [key: string]: string[] } = {};
    private _queryList: QueryViewModel[] = [];
    private _tableViewPopupState: boolean = false;
    private _rawQuery: string = '';
    private _isValidQuery: boolean = true;
    private _anyValidSelect: number = 0;
    private _queryLimit?: number = 100;
    private _console: Console = new Console();
    private _results: ResultTable[] = [];
    private _isExecuting: boolean = false;
    private _isFetchingInstanceData: boolean = false;
    private _isSidebar: boolean = true;
    private _instanceType?: InstanceType;
    private _codeMirrorMode?: string;
    private _sqlParserOptions: Option = {};
    private _selectedTableIndex: number = 0;
    private _lastSelectQueryIndex?: number;
    private _tabCount: number = 0;
    private _host?: string;
    private _selectedDatabase?: Database;
    private _selectedDatabaseTables?: TableSchemaViewModel[];

    public readonly isMacOs: boolean = /Mac/i.test(navigator.userAgent);


    // endregion Private Fields

    // region Public Getters

    get tableViewPopupState(): boolean {
        return this._tableViewPopupState;
    }

    get selectedDatabase(): Database | undefined {
        return this._selectedDatabase;
    }

    get selectedTableIndex(): number {
        return this._selectedTableIndex;
    }

    get rawQuery(): string {
        return this._rawQuery;
    }

    get host(): string | undefined {
        return this._host;
    }

    get lastSelectQueryIndex(): number | undefined {
        return this._lastSelectQueryIndex;
    }

    get isValidQuery(): boolean {
        return this._isValidQuery;
    }

    get anyValidSelect(): number {
        return this._anyValidSelect;
    }

    get queryLimit(): number | undefined {
        return this._queryLimit;
    }

    get console(): Console {
        return this._console;
    }

    get results(): ResultTable[] {
        return this._results;
    }

    get isExecuting(): boolean {
        return this._isExecuting;
    }

    get isFetchingInstanceData(): boolean {
        return this._isFetchingInstanceData;
    }

    get isSidebar(): boolean {
        return this._isSidebar;
    }

    get selectedDatabaseTables(): TableSchemaViewModel[] | undefined {
        return this._selectedDatabaseTables;
    }


    // endregion Public Getters

    // region Public Setters

    set selectedDatabaseTables(value: TableSchemaViewModel[] | undefined) {
        this._selectedDatabaseTables = value;
    }

    set results(value: ResultTable[]) {
        this._results = value;
    }

    set tableViewPopupState(value: boolean) {
        this._tableViewPopupState = value;
    }

    set isSidebar(value) {
        this._isSidebar = value;

        if (!value)
            this.onResizeSidebar(0);
        else {
            let sidebarWidth = parseInt(this.querySidebar!.nativeElement.style.width);

            if (sidebarWidth)
                this.onResizeSidebar(sidebarWidth);
        }
    }

    set selectedTableIndex(value: number) {
        this._selectedTableIndex = value;
    }

    // endregion Public Setters

    // region ViewChild fields

    @ViewChild('databaseViewer') databaseViewer!: DatabaseViewerComponent;
    @ViewChild('textArea') textArea?: ElementRef;
    @ViewChild('consoleHandle') consoleHandle?: ElementRef;
    @ViewChild('resultHandle') resultHandle?: ElementRef;
    @ViewChild('queryMain') queryMain?: ElementRef;
    @ViewChild('querySidebar') querySidebar?: ElementRef;
    @ViewChild('dsvExporterComponent') dsvExporter?: DsvExporterComponent;
    @ViewChild('queryGutter') queryGutter!: TemplateRef<{
        isSelect: boolean,
        startLine: number,
        endLine: number,
        queryIndex: number
    }>;

    // endregion ViewChild fields

    // region Private Methods

    private applyLimitForSqlAst(): void {
        this._queryList.flatMap(query => query.queryAst)!.forEach(sqlAst => {
            if (sqlAst?.type === 'select') {
                switch (this._instanceType) {
                    case InstanceType.MsSql: {
                        let ast: Select & { top: any } = sqlAst as Select & { top: any };

                        if (!ast.top?.value)
                            ast.top = {value: this.queryLimit!, percent: null};

                        break;
                    }
                    case InstanceType.PgSql: {
                        if (sqlAst.limit?.value?.length == 0) {
                            sqlAst.limit = {
                                seperator: '',
                                value: [
                                    {type: 'number', value: this.queryLimit!},
                                ]
                            };
                        }

                        break;
                    }
                }
            }
        });
    }

    private sqlValidator(): Annotation[] {
        this._isValidQuery = true;
        let errors: Annotation[] = [];
        this._codeEditor?.clearGutter('CodeMirror-query-gutter');

        let index: number = 0;
        let lineCount: number | undefined = this._codeEditor?.lineCount();
        this._queryList = [];

        let query: QueryViewModel = {lines: []};

        this._anyValidSelect = 0;

        if (this.rawQuery.length <= CHARACTER_VALIDATION_LIMIT) {
            // Split queries
            this._codeEditor?.eachLine(line => {

                if (!line.text.trimStart().startsWith('--') && line.text.trimEnd().length > 0) {
                    query.lines.push(line);
                }

                if (line.text.trimEnd().endsWith(';') && !line.text.trimStart().startsWith('--') || index + 1 == lineCount) {
                    if (query.lines.length > 0)
                        this._queryList.push(query!);

                    query = {
                        lines: []
                    };
                }

                index++;
            });

            this._queryList.forEach((query, index) => {
                let isValidQuery = true;
                let isValidSchema = true;

                try {
                    this.validateSchema(query, this._sqlParserOptions);
                } catch (exception: any) {
                    let location: any = exception?.location;
                    let name: any = exception?.name;
                    let message: any = exception?.message;

                    isValidQuery = false;

                    if (name != 'Schema') {
                        this._isValidQuery = false;
                    } else
                        isValidSchema = false;

                    if (location && name && message) {
                        errors.push({
                            from: {
                                ch: location.start.column - 1,
                                line: location.start.line
                            },
                            to: {
                                ch: location.end.column,
                                line: location.end.line
                            },
                            severity: isValidSchema ? 'warning' : 'error',
                            message: `${name}: ${message}`,
                        });
                    }
                }

                if (query.queryAst && isValidQuery) {
                    this._anyValidSelect += query.queryAst.filter(ast => ast.type == 'select').length;
                }

                if (query.queryAst && query.queryAst.some(ast => ast.type == 'select'))
                    this._lastSelectQueryIndex = index;

                if (this.selectedDatabase?.name && isValidSchema) {
                    this.addQueryExecution(query, index);
                }

                if (this.selectedDatabase?.name) {
                    this.addQueryExecution(query, index);
                }
            });
        } else {
            this._isValidQuery = false;

            errors.push({
                from: {
                    ch: 0,
                    line: 0
                },
                to: {
                    ch: 0,
                    line: 0
                },
                severity: 'warning',
                message: `WARNING: query validation has been disabled due to the big length: ${this.rawQuery.length} characters. Limit: ${CHARACTER_VALIDATION_LIMIT} characters.'`,
            });
        }

        if (this.rawQuery.length == 0)
            this._isValidQuery = false;

        return errors;
    }

    private addQueryExecution(query: QueryViewModel, queryIndex: number): void {
        if (query.lines.length > 0) {

            let startLine = this._codeEditor?.getLineNumber(query.lines[0]);
            let endLine = this._codeEditor?.getLineNumber(query.lines[query.lines.length - 1]);

            let isSelect: boolean = false;

            if (query.queryAst?.length == 1 && query.queryAst[0].type == 'select')
                isSelect = true;
            else if (query.rawQuery?.trimStart().toLowerCase().startsWith('select') == true)
                isSelect = true;

            let queryGutter = this.queryGutter.createEmbeddedView({
                isSelect: isSelect,
                startLine: startLine!,
                endLine: endLine!,
                queryIndex: queryIndex
            });

            queryGutter.detectChanges();

            this._codeEditor?.setGutterMarker(startLine, 'CodeMirror-query-gutter', queryGutter.rootNodes[0]);
        }
    }

    private validateSchema(query: QueryViewModel, option: Option): void {
        query.rawQuery = query.lines.map(line => line.text).join('\n');

        // validate syntax
        // thrown exception on syntax error

        let keys = Object.keys(this._schema);

        let getTableSchema = (tableName: string): string[] | undefined => {
            let schema: string[] | undefined = undefined;

            let key = keys.find(key => key.endsWith(tableName));

            if (key)
                schema = this._schema[key];

            return schema;
        };

        try {
            let ast: AST | AST[] = this._sqlParser.astify(query.rawQuery, option);

            if (ast instanceof Array)
                query.queryAst = ast;
            else
                query.queryAst = [ast];

        } catch (exception: any) {
            let location: any = exception?.location;

            let startLineNumber = this._codeEditor?.getLineNumber(query.lines[0]);

            if (location) {
                location.start.line += startLineNumber! - 1;
                location.end.line += startLineNumber! - 1;
            }

            throw exception;
        }

        let tableList: string[] = this._sqlParser.tableList(query.rawQuery, {
            database: option.database,
            type: 'column'
        }).map(value => value.split('::')[2]);

        if (query.queryAst[0].type != 'create') {
            let columnList: string[] = this._sqlParser.columnList(
                query.rawQuery, {
                    database: option.database,
                    type: 'column'
                })
                .filter(value => value.split('::')[2] != '(.*)');

            // validate query schema

            // validate tableNames
            if (tableList.length > 0)
                tableList.forEach(tableName => {
                    if (!getTableSchema(tableName)) {
                        query.lines.forEach((queryLine: LineHandle) => {
                            let tableNameFirstCharacterPosition = queryLine.text.search(new RegExp(`[\["\\s]${tableName}`));

                            if (tableNameFirstCharacterPosition > -1) {
                                let lineNumber = this._codeEditor?.getLineNumber(queryLine);

                                throw {
                                    location: {
                                        start: {
                                            column: tableNameFirstCharacterPosition + 2,
                                            line: lineNumber
                                        },
                                        end: {
                                            column: tableNameFirstCharacterPosition + tableName.length + 1,
                                            line: lineNumber
                                        }
                                    },
                                    name: 'Schema',
                                    message: `Unable to resolve table '${tableName}'`

                                };
                            }
                        });
                    }
                });

            // extract columnName from Query path in format '{func}::{tableName}::{columnName}'
            let columnListWithTables = columnList.filter(columnPath => columnPath.split('::')[1] != 'null');

            // validate columns with specified tables
            columnListWithTables.forEach(columnPath => {
                let path = columnPath.split('::');

                let tableName = path[1];
                let columnName = path[2];

                if (tableName.includes('.'))
                    tableName = tableName.split('.')[1];

                if (!tableList.includes(tableName)) {
                    query.lines.forEach((queryLine, lineIndex) => {
                        let tableNameFirstCharacterPosition = queryLine.text.search(new RegExp(`[\["\\s]${tableName}`));

                        if (tableNameFirstCharacterPosition > -1) {
                            let lineNumber = this._codeEditor?.getLineNumber(queryLine);

                            throw {
                                location: {
                                    start: {
                                        column: tableNameFirstCharacterPosition + 2,
                                        line: lineNumber
                                    },
                                    end: {
                                        column: tableNameFirstCharacterPosition + tableName.length + 1,
                                        line: lineNumber
                                    }
                                },
                                name: 'Schema',
                                message: `Unexpected table name '${tableName}'`

                            };
                        }
                    });
                }

                if (!(getTableSchema(tableName)?.includes(columnName))) {
                    query.lines.forEach((queryLine, lineIndex) => {
                        let columnNameFirstCharacterPosition = queryLine.text
                            .search(new RegExp(`(?<=[."])${columnName}`));

                        if (columnNameFirstCharacterPosition == -1)
                            columnNameFirstCharacterPosition = queryLine.text
                                .search(new RegExp(`(["\(,\\s])${columnName}`));

                        if (columnNameFirstCharacterPosition > -1) {
                            let lineNumber = this._codeEditor?.getLineNumber(queryLine);

                            throw {
                                location: {
                                    start: {
                                        column: columnNameFirstCharacterPosition,
                                        line: lineNumber
                                    },
                                    end: {
                                        column: columnNameFirstCharacterPosition + columnName.length + 1,
                                        line: lineNumber
                                    }
                                },
                                name: 'Schema',
                                message: `Unable to resolve column '${columnName}' in table '${tableName}'.`

                            };
                        }
                    });
                }
            });

            let columnListWithoutTables = columnList
                .filter(columnPath => columnPath.split('::')[1] == 'null')
                .map(value => value.split('::')[2]);

            // validate columns without specified tables
            if (columnList.length > 0 && query.queryAst[0].type != 'alter') {
                columnListWithoutTables.forEach(columnName => {
                    let tableThatContainsColumns: number = 0;

                    tableList.forEach(tableName => {
                        if (getTableSchema(tableName)?.includes(columnName)) {
                            tableThatContainsColumns++;
                        }
                    });

                    if (tableThatContainsColumns != 1) {
                        query.lines.forEach((queryLine) => {
                            let columnNameFirstCharacterPosition = queryLine.text
                                .search(new RegExp(`(?<!\\.")${columnName}`));

                            if (columnNameFirstCharacterPosition > -1) {
                                let lineNumber = this._codeEditor?.getLineNumber(queryLine);

                                throw {
                                    location: {
                                        start: {
                                            column: columnNameFirstCharacterPosition,
                                            line: lineNumber
                                        },
                                        end: {
                                            column: columnNameFirstCharacterPosition + columnName.length + 1,
                                            line: lineNumber
                                        }
                                    },
                                    name: 'Schema',
                                    message: columnNameFirstCharacterPosition == 0
                                        ? `Unable to resolve table '${columnName}'`
                                        : `Ambiguous column reference '${columnName}'`

                                };
                            }
                        });
                    }
                });
            }
        } else {
            if (tableList.length > 0)
                tableList.forEach(tableName => {
                    if (getTableSchema(tableName)) {
                        query.lines.forEach((queryLine: LineHandle) => {
                            let tableNameFirstCharacterPosition = queryLine.text.search(new RegExp(`[\["|\\s]${tableName}`));

                            if (tableNameFirstCharacterPosition > -1) {
                                let lineNumber = this._codeEditor?.getLineNumber(queryLine);

                                throw {
                                    location: {
                                        start: {
                                            column: tableNameFirstCharacterPosition + 2,
                                            line: lineNumber
                                        },
                                        end: {
                                            column: tableNameFirstCharacterPosition + tableName.length + 1,
                                            line: lineNumber
                                        }
                                    },
                                    name: 'Schema',
                                    message: `Table with name '${tableName}' already exists.`

                                };
                            }
                        });
                    }
                });
        }
    }


    private executeRawSql(sql: string, newTab: boolean = false): Observable<any> {
        this._isExecuting = true;

        return from([sql])
            .pipe(
                concatMap((query) => {

                    this._console.pushSeparator();

                    if (sql.length <= CHARACTER_VALIDATION_LIMIT)
                        this.console.pushMessage(`Executing query: '${sql}'.`);
                    else {
                        this.console.pushMessage(`Executing query...`);
                    }

                    if (newTab && this.results.length < 1)
                        newTab = false;

                    return new Observable<{ confirmer: boolean, sql: string }>((subscriber) => {
                        if (newTab && this.results.length == 6) {
                            this._confirmService!
                                .confirm('All saved results but Main will be closed. Execute?')
                                .subscribe(result => {
                                    if (result) {
                                        subscriber.next({confirmer: true, sql: query});
                                        subscriber.complete();
                                    } else {
                                        subscriber.next({confirmer: false, sql: query});
                                        subscriber.complete();
                                    }
                                });
                        } else {
                            subscriber.next({confirmer: true, sql: query});
                            subscriber.complete();
                        }
                    });
                }),
                filter((result) => {

                    if (result.confirmer == false) {
                        this._isExecuting = false;

                        this._console.pushWarning('Execution canceled.');
                    }

                    return result.confirmer == true;
                }),
                concatMap((result) => {
                    return this._queryService.execute(result.sql, this._selectedDatabase!.name!);
                }),
                tap(response => {
                    if (response.errorMessage != undefined) {
                        const messagePart = 'Something went wrong on executing query.';

                        if (response.errorMessage)
                            this.console.pushDanger(`${messagePart} ErrorMessage: '${response.errorMessage}'`);
                        else if (response.errorMessage)
                            this.console.pushDanger(`${messagePart} ErrorMessages: '${response.errorMessages?.join(',')}'`);
                    } else {
                        if (response.data?.recordsAffected != undefined && response.data.recordsAffected >= 0) {
                            if (response.data.elapsedTimeMs)
                                this.console.pushSuccess(`Result: ${response.data.recordsAffected} row(s) affected. Elapsed time: ${response.data.elapsedTimeMs} ms.`);
                            else
                                this.console.pushSuccess(`Result: ${response.data.recordsAffected} row(s) affected.`);
                        } else {
                            if (response.data?.elapsedTimeMs)
                                this.console.pushSuccess(`Successfully executed. Elapsed time: ${response.data.elapsedTimeMs} ms.`);
                            else
                                this.console.pushSuccess(`Successfully executed.`);
                        }

                        if (response.data?.table != undefined) {
                            let table = ResultTable.createFromQueryTable(response.data.table);

                            if (newTab) {
                                this._tabCount++;

                                if (this.results.length > 5) {
                                    this._tabCount = 1;
                                    this.results = this.results.slice(0, 1);
                                }

                                table.name = `Execution ${this._tabCount}`;

                                this.selectedTableIndex = this.results.length;
                                this.results.push(table);
                            } else {
                                table.name = 'Main execution';
                                this.results[0] = table;
                                this.selectedTableIndex = 0;
                            }
                            this.console.pushSuccess(`Result: ${table.body.length} row(s) with ${table.header?.length} column(s) retrieved.`);

                            this.resultHandle?.nativeElement.click();
                        }
                    }
                }),
                catchError((error: Error) => {
                    if (error.message)
                        this._console.pushDanger(error.message);

                    return throwError(() => error);
                }),
                finalize(() => {
                    this._isExecuting = false;
                })
            );
    }

    // endregion Private Methods

    // region Override Methods

    ngOnInit(): void {
        $(function () {
            $('body').popover({
                html: true,
                container: 'body',
                selector: '[data-bs-toggle="popover"]'
            });
        });

        let tokenClaims: any = JSON.parse(window.atob(SessionStorageService.get(SESSION_TOKEN_KEY)!.split('.')[1]));

        let rawInstanceType = tokenClaims?.instance_type;
        this._host = tokenClaims?.host;

        switch (rawInstanceType) {
            case InstanceType.PgSql: {
                this._instanceType = InstanceType.PgSql;
                this._sqlParserOptions.database = 'PostgresQL';
                this._codeMirrorMode = 'text/x-pgsql';
                break;
            }
            case InstanceType.MsSql: {
                this._instanceType = InstanceType.MsSql;
                this._sqlParserOptions.database = 'TransactSQL';
                this._codeMirrorMode = 'text/x-mssql';
                break;
            }
            default: {
                this.console.pushDanger('InstanceType is not specified!');
                break;
            }
        }

        this.console.pushMessage(`Current instance type is '${this._instanceType}'.`);

        if (this._host)
            this._titleService.setTitle(`SQL Console | ${this._instanceType} | ${this._host}`);
    }

    ngOnDestroy(): void {
        $(function () {
            $('.popover').remove();
        });
    }

    ngAfterViewInit(): void {
        this._codeEditor = CodeMirror
            .fromTextArea(this.textArea?.nativeElement, {
                extraKeys: {
                    'Alt-Up': (codeMirror): void => {
                        if (!codeMirror.isReadOnly()) {
                            let ranges = codeMirror.listSelections(), linesToMove: number[] = [],
                                at = codeMirror.firstLine() - 1,
                                newSels: { anchor: CodeMirror.Position; head: CodeMirror.Position; }[] = [];
                            for (let i = 0; i < ranges.length; i++) {
                                let range = ranges[i], from = range.from().line - 1, to = range.to().line;
                                newSels.push({
                                    anchor: CodeMirror.Pos(range.anchor.line - 1, range.anchor.ch),
                                    head: CodeMirror.Pos(range.head.line - 1, range.head.ch)
                                });
                                if (range.to().ch == 0 && !range.empty()) --to;
                                if (from > at) linesToMove.push(from, to);
                                else if (linesToMove.length) linesToMove[linesToMove.length - 1] = to;
                                at = to;
                            }
                            codeMirror.operation(function () {
                                for (let i = 0; i < linesToMove.length; i += 2) {
                                    let from = linesToMove[i], to = linesToMove[i + 1];
                                    let line = codeMirror.getLine(from);
                                    codeMirror.replaceRange(
                                        '',
                                        CodeMirror.Pos(from, 0),
                                        CodeMirror.Pos(from + 1, 0),
                                        '+swapLine');
                                    if (to > codeMirror.lastLine())
                                        codeMirror.replaceRange(
                                            '\n' + line,
                                            CodeMirror.Pos(codeMirror.lastLine()),
                                            undefined,
                                            '+swapLine');
                                    else
                                        codeMirror.replaceRange(
                                            line + '\n',
                                            CodeMirror.Pos(to, 0),
                                            undefined,
                                            '+swapLine');
                                }
                                codeMirror.setSelections(newSels);
                            });
                        }
                    },
                    'Alt-Down': (codeMirror): void => {
                        if (!codeMirror.isReadOnly()) {
                            let ranges = codeMirror.listSelections(), linesToMove: number[] = [],
                                at = codeMirror.lastLine() + 1;
                            for (let i = ranges.length - 1; i >= 0; i--) {
                                let range = ranges[i], from = range.to().line + 1, to = range.from().line;
                                if (range.to().ch == 0 && !range.empty()) from--;
                                if (from < at) linesToMove.push(from, to);
                                else if (linesToMove.length) linesToMove[linesToMove.length - 1] = to;
                                at = to;
                            }
                            codeMirror.operation(function () {
                                for (let i = linesToMove.length - 2; i >= 0; i -= 2) {
                                    let from = linesToMove[i], to = linesToMove[i + 1];
                                    let line = codeMirror.getLine(from);
                                    if (from == codeMirror.lastLine())
                                        codeMirror.replaceRange(
                                            '',
                                            CodeMirror.Pos(from - 1),
                                            CodeMirror.Pos(from),
                                            '+swapLine');
                                    else
                                        codeMirror
                                            .replaceRange(
                                                '',
                                                CodeMirror.Pos(from, 0),
                                                CodeMirror.Pos(from + 1, 0),
                                                '+swapLine');
                                    codeMirror
                                        .replaceRange(
                                            line + '\n',
                                            CodeMirror.Pos(to, 0),
                                            undefined,
                                            '+swapLine');
                                }
                            });
                        }
                    },
                    'Shift-Space': 'autocomplete',
                    'Ctrl-/': 'toggleComment',
                    'Ctrl-Y': CodeMirror.commands.deleteLine,
                    'Ctrl-D': (cm: any) => {
                        cm.operation(function () {
                            let rangeCount = cm.listSelections().length;
                            for (let i = 0; i < rangeCount; i++) {
                                let range = cm.listSelections()[i];
                                if (range.empty())
                                    cm.replaceRange(cm.getLine(range.head.line) + '\n', CodeMirror.Pos(range.head.line, 0));
                                else
                                    cm.replaceRange(cm.getRange(range.from(), range.to()), range.from());
                            }
                            cm.scrollIntoView();
                        });
                    },
                    'Alt-Enter': () => {
                        this.execute();
                    }
                },
                spellcheck: true,
                autocorrect: true,
                lineNumbers: true,
                autoCloseBrackets: true,
                matchBrackets: true,
                gutters: [
                    'CodeMirror-linenumbers',
                    'CodeMirror-lint-markers',
                    {
                        className: 'CodeMirror-query-gutter',
                        style: 'width: 15px'
                    }
                ],
                lint: {
                    lintOnChange: true,
                    getAnnotations: this.sqlValidator.bind(this),
                },
                smartIndent: true,
                hintOptions: this._hintOptions,
                mode: this._codeMirrorMode
            });

        this._codeEditor.setValue(this.rawQuery);
        this._codeEditor.on('change', (editor: Editor) => {
            this._rawQuery = editor.getValue();
        });
    }

    // endregion Override Methods

    // region Public Methods

    public onExecuteRawSql(data: { sql: string, newTab: boolean }) {
        this.executeRawSql(data.sql, data.newTab).subscribe();
    }

    public onSelectDatabase(database: Database) {
        this._selectedDatabase = database;
    }

    public onSelectSchema(schema: Schema[]) {
        this.selectedDatabaseTables = [];
        this._schema = {};

        this._isFetchingInstanceData = true;

        schema.forEach(schema => {
            schema.tables?.forEach(table => {
                let tableView: TableSchemaViewModel = table as TableSchemaViewModel;
                tableView.schema = schema.name!;

                if (table.columns)
                    this._schema[`${tableView.schema}.${table.name!}`] = table.columns.map(column => column.name!);

                this._selectedDatabaseTables!.push(tableView);
            });
        });

        this._hintOptions.tables = this._schema;
        this._isFetchingInstanceData = false;
        this._codeEditor?.performLint();
    }

    @HostListener('document:keydown', ['$event'])
    closeResultModal(event: KeyboardEvent & { delegateTarget: any }): void {
        if (event.key == 'Escape' && this.tableViewPopupState && event.delegateTarget == null) {
            this.tableViewPopupState = false;
        }
    }

    addLineSelection(startLineNumber: number, endLineNumber: number): void {
        this._codeEditor?.extendSelection({
            ch: 0,
            line: startLineNumber
        }, {
            ch: 0,
            line: endLineNumber + 1
        });
    }

    removeLineSelection(): void {
        this._codeEditor?.undoSelection();
    }

    getQuerySql(queryIndex?: number): string | undefined {

        let sql: string | undefined = undefined;

        if (queryIndex != undefined)
            sql = this._sqlParser
                .sqlify(this._sqlParser.astify(this._queryList[queryIndex]!.rawQuery!, this._sqlParserOptions), this._sqlParserOptions);

        return sql;
    }

    execute(queryIndex: number | undefined = undefined, newTab: boolean = false): void {
        this._console.pushSeparator();
        this._console.pushSuccess('Executing...');
        this._console.pushMessage('Preparing queries...');

        if (newTab && this.results.length < 1)
            newTab = false;

        if (this.selectedDatabase && this.rawQuery.length > 0) {
            this.sqlValidator();

            if (this.queryLimit && this._queryList?.length) {
                this.console.pushMessage(`Applying limit '${this.queryLimit}' to SQL queries.`);
                this.applyLimitForSqlAst();
            }

            let queryList: string[] = [];

            if (queryIndex != undefined) {
                let query: QueryViewModel = this._queryList![queryIndex];

                if (query.queryAst) {
                    let queryAst: AST[] = this._queryList![queryIndex].queryAst!;

                    let lastSelectQuery = 0;

                    queryAst.forEach((ast, indexAst) => {
                        if (ast?.type == 'select')
                            lastSelectQuery = indexAst;
                    });

                    queryAst.forEach((ast, indexAst) => {
                        if (ast?.type != 'select' || (indexAst == lastSelectQuery))
                            queryList.push(this._sqlParser.sqlify(ast!, this._sqlParserOptions));
                    });
                } else
                    queryList.push(query.rawQuery!);

            } else {
                if (!this.isValidQuery) {
                    if (this.rawQuery.length > 0)
                        queryList.push(this.rawQuery);
                } else {
                    this._queryList.forEach((query, index) => {
                        let lastSelectQuery = 0;

                        query.queryAst?.forEach((ast, indexAst) => {
                            if (ast?.type == 'select')
                                lastSelectQuery = indexAst;
                        });

                        query.queryAst?.forEach((ast, indexAst) => {
                            if (ast?.type != 'select' || (index == this._lastSelectQueryIndex && indexAst == lastSelectQuery))
                                queryList.push(this._sqlParser.sqlify(ast!, this._sqlParserOptions));
                        });
                    });
                }
            }

            this._console.pushMessage(`Query to execute count: '${queryList.length}'.`);

            let isError$: Subject<any> = new Subject();

            from(queryList)
                .pipe(
                    concatMap(query => {
                        return this.executeRawSql(query, newTab);
                    }),
                    tap((response: Response<Query>) => {
                        if (response?.errorMessage) {
                            isError$.next(null);
                        }
                    }),
                    catchError(() => {
                        isError$.next(null);

                        return of(false);
                    }),
                    takeUntil(isError$))
                .subscribe();


        } else if (this.rawQuery.length <= 0) {
            this.console.pushWarning('Execution rejected. Reason: \'Query is empty\'.');
            this._isExecuting = false;
        } else if (!this.selectedDatabase) {
            this.isSidebar = true;

            this.console.pushWarning('Execution rejected. Reason: \'Select database before execution\'.');

            console.log(this.databaseViewer);

            this.databaseViewer.databaseSelector?.nativeElement.classList.add('btn-outline-danger');
            this.databaseViewer.databaseSelector?.nativeElement.scrollIntoView();
        }
    }

    selectLimit(element: HTMLElement, value?: any, hideMenu: boolean = true): void {

        if (hideMenu)
            element.hidden = true;

        this._queryLimit = value ? parseInt(value) : undefined;
    }

    onResizeSidebar(value: number): void {
        if (this.queryMain)
            this.queryMain.nativeElement.style.width = (100 - value) + '%';
    }

    closeResultTab(resultIndex: number): void {
        this.results.splice(resultIndex, 1);

        if (resultIndex == this.selectedTableIndex)
            this.selectedTableIndex = 0;

        if (resultIndex < this.selectedTableIndex)
            this.selectedTableIndex--;
    }

    insertNewLineToEditor(value: string): void {

        if (this.rawQuery.length > 0)
            this._codeEditor!.setValue(`${this.rawQuery}\n${value}`);
        else
            this._codeEditor!.setValue(value);
    }

    onResizeSidebarContent(sidebar: HTMLDivElement, navigation: HTMLDivElement): void {
        if (sidebar.scrollHeight > sidebar.clientHeight) {
            navigation.classList.add('d-flex');
            navigation.classList.remove('d-none');
        } else {
            navigation.classList.remove('d-flex');
            navigation.classList.add('d-none');
        }
    }

    scrollTo(element: HTMLElement, scrollTo: number): void {
        $(element).animate({scrollTop: scrollTo});
    }

    // endregion Public Methods
}