import {ResponseBase} from './response';

export class PaginationResponse<T> extends ResponseBase {
    private _currentPage?: number;
    private _perPage?: number;
    private _totalAmount?: number;
    private _entities?: T[];

    get currentPage(): number | undefined {
        return this._currentPage;
    }

    get perPage(): number | undefined {
        return this._perPage;
    }

    get totalAmount(): number | undefined {
        return this._totalAmount;
    }

    get entities(): T[] | undefined {
        return this._entities;
    }

    set currentPage(value: number | undefined) {
        this._currentPage = value;
    }

    set perPage(value: number | undefined) {
        this._perPage = value;
    }

    set totalAmount(value: number | undefined) {
        this._totalAmount = value;
    }

    set entities(value: T[] | undefined) {
        this._entities = value;
    }

}