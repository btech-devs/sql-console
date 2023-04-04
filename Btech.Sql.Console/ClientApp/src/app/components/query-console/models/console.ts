export class Console {
    private _consoleRowList: Array<ConsoleRow> = new Array<ConsoleRow>();

    public pushMessage(message: string, severity: string = '') {
        let consoleRow: ConsoleRow = new ConsoleRow(
            new Date(Date.now()),
            message,
            severity);

        this._consoleRowList.push(consoleRow);
    }

    public pushDanger(message: string) {
        this.pushMessage(message, 'danger');
    }

    public pushWarning(message: string) {
        this.pushMessage(message, 'warning');
    }

    public pushSuccess(message: string) {
        this.pushMessage(message, 'success');
    }

    public pushSeparator() {
        this.pushMessage('', 'separator');
    }

    public get consoleRowList(): Array<ConsoleRow> {
        return this._consoleRowList;
    }
}

class ConsoleRow {
    private readonly _messageTime?: Date;
    private readonly _message?: string;
    private readonly _severity: string = '';

    get messageTime(): Date | undefined {
        return this._messageTime;
    }

    get message(): string | undefined {
        return this._message;
    }

    get severity(): string {
        return this._severity;
    }

    constructor(messageTime?: Date, message?: string, severity: string = '') {
        this._messageTime = messageTime;
        this._message = message;
        this._severity = severity;
    }
}