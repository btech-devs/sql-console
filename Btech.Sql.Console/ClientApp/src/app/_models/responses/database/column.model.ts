export class Column {
    private _name?: string;
    private _defaultValue?: string;
    private _type?: string;
    private _maxLength?: number;
    private _isPrimaryKey?: boolean;
    private _isForeignKey?: boolean;

    get name(): string | undefined {
        return this._name;
    }

    get defaultValue(): string | undefined{
        return this._defaultValue;
    }

    get type(): string | undefined {
        return this._type;
    }

    get maxLength(): number | undefined {
        return this._maxLength;
    }

    get isPrimaryKey(): boolean | undefined {
        return this._isPrimaryKey;
    }

    get isForeignKey(): boolean | undefined {
        return this._isForeignKey;
    }

    set name(value: string | undefined) {
        this._name = value;
    }

    set defaultValue(value: string | undefined) {
        this._defaultValue = value;
    }

    set type(value: string | undefined) {
        this._type = value;
    }

    set maxLength(value: number | undefined) {
        this._maxLength = value;
    }

    set isPrimaryKey(value: boolean | undefined) {
        this._isPrimaryKey = value;
    }

    set isForeignKey(value: boolean | undefined) {
        this._isForeignKey = value;
    }
}