export class SavedQuery {
    private _id?: number;
    private _name?: string;
    private _query?: string;

    get id(): number | undefined {
        return this._id;
    }

    set id(value: number | undefined) {
        this._id = value;
    }

    get name(): string | undefined {
        return this._name;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    get query(): string | undefined {
        return this._query;
    }

    set query(value: string | undefined) {
        this._query = value;
    }
}