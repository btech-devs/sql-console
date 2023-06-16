
/**
 * The SessionStorageService class provides static methods for interacting with the session storage.
 * It encapsulates common operations such as getting, setting, deleting, and clearing values in the session storage.
 * The methods allow you to store values associated with specific keys and retrieve them later.
 */
export class SessionStorageService {
    private static _storage?: Storage;

    /**
     * Returns the session storage object.
     * If it's not already set, initializes it with the session storage.
     */
    private static get storage(): Storage {
        if (this._storage == undefined)
            this._storage = sessionStorage;

        return this._storage;
    }

    /**
     * Retrieves the value associated with the specified key from the session storage.
     * @param key The key to retrieve the value for.
     * @returns The value associated with the key, or null if the key is not found.
     */
    static get(key: string): string | null {
        return this.storage.getItem(key);
    }

    /**
     * Sets the value associated with the specified key in the session storage.
     * @param key The key to set the value for.
     * @param value The value to be stored.
     */
    static set(key: string, value: any): void {
        this.storage.setItem(key, typeof value === 'string' || value instanceof String ? value : value.toString());
    }

    /**
     * Removes the value associated with the specified key from the session storage.
     * @param key The key to remove the value for.
     */
    static delete(key: string): void {
        this.storage.removeItem(key);
    }

    /**
     * Clears all values from the session storage.
     */
    static clear(): void {
        this.storage.clear();
    }
}