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

/**
 * Represents the database viewer component.
 */
@Component({
    selector: 'app-database-viewer',
    templateUrl: './database-viewer.component.html',
    styleUrls: ['./database-viewer.component.less']
})
export class DatabaseViewerComponent implements OnInit {
    constructor(private _databaseService: DatabaseService) {
    }

    /** Reference to the database selector element. */
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

    /** The query limit for database queries. */
    @Input() queryLimit?: number;

    /** The console for displaying messages. */
    @Input() set console(value: Console) {
        this._console = value;
    }

    /** Event emitted when raw SQL query should be executed. */
    @Output() onExecuteRawSql: EventEmitter<{ sql: string, newTab: boolean }> = new EventEmitter<{
        sql: string,
        newTab: boolean
    }>();

    /** Event emitted when a new line should be inserted. */
    @Output() onInsertNewLine: EventEmitter<string> = new EventEmitter<string>();

    /** Event emitted when a database is selected. */
    @Output() onSelectDatabase: EventEmitter<Database> = new EventEmitter<Database>();

    /** Event emitted when schemas are selected. */
    @Output() onSelectSchema: EventEmitter<Schema[]> = new EventEmitter<Schema[]>();

    /** Gets the host of the database. */
    get host(): string | undefined {
        return this._host;
    }

    /** Gets the databases. */
    get databases(): Database[] {
        return this._databases;
    }

    /** Gets whether instance data is being fetched. */
    get isFetchingInstanceData(): boolean {
        return this._isFetchingInstanceData;
    }

    /** Gets the selected database. */
    get selectedDatabase(): Database | undefined {
        return this._selectedDatabase;
    }

    /** Gets the database page. */
    get databasePage(): number {
        return this._databasePage;
    }

    /** Gets the selected schemas. */
    get selectedSchemas(): Schema[] {
        return this._selectedSchemas;
    }

    /** Gets the number of databases per page. */
    get databasesPerPage(): number | undefined {
        return this._databasesPerPage;
    }

    /** Gets the total amount of databases. */
    get databasesTotalAmount(): number | undefined {
        return this._databasesTotalAmount;
    }

    /** Gets the database search string. */
    get databaseSearch(): string {
        return this._databaseSearch;
    }

    /** Sets the selected database. */
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

    /**
     * Sets the current page number of the database list.
     * @param value The page number to set.
     */
    set databasePage(value: number) {
        if (value < 0)
            value = 0;

        if (this._databases.length > 0 || value <= this._databasePage) {
            this._databasePage = value;
            this.getDatabases();
        }
    }

    /**
     * Sets the search value for filtering the database list.
     * @param value The search value to set.
     */
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

    /**
     * Reloads the database structure.
     */
    reloadDatabaseStructure() {
        this.getDatabases();

        if (this.selectedDatabase?.name) {
            this.selectDatabase(this.selectedDatabase);
        }
    }

    /**
     * Retrieves the list of databases.
     */
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

    /**
     * Selects a database.
     * @param database The database to select.
     */
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

    /**
     * Retrieves additional information for a column.
     * @param column The column to retrieve information for.
     * @returns The additional information for the column.
     */
    getColumnAdditionalInfo(column: Column): string | undefined {
        let info: string | undefined = column.type;

        if (column.maxLength)
            info += ` (${column.maxLength})`;

        return info;
    }

    /**
     * Execute select query for table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     * @param newTab Determines if the table should be opened in a new tab.
     */
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

    /**
     * Views the row count of a table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     * @param newTab Determines if the row count should be opened in a new tab.
     */
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

    /**
     * Adds a select template for a table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     */
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

    /**
     * Adds an update template for a table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     */
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

    /**
     * Adds an insert template for a table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     */
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

    /**
     * Adds a delete template for a table.
     * @param schema The schema of the table.
     * @param table The name of the table.
     */
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

    /**
     * Loads the tables of a schema.
     * @param schema The schema to load tables for.
     * @param isHidden Determines if the tables should be hidden initially.
     * @param emitChanges Determines if changes should be emitted.
     */
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

    /**
     * Loads the columns of a table in a schema.
     * @param schema The schema of the table.
     * @param table The table to load columns for.
     * @param isHidden Determines if the columns should be hidden initially.
     */
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

    /**
     * Loads the views of a schema.
     * @param schema The schema to load views for.
     * @param isHidden Determines if the views should be hidden initially.
     */
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

    /**
     * Loads the routines of a schema.
     * @param schema The schema to load routines for.
     * @param isHidden Determines if the routines should be hidden initially.
     */
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

    /**
     * Loads the columns of a view in a schema.
     * @param schema The schema of the view.
     * @param view The view to load columns for.
     * @param isHidden Determines if the columns should be hidden initially.
     */
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

    /**
     * Emits changes on schema has been selected.
     */
    emitChanges() {
        this.onSelectSchema.emit(this._selectedSchemas);
    }

    /**
     * Handles the checkbox selection of a schema.
     * @param schema The selected schema.
     * @param event The event object.
     */
    onCheckboxSelectSchema(schema: Schema, event: any) {
        if (event?.target?.checked == true) {
            this._selectedSchemas.push(schema);

            this.loadTables(schema, true, true);
        } else {
            this._selectedSchemas = this._selectedSchemas.filter(existingSchema => existingSchema.name != schema.name);

            this.emitChanges();
        }
    }

    /**
     * Shows the schema folder.
     * @param schema The schema to show.
     * @param shown Determines if the schema is shown.
     */
    showSchemaFolder(schema: Schema, shown: boolean) {
        if (shown) {
            let checkbox: HTMLInputElement = (document.getElementById(`${schema.name}-checkbox`) as HTMLInputElement);

            if (checkbox) {
                checkbox.checked = true;
                checkbox.dispatchEvent(new Event('change'));
            }
        }
    }

    /**
     * Checks if a schema already selected.
     * @param schema The schema to check.
     * @returns True if the schema is selected, false otherwise.
     */
    isSelectedSchema(schema: Schema): boolean {
        return this.selectedSchemas.some(s => s.name == schema.name);
    }
}