export class SessionStorageService {
    private static _storage?: Storage;

    private static get storage(): Storage {
        if (this._storage == undefined)
            this._storage = sessionStorage;

        return this._storage;
    }

    static get(key: string): string | null {
        return this.storage.getItem(key);
    }

    static set(key: string, value: any): void {
        this.storage.setItem(key, typeof value === 'string' || value instanceof String ? value : value.toString());
    }

    static delete(key: string): void {
        this.storage.removeItem(key);
    }

    static clear(): void {
        this.storage.clear();
    }
}