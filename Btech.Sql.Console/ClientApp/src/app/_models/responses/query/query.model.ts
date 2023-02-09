import {QueryTable} from './queryTable.model';

export class Query {
    private _recordsAffected?: number;
    private _elapsedTimeMs?: number;
    private _table?: QueryTable;

    get recordsAffected(): number | undefined {
        return this._recordsAffected;
    }

    get elapsedTimeMs(): number | undefined {
        return this._elapsedTimeMs;
    }

    get table(): QueryTable | undefined {
        return this._table;
    }

    set recordsAffected(value: number | undefined) {
        this._recordsAffected = value;
    }

    set elapsedTimeMs(value: number | undefined) {
        this._elapsedTimeMs = value;
    }

    set table(value: QueryTable | undefined) {
        this._table = value;
    }
}