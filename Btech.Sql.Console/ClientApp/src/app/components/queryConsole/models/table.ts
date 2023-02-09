import {QueryTable} from '../../../_models/responses/query/queryTable.model';
import {QueryColumn} from '../../../_models/responses/query/queryColumn.model';

export class Table {
    private _name: string = '';
    private _header: TableCell[] = new Array<TableCell>();
    private _body: TableCell[][] = [];

    get header(): TableCell[] {
        return this._header;
    }

    get body(): TableCell[][] {
        return this._body;
    }

    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
    }

    constructor() {
    }

    static createFromQueryTable(queryTable: QueryTable, name: string = ''): Table {
        let table = new Table();

        table.name = name;

        queryTable.columns?.forEach(column => {
            table.header.push(new TableCell(column.name, undefined, column.type));
        });

        queryTable.rows?.forEach(row => {
            let tableRow: TableCell[] = [];

            let columnIndex = 0;

            row.forEach(value => {
                let queryColumn: QueryColumn = queryTable.columns![columnIndex++];

                tableRow.push(new TableCell(queryColumn.name, value, queryColumn.type));
            });

            table.body.push(tableRow);
        });

        return table;
    }
}

class TableCell {
    private readonly _value?: string;
    private readonly _columnName?: string;
    private readonly _type?: string;
    private _isExpandable: boolean = false;
    private _expanded: boolean = false;

    get columnName(): string | undefined {
        return this._columnName;
    }

    get type(): string | undefined {
        return this._type;
    }

    get isExpandable(): boolean {
        return this._isExpandable;
    }

    get expanded(): boolean {
        return this._expanded;
    }

    set expanded(value: boolean) {
        this._expanded = value;
    }

    get value(): string | undefined {
        let result: string | undefined;

        if (this._value && this.type == 'bytea' && !this._value.startsWith('H4sI')) {
            result = Buffer.from(this._value, 'base64').toString('utf8');

            if (result && result?.length > 128)
                this._isExpandable = true;
        } else
            result = this._value;

        return result;
    }

    constructor(columnName?: string, value?: string, type?: string) {
        this._value = value;

        this._columnName = columnName;
        this._type = type;

        if (value && value?.length > 128)
            this._isExpandable = true;
    }
}