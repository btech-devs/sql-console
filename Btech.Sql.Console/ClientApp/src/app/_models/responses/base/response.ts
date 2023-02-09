export class ResponseBase {
    private _errorMessage?: string;
    private _errorMessages?: string[];
    private _validationErrorMessages?: { key: string, value: string }[];

    get errorMessage(): string | undefined {
        return this._errorMessage;
    }

    set errorMessage(value: string | undefined) {
        this._errorMessage = value;
    }

    get errorMessages(): string[] | undefined {
        return this._errorMessages;
    }

    set errorMessages(value: string[] | undefined) {
        this._errorMessages = value;
    }

    get validationErrorMessages(): { key: string, value: string }[] | undefined {
        return this._validationErrorMessages;
    }

    set validationErrorMessages(value: { key: string, value: string }[] | undefined) {
        this._validationErrorMessages = value;
    }
}

export class Response<T> extends ResponseBase {
    private _data?: T;

    get data(): T | undefined {
        return this._data;
    }

    set data(value: T | undefined) {
        this._data = value;
    }
}