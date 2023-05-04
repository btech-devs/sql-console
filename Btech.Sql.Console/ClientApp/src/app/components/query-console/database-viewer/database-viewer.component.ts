import {Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild} from '@angular/core';
import {PaginationResponse} from '../../../_models/responses/base/pagination-response';
import {Database} from '../../../_models/responses/database/database.model';
import {Table} from '../../../_models/responses/database/table.model';
import {Column} from '../../../_models/responses/database/column.model';
import {SESSION_TOKEN_KEY} from '../../../utils';
import {DatabaseService} from '../../../_services/database.service';
import {Console} from '../models/console';
import {InstanceType} from '../../../_models/instance-type';
import {SessionStorageService} from '../../../_services/sessionStorageService';
import {Schema} from '../../../_models/responses/database/schema.model';
import {View} from '../../../_models/responses/database/view.model';

declare var $: any;

@Component({
    selector: 'app-database-viewer',
    templateUrl: './database-viewer.component.html',
    styleUrls: ['./database-viewer.component.less']
})
export class DatabaseViewerComponent implements OnInit {
    constructor(private _databaseService: DatabaseService) {
    }

    @ViewChild('databaseSelector') databaseSelector?: ElementRef;

    private _console: Console = new Console();
    private _databases: Database[] = [];
    private _databasePage: number = 0;
    private _databasesTotalAmount?: number;
    private _databasesPerPage?: number;
    private _databaseSearch: string = '';
    private _databaseSearchTimeout?: NodeJS.Timeout;
    private _selectedDatabase?: Database;
    private _isFetchingInstanceData: boolean = false;
    private _instanceType?: InstanceType;
    private _host?: string;
    private _selectedSchemas: Schema[] = [];

    @Input() queryLimit?: number;

    @Input() set console(value: Console) {
        this._console = value;
    }

    @Output() onExecuteRawSql: EventEmitter<{ sql: string, newTab: boolean }> = new EventEmitter<{
        sql: string,
        newTab: boolean
    }>();
    @Output() onInsertNewLine: EventEmitter<string> = new EventEmitter<string>();

    @Output() onSelectDatabase: EventEmitter<Database> = new EventEmitter<Database>();
    @Output() onSelectSchema: EventEmitter<Schema[]> = new EventEmitter<Schema[]>();

    get host(): string | undefined {
        return this._host;
    }

    get databases(): Database[] {
        return this._databases;
    }

    get isFetchingInstanceData(): boolean {
        return this._isFetchingInstanceData;
    }

    get selectedDatabase(): Database | undefined {
        return this._selectedDatabase;
    }

    get databasePage(): number {
        return this._databasePage;
    }

