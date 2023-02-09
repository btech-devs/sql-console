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
import {Table} from './models/table';
import * as CodeMirror from 'codemirror';
import {Editor, LineHandle} from 'codemirror';
import {Annotation} from 'codemirror/addon/lint/lint';
import {AST, Option, Select} from 'node-sql-parser/types';
import {Parser} from 'node-sql-parser';
import {QueryService} from '../../_services/query.service';
import {concatMap, from, Subject, takeUntil} from 'rxjs';
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
import {TableSchema} from '../../_models/responses/database/tableSchema.model';
import {Response} from '../../_models/responses/base/response';
import {DsvExporterComponent} from './dataExporters/dsvExporter/dsvExporter.component';
import {InstanceType} from '../../_models/instance-type';
import {SESSION_TOKEN_KEY} from '../../utils';
import {ConfirmModalComponent} from '../confirmModal/confirmModal.component';
import {SessionStorageService} from '../../_services/sessionStorageService';
import {Column} from '../../_models/responses/database/column.model';
import {PaginationResponse} from '../../_models/responses/base/pagination-response';

declare var $: any;

@Component({
    selector: 'app-query',
    templateUrl: './queryConsole.component.html',
    styleUrls: ['./queryConsole.component.less']
})
export class QueryConsoleComponent extends BaseComponent implements OnInit, AfterViewInit, OnDestroy {

