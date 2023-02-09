import {TableSchema} from './tableSchema.model';

export class Database {
    private _name?: string;
    private _tables?: TableSchema[];

    get name(): string | undefined {
        return this._name;
    }

    get tables(): TableSchema[] | undefined {
        return this._tables;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    set tables(value: TableSchema[] | undefined) {
        this._tables = value;
    }
}