    get selectedSchemas(): Schema[] {
        return this._selectedSchemas;
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

    set selectedDatabase(value) {
        this._selectedDatabase = value;
        this.databaseSelector?.nativeElement.classList.remove('btn-outline-danger');

        $(this.databaseSelector?.nativeElement).dropdown('hide');
    }

    ngOnInit(): void {

        let tokenClaims: any = JSON.parse(window.atob(SessionStorageService.get(SESSION_TOKEN_KEY)!.split('.')[1]));

        let rawInstanceType = tokenClaims?.instance_type;
        this._host = tokenClaims?.host;

        switch (rawInstanceType) {
            case InstanceType.PgSql: {
                this._instanceType = InstanceType.PgSql;
                break;
            }
            case InstanceType.MsSql: {
                this._instanceType = InstanceType.MsSql;
                break;
            }
            default: {
                break;
            }
        }

        this.getDatabases();
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
                this._databasePage = 0;
                this.getDatabases();
            },
            300);
    }

    reloadDatabaseStructure() {
        this.getDatabases();

        if (this.selectedDatabase?.name) {
            this.selectDatabase(this.selectedDatabase);
        }
    }

    getDatabases(): void {
        this._isFetchingInstanceData = true;

        this._databaseService
            .getDatabases(this._databasePage, 5, this._databaseSearch)
            .subscribe({
                next: (response: PaginationResponse<Database>) => {
                    if (response.errorMessage)
                        this._console.pushDanger(`Something went wrong on fetching database list. Message: '${response.errorMessage}'.`);
                    else if (response.entities) {
                        this._databasesTotalAmount = response.totalAmount;
                        this._databasesPerPage = response.perPage;
                        this._databases = response.entities;
                    }
                },
                error: error => {
                    this._databases = [];
                    this._console.pushDanger(error.message);
                    this._isFetchingInstanceData = false;
                },
                complete: () => {
                    this._isFetchingInstanceData = false;
                }
            });
    }

    selectDatabase(database: Database): void {
        this._isFetchingInstanceData = true;


        let selectedSchemaNames: string[] = [];

        if (this._selectedDatabase?.name == database?.name) {
            selectedSchemaNames = this.selectedSchemas.map((schema: Schema) => schema.name!);
        }

        this._selectedSchemas = [];
        this.emitChanges();

        this.selectedDatabase = database;

        this._console.pushMessage(`Start fetching schema for database '${this.selectedDatabase?.name}'.`);
        this._databaseService
            .getDatabase(this.selectedDatabase!.name!)
            .subscribe({
                next: response => {
                    if (response.errorMessage) {
                        this._console.pushDanger(`Something went wrong on fetching database list. Message: '${response.errorMessage}'.`);
                    } else if (response.data) {
                        this.selectedDatabase = response.data;

                        this.onSelectDatabase.emit(this.selectedDatabase);
                    }
                },
                error: error => {
                    this._console.pushDanger(error.message);
                    this._isFetchingInstanceData = false;
                },
                complete: () => this._isFetchingInstanceData = false
            })
            .add(() => {
                this.selectedDatabase?.schemas?.forEach(schema => {
                    if (selectedSchemaNames.some(selectedSchemaName => selectedSchemaName == schema.name)) {
                        this._selectedSchemas.push(schema);
                        this.loadTables(schema, true, true);
                    }
                })
            });
    }

    getColumnAdditionalInfo(column: Column): string | undefined {
        let info: string | undefined = column.type;

        if (column.maxLength)
            info += ` (${column.maxLength})`;

        return info;
    }

    viewTable(schema: string, table: string, newTab: boolean = false): void {
        let viewTableSql: string = '';

        // @formatter:off
    switch (this._instanceType) {
      case InstanceType.PgSql: {
        viewTableSql = `SELECT * FROM "${schema}"."${table}" LIMIT ${this.queryLimit}`;
        break;
      }
      case InstanceType.MsSql: {
        // @ts-ignore
        viewTableSql = `SELECT TOP ${this.queryLimit} * FROM [${schema}].[${table}]`;
        break;
      }
    }
    // @formatter:on

        this.onExecuteRawSql.emit({sql: viewTableSql, newTab: newTab});
    }

    viewRowCount(schema: string, table: string, newTab: boolean = false): void {
        let rowCountSql: string = '';

        // @formatter:off
    switch (this._instanceType) {
      case InstanceType.PgSql: {
        rowCountSql = `SELECT COUNT(*) FROM "${schema}"."${table}"`;
        break;
      }
      case InstanceType.MsSql: {
        // @ts-ignore
        rowCountSql = `SELECT COUNT(*) FROM [${schema}].[${table}]`;
        break;
      }
    }
    // @formatter:on

        this.onExecuteRawSql.emit({sql: rowCountSql, newTab: newTab});
    }

    addSelectTemplate(schema: string, table: string): void {
        let selectFromTablePattern: string = '';

        // @formatter:off
    switch (this._instanceType) {
      case InstanceType.PgSql: {
        selectFromTablePattern = `SELECT "column_1", "column_2" FROM "${schema}"."${table}" WHERE condition...;`;
        break;
      }
      case InstanceType.MsSql: {
        selectFromTablePattern = `SELECT [column_1], [column_2] FROM [${schema}].[${table}] WHERE condition...;`;
        break;
      }
    }
    // @formatter:on

        this.onInsertNewLine.emit(selectFromTablePattern);
    }

    addUpdateTemplate(schema: string, table: string): void {
        let updateTablePattern: string = '';

        // @formatter:off
    switch (this._instanceType) {
      case InstanceType.PgSql: {
        updateTablePattern = `UPDATE "${schema}"."${table}" SET "column" = 'value' WHERE condition...;`
        break;
      }
      case InstanceType.MsSql: {
        updateTablePattern = `UPDATE [${schema}].[${table}] SET [column] = 'value' WHERE condition...;`
        break;
      }
    }
    // @formatter:on

        this.onInsertNewLine.emit(updateTablePattern);
    }

    addInsertTemplate(schema: string, table: string): void {
        let insertTablePattern: string = '';
        let tableSchema: Table = this.selectedDatabase!.schemas!
            .find(existingSchema => existingSchema.name == schema)!
            .tables!
            .find(tableSchema => tableSchema.name == table)!;

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
        insertTablePattern = `INSERT INTO "${schema}"."${table}" ${insertColumnsPattern} VALUES(value1, value2, ...)`
        break;
      }
      case InstanceType.MsSql: {
        insertTablePattern = `INSERT INTO [${schema}].[${table}] ${insertColumnsPattern} VALUES(value1, value2, ...)`
        break;
      }
    }
    // @formatter:on

        this.onInsertNewLine.emit(insertTablePattern);
    }

    addDeleteTemplate(schema: string, table: string): void {
        let deleteFromTablePattern: string = '';

        // @formatter:off
    switch (this._instanceType) {
      case InstanceType.PgSql: {
        deleteFromTablePattern = `DELETE FROM "${schema}"."${table}" WHERE condition...;`
        break;
      }
      case InstanceType.MsSql: {
        deleteFromTablePattern = `DELETE FROM [${schema}].[${table}] WHERE condition...;`
        break;
      }
    }
    // @formatter:on

        this.onInsertNewLine.emit(deleteFromTablePattern);
    }

    loadTables(schema: Schema, isHidden: boolean = false, emitChanges: boolean = false): void {
        if (isHidden && !schema.tables?.length) {
            this._databaseService.getSchemaTables(this.selectedDatabase!.name!, schema!.name!)
                .subscribe(response => {
                    if (response.errorMessage) {
                        this._console.pushDanger(response.errorMessage);
                    }

                    schema.tables = response.data?.tables ?? [];

                    this.emitChanges();
                });
        } else if (!isHidden) {
            schema.tables = undefined;
        } else if (emitChanges)
            this.emitChanges();
    }

    loadTableColumns(schema: Schema, table: Table, isHidden: boolean = false): void {
        if (isHidden) {
            this._databaseService.getSchemaTable(this.selectedDatabase!.name!, schema!.name!, table!.name!)
                .subscribe(response => {
                    if (response.errorMessage) {
                        this.console.pushDanger(response.errorMessage);
                    }


                    table.columns = response.data?.columns ?? [];
                    table.constraints = response.data?.constraints ?? [];
                    table.indexes = response.data?.indexes ?? [];
                });
        } else {
            table.columns = undefined;
            table.constraints = undefined;
            table.indexes = undefined;
        }
    }

    loadViews(schema: Schema, isHidden: boolean = false): void {
        if (isHidden) {
            this._databaseService.getSchemaViews(this.selectedDatabase!.name!, schema!.name!)
                .subscribe(response => {
                    if (response.errorMessage) {
                        this._console.pushDanger(response.errorMessage);
                    }

                    schema.views = response.data?.views ?? [];
                });
        } else {
            schema.views = undefined;
        }
    }

    loadRoutines(schema: Schema, isHidden: boolean = false): void {
        if (isHidden) {
            this._databaseService.getSchemaRoutines(this.selectedDatabase!.name!, schema!.name!)
                .subscribe(response => {
                    if (response.errorMessage) {
                        this._console.pushDanger(response.errorMessage);
                    }

                    schema.routines = response.data?.routines ?? [];
                });
        } else {
            schema.routines = undefined;
        }
    }

    loadViewColumns(schema: Schema, view: View, isHidden: boolean = false): void {
        if (isHidden) {
            this._databaseService.getViewColumns(this.selectedDatabase!.name!, schema!.name!, view!.name!)
                .subscribe(response => {
                    if (response.errorMessage) {
                        this._console.pushDanger(response.errorMessage);
                    }

                    view.columns = response.data?.columns ?? [];
                });
        } else {
            view.columns = undefined;
        }
    }

    emitChanges() {
        this.onSelectSchema.emit(this._selectedSchemas);
    }

    onCheckboxSelectSchema(schema: Schema, event: any) {
        if (event?.target?.checked == true) {
            this._selectedSchemas.push(schema);

            this.loadTables(schema, true, true);
        } else {
            this._selectedSchemas = this._selectedSchemas.filter(existingSchema => existingSchema.name != schema.name);

            this.emitChanges();
        }
    }

    showSchemaFolder(schema: Schema, shown: boolean) {
        if (shown) {
            let checkbox: HTMLInputElement = (document.getElementById(`${schema.name}-checkbox`) as HTMLInputElement);

            if (checkbox) {
                checkbox.checked = true;
                checkbox.dispatchEvent(new Event('change'));
            }
        }
    }

    isSelectedSchema(schema: Schema): boolean {
        return this.selectedSchemas.some(s => s.name == schema.name);
    }
}