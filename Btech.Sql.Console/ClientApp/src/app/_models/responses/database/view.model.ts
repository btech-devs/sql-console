import {Column} from './column.model';

export class View {
    private _name?: string;
    private _columns?: Column[];

    get name(): string | undefined {
        return this._name;
    }

    get columns(): Column[] | undefined {
        return this._columns;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    set columns(value: Column[] | undefined) {
        this._columns = value;
    }
}