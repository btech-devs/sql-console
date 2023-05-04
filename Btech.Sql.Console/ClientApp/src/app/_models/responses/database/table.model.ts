import {Column} from './column.model';
import {Constraint} from './constraint.model';
import {Index} from './index.model';

export class Table {

    private _name?: string;
    private _columns?: Column[];
    private _constraints?: Constraint[];
    private _indexes?: Index[];

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

    get constraints(): Constraint[] | undefined {
        return this._constraints;
    }

    set constraints(value: Constraint[] | undefined) {
        this._constraints = value;
    }
    get indexes(): Index[] | undefined {
        return this._indexes;
    }

    set indexes(value: Index[] | undefined) {
        this._indexes = value;
    }
}