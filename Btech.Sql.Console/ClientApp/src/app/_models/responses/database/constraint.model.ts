export class Constraint {
    private _name?: string;
    private _type?: string;
    private _sourceTable?: string;
    private _sourceColumn?: string;
    private _targetTable?: string;
    private _targetColumn?: string;

    get name(): string | undefined {
        return this._name;
    }

    set name(value: string | undefined) {
        this._name = value;
    }
    get type(): string | undefined {
        return this._type;
    }

    set type(value: string | undefined) {
        this._type = value;
    }
    get sourceTable(): string | undefined {
        return this._sourceTable;
    }

    set sourceTable(value: string | undefined) {
        this._sourceTable = value;
    }
    get sourceColumn(): string | undefined {
        return this._sourceColumn;
    }

    set sourceColumn(value: string | undefined) {
        this._sourceColumn = value;
    }
    get targetTable(): string | undefined {
        return this._targetTable;
    }

    set targetTable(value: string | undefined) {
        this._targetTable = value;
    }
    get targetColumn(): string | undefined {
        return this._targetColumn;
    }

    set targetColumn(value: string | undefined) {
        this._targetColumn = value;
    }
}