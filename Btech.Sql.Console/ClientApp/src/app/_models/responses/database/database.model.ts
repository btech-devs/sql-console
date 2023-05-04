import {Schema} from './schema.model';

export class Database {
    private _name?: string;
    private _schemas?: Schema[];

    get name(): string | undefined {
        return this._name;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    get schemas(): Schema[] | undefined{
        return this._schemas;
    }

    set schemas(value: Schema[] | undefined) {
        this._schemas = value;
    }
}