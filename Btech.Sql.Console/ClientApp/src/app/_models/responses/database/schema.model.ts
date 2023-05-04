import {View} from './view.model';
import {Routine} from './routine.model';
import {Table} from './table.model';

export class Schema {
    private _name?: string;
    private _tables?: Table[];
    private _views?: View[];
    private _routines?: Routine[];

    get name(): string | undefined {
        return this._name;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    get routines(): Routine[] | undefined {
        return this._routines;
    }

    set routines(value: Routine[] | undefined) {
        this._routines = value;
    }

    get views(): View[] | undefined {
        return this._views;
    }

    set views(value: View[] | undefined) {
        this._views = value;
    }

    get tables(): Table[] | undefined {
        return this._tables;
    }

    set tables(value: Table[] | undefined) {
        this._tables = value;
    }
}