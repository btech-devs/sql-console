export class QueryColumn{
    private _name?: string;
    private _type?: string;

    get name(): string | undefined {
        return this._name;
    }

    get type(): string | undefined {
        return this._type;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    set type(value: string | undefined) {
        this._type = value;
    }
}