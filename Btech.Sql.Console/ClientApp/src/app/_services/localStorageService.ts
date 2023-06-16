/**
 * Service for accessing and manipulating data in the local storage.
 * It provides methods to get, set, delete, and clear items in the local storage.
 */
export class LocalStorageService {
    private static _storage?: Storage;

    /**
     * Gets the underlying storage object (localStorage).
     * If not initialized, it defaults to localStorage.
     */
    private static get storage(): Storage {
        if (this._storage == undefined)
            this._storage = localStorage;

        return this._storage;
    }

    /**
     * Retrieves a value from the local storage by the specified key.
     * @param key The key of the item to retrieve.
     * @returns The value associated with the key, or null if the key is not found.
     */
    static get(key: string): string | null {
        return this.storage.getItem(key);
    }

    /**
     * Sets a value in the local storage with the specified key.
     * @param key The key under which to store the value.
     * @param value The value to store.
     */
    static set(key: string, value: any): void {
        this.storage.setItem(key, typeof value === 'string' || value instanceof String ? value : value.toString());
    }

    /**
     * Deletes an item from the local storage by the specified key.
     * @param key The key of the item to delete.
     */
    static delete(key: string): void {
        this.storage.removeItem(key);
    }

    /**
     * Clears all items from the local storage.
     */
    static clear(): void {
        this.storage.clear();
    }
}