import {QueryColumn} from './queryColumn.model';

export class QueryTable {
    private _columns?: QueryColumn[];
    private _rows?: string[][];

    get columns(): QueryColumn[] | undefined {
        return this._columns;
    }

    get rows(): string[][] | undefined {
        return this._rows;
    }

    set columns(value: QueryColumn[] | undefined) {
        this._columns = value;
    }

    set rows(value: string[][] | undefined) {
        this._rows = value;
    }
}