    constructor(
        private _queryService: QueryService,
        private _instanceService: DatabaseService,
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
    private _queryList: { lines: LineHandle[], queryAst?: AST, rawQuery?: string }[] = [];
    private _tableViewPopupState: boolean = false;
    private _rawQuery: string = '';
    private _isValidQuery: boolean = true;
    private _anyValidSelect: number = 0;
    private _queryLimit?: number = 100;
    private _console: Console = new Console();
    private _results: Table[] = [];
    private _isExecuting: boolean = false;
    private _isFetchingInstanceData: boolean = true;
    private _isSidebar: boolean = true;
    private _databases: Database[] = [];
    private _selectedDatabase?: Database;
    private _selectedDatabaseTables?: TableSchema[];
    private _instanceType?: InstanceType;
    private _codeMirrorMode?: string;
    private _sqlParserOptions: Option = {};
    private _selectedTableIndex: number = 0;
    private _lastSelectQueryIndex?: number;
    private _executionCount: number = 0;
    private _host?: string;
    private _databasePage: number = 0;
    private _databasesTotalAmount?: number;
    private _databasesPerPage?: number;
    private _databaseSearch: string = '';
    private _databaseSearchTimeout?: NodeJS.Timeout;


    // endregion Private Fields

    // region Public Getters

    get tableViewPopupState(): boolean {
        return this._tableViewPopupState;
    }

    get databasePage(): number {
        return this._databasePage;
    }

    get databasesPerPage(): number | undefined {
        return this._databasesPerPage;
    }

    get databasesTotalAmount(): number | undefined {
        return this._databasesTotalAmount;
    }

    get databaseSearch(): string {
        return this._databaseSearch;
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

    get results(): Table[] {
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

    get databases(): Database[] {
        return this._databases;
    }

    get selectedDatabase(): Database | undefined {
        return this._selectedDatabase;
    }

    get selectedDatabaseTables(): TableSchema[] | undefined {
        return this._selectedDatabaseTables;
    }

    // endregion Public Getters

    // region Public Setters

    set results(value: Table[]) {
        this._results = value;
    }

    set databasePage(value: number) {
        if (value < 0)
            value = 0;

        if (this._databases.length > 0 || value <= this._databasePage) {
            this._databasePage = value;
            this.getDatabases();
        }
    }

    set databaseSearch(value: string) {
        this._databaseSearch = value;

        if (this._databaseSearchTimeout)
            clearTimeout(this._databaseSearchTimeout);

        this._databaseSearchTimeout = setTimeout(
            () => {
                this.databasePage = 0;
                this.getDatabases();
            },
            300);
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

    set selectedDatabase(value) {
        this._selectedDatabase = value;
        this.databaseSelector?.nativeElement.classList.remove('btn-outline-danger');

        $(this.databaseSelector?.nativeElement).click();

        if (value != undefined) {
            this.selectDatabase();
        }
    }

    set selectedTableIndex(value: number) {
        this._selectedTableIndex = value;
    }

    // endregion Public Setters

    // region ViewChild fields

    @ViewChild('textArea') textArea?: ElementRef;
    @ViewChild('consoleHandle') consoleHandle?: ElementRef;
    @ViewChild('resultHandle') resultHandle?: ElementRef;
    @ViewChild('databaseSelector') databaseSelector?: ElementRef;
    @ViewChild('queryMain') queryMain?: ElementRef;
    @ViewChild('querySidebar') querySidebar?: ElementRef;
    @ViewChild('dsvExporterComponent') dsvExporter?: DsvExporterComponent;
    @ViewChild('confirmModalComponent') confirmModalComponent?: ConfirmModalComponent;
    @ViewChild('queryGutter') queryGutter!: TemplateRef<{
        type: string,
        startLine: number,
        endLine: number,
        queryIndex: number
    }>;

    // endregion ViewChild fields

    // region Private Methods

    private applyLimitForSqlAst(): void {
        this._queryList.map(query => query.queryAst)!.forEach(sqlAst => {
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

        let query: {
            lines: LineHandle[],
            queryAst?: AST,
            rawQuery?: string
        } = {lines: []};

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

        this._anyValidSelect = 0;

        this._queryList.forEach((query, index) => {
            let isSyntaxError: boolean = false;

            let isValidQuery = true;

            try {
                this.validateSchema(query, this._sqlParserOptions);
            } catch (exception: any) {

                let location: any = exception?.location;
                let name: any = exception?.name;
                let message: any = exception?.message;

                isValidQuery = false;

                if (name != 'Schema') {
                    isSyntaxError = true;
                    this._isValidQuery = false;
                }

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
                        severity: 'warning',
                        message: `${name}: ${message}`,
                    });
                }
            }

            if (query.queryAst && query.queryAst.type == 'select' && isValidQuery) {
                this._anyValidSelect++;
            }

            if (query.queryAst && query.queryAst.type == 'select')
                this._lastSelectQueryIndex = index;

            if (!isSyntaxError && this.selectedDatabase?.name) {
                this.addQueryExecution(query, index);
            }
        });

        if (this.rawQuery.length == 0)
            this._isValidQuery = false;

        return errors;
    }

    private addQueryExecution(query: { lines: LineHandle[], rawQuery?: string, queryAst?: AST }, queryIndex: number): void {
        if (query.lines.length > 0) {

            let startLine = this._codeEditor?.getLineNumber(query.lines[0]);
            let endLine = this._codeEditor?.getLineNumber(query.lines[query.lines.length - 1]);

            let queryGutter = this.queryGutter.createEmbeddedView({
                type: query.queryAst!.type!,
                startLine: startLine!,
                endLine: endLine!,
                queryIndex: queryIndex
            });

            queryGutter.detectChanges();

            this._codeEditor?.setGutterMarker(startLine, 'CodeMirror-query-gutter', queryGutter.rootNodes[0]);
        }
    }

    private validateSchema(query: { lines: LineHandle[], queryAst?: AST, rawQuery?: string }, option: Option): void {

        query.rawQuery = query.lines.map(line => line.text).join('\n');

        // validate syntax
        // thrown exception on syntax error

        try {
            let ast: AST | AST[] = this._sqlParser.astify(query.rawQuery, option);

            if (ast instanceof Array)
                ast = ast[0];

            query.queryAst = ast;
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

        if (query.queryAst.type != 'create') {
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
                    if (!this._schema[tableName]) {
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

                if (this._schema[tableName] && !(this._schema[tableName].includes(columnName))) {
                    query.lines.forEach((queryLine, lineIndex) => {
                        let columnNameFirstCharacterPosition = queryLine.text
                            .search(new RegExp(`(?<=\\.")${columnName}`));

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
            if (columnList.length > 0) {
                columnListWithoutTables.forEach(columnName => {
                    let tableThatContainsColumns: number = 0;

                    tableList.forEach(tableName => {
                        if ((tableName in this._schema) && this._schema[tableName].includes(columnName)) {
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
                    if (this._schema[tableName]) {
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
            this._titleService.setTitle(`SQL Console | ${this._instanceType} | ${this._host}`)

        this.getDatabases();
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
                    'Ctrl-Space': 'autocomplete',
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
                gutters: ['CodeMirror-lint-markers', 'CodeMirror-linenumbers', {
                    className: 'CodeMirror-query-gutter',
                    style: 'width: 15px'
                }],
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
        let executeFunc: Function = () => {
            this._isExecuting = true;

            this.console.pushMessage(`====== Executing ======`);

            if (newTab && this.results.length < 1)
                newTab = false;

            if (this.selectedDatabase && this.rawQuery.length > 0) {
                this.sqlValidator();

                if (this.queryLimit) {
                    this.console.pushMessage(`Applying limit '${this.queryLimit}' to SQL queries.`);
                    this.applyLimitForSqlAst();
                }

                let queryList: string[] = [];

                if (queryIndex != undefined) {
                    queryList.push(this._sqlParser?.sqlify(this._queryList![queryIndex].queryAst!, this._sqlParserOptions)!);
                } else {
                    if (!this.isValidQuery) {
                        if (this.rawQuery.length > 0)
                            queryList.push(this.rawQuery);
                    } else {
                        this._queryList.forEach((query, index) => {
                            if (query.queryAst?.type != 'select' || index == this._lastSelectQueryIndex)
                                queryList.push(this._sqlParser.sqlify(query.queryAst!, this._sqlParserOptions));
                        });
                    }
                }

                let hasError: boolean = false;
                let executionIndex: number = 0;
                let lastResponseData: Query | undefined = undefined;

                let hasError$ = new Subject<boolean>();

                hasError$.subscribe((value: boolean) => {
                    hasError = value;
                });

                from(queryList)
                    .pipe(
                        concatMap(query => {
                            lastResponseData = undefined;

                            this.console.pushMessage(`Executing query: '${query}'.`);

                            return this._queryService
                                .execute(query, this.selectedDatabase?.name!);

                        }),
                        takeUntil(hasError$))
                    .subscribe({
                        next: (response: Response<Query>) => {
                            executionIndex++;
                            this._executionCount++;


                            if (response.errorMessage != undefined) {
                                hasError$.next(true);

                                const messagePart = 'Something went wrong on executing query.';

                                if (response.errorMessage)
                                    this.console.pushDanger(`${messagePart} ErrorMessage: '${response.errorMessage}'`);
                                else if (response.errorMessage)
                                    this.console.pushDanger(`${messagePart} ErrorMessages: '${response.errorMessages?.join(',')}'`);
                            } else {
                                lastResponseData = response.data;

                                if (lastResponseData?.recordsAffected != undefined && lastResponseData.recordsAffected >= 0) {
                                    if (lastResponseData.elapsedTimeMs)
                                        this.console.pushSuccess(`Result: ${lastResponseData.recordsAffected} row(s) affected. Elapsed time: ${lastResponseData.elapsedTimeMs} ms.`);
                                    else
                                        this.console.pushSuccess(`Result: ${lastResponseData.recordsAffected} row(s) affected.`);
                                } else {
                                    if (lastResponseData?.elapsedTimeMs)
                                        this.console.pushSuccess(`Successfully executed. Elapsed time: ${lastResponseData.elapsedTimeMs} ms.`);
                                    else
                                        this.console.pushSuccess(`Successfully executed.`);
                                }
                            }
                        },
                        error: (error) => {
                            console.log(error);
                            hasError$.next(true);

                            this.console.pushDanger(error?.message);
                        },
                        complete: () => {
                            hasError$.complete();

                            if (hasError)
                                this.consoleHandle?.nativeElement.click();
                            else {
                                if (lastResponseData?.table != undefined) {
                                    let table = Table.createFromQueryTable(lastResponseData.table);


                                    if (newTab) {
                                        if (this.results.length > 5) {
                                            this._executionCount = 2;
                                            this.results = this.results.slice(0, 1);
                                        }

                                        table.name = `Execution ${this._executionCount - 1}`;

                                        this.selectedTableIndex = this.results.length;
                                        this.results.push(table);
                                    } else {
                                        table.name = 'Main execution';
                                        this.results[0] = table;
                                        this.selectedTableIndex = 0;
                                    }
                                    this.console.pushSuccess(`Result: ${table.body.length} row(s) with ${table.header?.length} column(s) retrieved.`);
                                }

                                if (this.results.length > 0)
                                    this.resultHandle?.nativeElement.click();
                            }

                            this._isExecuting = false;
                        }
                    });
            } else if (this.rawQuery.length <= 0) {
                this.console.pushWarning('Execution rejected. Reason: \'Query is empty\'.');
                this._isExecuting = false;
            } else if (!this.selectedDatabase) {
                this.isSidebar = true;
                this.databaseSelector?.nativeElement.classList.add('btn-outline-danger');
                this.databaseSelector?.nativeElement.scrollIntoView();
                this.console.pushWarning('Execution rejected. Reason: \'Select database before execution\'.');
                this._isExecuting = false;
            }
        };

        if (newTab && this.results.length == 6)
            this.confirmModalComponent!.confirm(executeFunc, undefined, 'All saved results but Main will be closed. Execute?');
        else
            // Run in setTimeout without blocking UI
            setTimeout(executeFunc, 1);

    }

    executeRawSql(sql: string, newTab: boolean = false) {
        let executeFunc: Function = () => {
            this._isExecuting = true;
            this._executionCount++;

            this.console.pushMessage(`====== Executing ======`);

            if (newTab && this.results.length < 1)
                newTab = false;

            this._queryService.execute(sql, this.selectedDatabase!.name!)
                .subscribe({
                    next: (response: Response<Query>) => {
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
                                let table = Table.createFromQueryTable(response.data.table);

                                if (newTab) {
                                    if (this.results.length > 5) {
                                        this._executionCount = 2;
                                        this.results = this.results.slice(0, 1);
                                    }

                                    table.name = `Execution ${this._executionCount - 1}`;

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

                        this._isExecuting = false;
                    },
                    error: (error) => {
                        console.log(error);

                        this.console.pushDanger(error?.message);
                    }
                });
        };

        if (newTab && this.results.length == 6)
            this.confirmModalComponent!.confirm(executeFunc, undefined, 'All saved results but Main will be closed. Execute?');
        else
            // Run in setTimeout without blocking UI
            setTimeout(executeFunc, 1);
    }

    selectLimit(element: HTMLElement, value?: any, hideMenu: boolean = true): void {

        if (hideMenu)
            element.hidden = true;

        this._queryLimit = value ? parseInt(value) : undefined;
    }

    selectDatabase(): void {
        this._isFetchingInstanceData = true;
        this._selectedDatabaseTables = undefined;
        this.console.pushMessage(`Start fetching schema for database '${this.selectedDatabase?.name}'.`);
        this._instanceService
            .getDatabase(this.selectedDatabase!.name!)
            .subscribe({
                next: response => {
                    if (response.errorMessage) {
                        this.console.pushDanger(`Something went`);
                    } else if (response.data) {
                        this._schema = {};

                        response.data.tables?.forEach(table => {
                            this._schema[table.name!] = table.columns!.map(column => column.name!);
                        });

                        this._selectedDatabaseTables = response.data!.tables;

                        this._hintOptions.tables = this._schema;

                        this._codeEditor?.performLint();
                    }
                },
                error: error => {
                    this.console.pushDanger(error.message);
                    this._isFetchingInstanceData = false;
                },
                complete: () => this._isFetchingInstanceData = false
            });
    }

    getColumnAdditionalInfo(column: Column): string | undefined {
        let info: string | undefined = column.type;

        if (column.isPrimaryKey == true)
            info += ' (PK)';
        else if (column.isForeignKey == true)
            info += ' (FK)';
        else if (column.maxLength)
            info += ` (${column.maxLength})`;

        return info;
    }

    onResizeSidebar(value: number): void {
        if (this.queryMain)
            this.queryMain.nativeElement.style.width = (100 - value) + '%';
    }

    getDatabases(): void {
        this._isFetchingInstanceData = true;

        this._instanceService
            .getDatabases(this._databasePage, 5, this._databaseSearch)
            .subscribe({
                next: (response: PaginationResponse<Database>) => {
                    if (response.errorMessage)
                        this.console.pushDanger(`Something went wrong on fetching database list. Message: '${response.errorMessage}'.`);
                    else if (response.entities) {
                        this._databasesTotalAmount = response.totalAmount;
                        this._databasesPerPage = response.perPage;
                        this._databases = response.entities;
                    }
                },
                error: error => {
                    this._databases = [];
                    this.console.pushDanger(error.message);
                    this._isFetchingInstanceData = false;
                },
                complete: () => {
                    this._isFetchingInstanceData = false;
                }
            });
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

    viewTable(table: string, newTab: boolean = false): void {
        let viewTableSql: string = '';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                viewTableSql = `SELECT * FROM "${table}" LIMIT ${this.queryLimit}`;
                break;
            }
            case InstanceType.MsSql: {
                // @ts-ignore
                viewTableSql = `SELECT TOP ${this.queryLimit} * FROM [${table}]`;
                break;
            }
        }
        // @formatter:on

        this.executeRawSql(viewTableSql, newTab);
    }

    viewRowCount(table: string, newTab: boolean = false): void {
        let rowCountSql: string = '';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                rowCountSql = `SELECT COUNT(*) FROM "${table}"`;
                break;
            }
            case InstanceType.MsSql: {
                // @ts-ignore
                rowCountSql = `SELECT COUNT(*) FROM [${table}]`;
                break;
            }
        }
        // @formatter:on

        this.executeRawSql(rowCountSql, newTab);
    }

    addSelectTemplate(table: string): void {
        let selectFromTablePattern: string = '';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                selectFromTablePattern = `SELECT "column_1", "column_2" FROM "${table}" WHERE condition...;`;
                break;
            }
            case InstanceType.MsSql: {
                selectFromTablePattern = `SELECT [column_1], [column_2] FROM [${table}] WHERE condition...;`;
                break;
            }
        }
        // @formatter:on

        this.insertNewLineToEditor(selectFromTablePattern);
    }

    addUpdateTemplate(table: string): void {
        let updateTablePattern: string = '';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                updateTablePattern = `UPDATE "${table}" SET "column" = 'value' WHERE condition...;`
                break;
            }
            case InstanceType.MsSql: {
                updateTablePattern = `UPDATE [${table}] SET [column] = 'value' WHERE condition...;`
                break;
            }
        }
        // @formatter:on

        this.insertNewLineToEditor(updateTablePattern);
    }

    addInsertTemplate(table: string): void {
        let insertTablePattern: string = '';
        let tableSchema: TableSchema = this.selectedDatabaseTables!.find(tableSchema => tableSchema.name == table)!;

        let insertColumnsPattern = '(';

        tableSchema.columns?.forEach((column: Column, index: number) => {
            if (index != 0) {
                insertColumnsPattern += ', ';
            }

            // @formatter:off
            switch (this._instanceType) {
                case InstanceType.PgSql: {
                    insertColumnsPattern += `"${column.name}"`;

                    break;
                }
                case InstanceType.MsSql: {
                    insertColumnsPattern += `[${column.name}]`;

                    break;
                }
            }
            // @formatter:on

        });

        insertColumnsPattern += ')';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                insertTablePattern = `INSERT INTO "${table}" ${insertColumnsPattern} VALUES(value1, value2, ...)`
                break;
            }
            case InstanceType.MsSql: {
                insertTablePattern = `INSERT INTO [${table}] ${insertColumnsPattern} VALUES(value1, value2, ...)`
                break;
            }
        }
        // @formatter:on

        this.insertNewLineToEditor(insertTablePattern);
    }

    addDeleteTemplate(table: string): void {
        let deleteFromTablePattern: string = '';

        // @formatter:off
        switch (this._instanceType) {
            case InstanceType.PgSql: {
                deleteFromTablePattern = `DELETE FROM "${table}" WHERE condition...;`
                break;
            }
            case InstanceType.MsSql: {
                deleteFromTablePattern = `DELETE FROM [${table}] WHERE condition...;`
                break;
            }
        }
        // @formatter:on

        this.insertNewLineToEditor(deleteFromTablePattern);
    }

    // endregion Public Methods
}