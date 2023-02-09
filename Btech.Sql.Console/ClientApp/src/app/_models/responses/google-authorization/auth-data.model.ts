export class AuthData {
    private _idToken?: string;
    private _pictureUrl?: string[];

    get idToken(): string | undefined { return this._idToken; }
    set idToken(value: string | undefined) { this._idToken = value; }

    get pictureUrl(): string[] | undefined { return this._pictureUrl; }
    set pictureUrl(value: string[] | undefined) { this._pictureUrl = value; }
}