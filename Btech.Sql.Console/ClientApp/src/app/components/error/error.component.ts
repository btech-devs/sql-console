import {BaseComponent} from '../../_base/base.component';
import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {PATH_GOOGLE_AUTHORIZATION} from '../../utils';

/**
 * Component for displaying error messages and handling error actions.
 */
@Component({
    selector: 'app-error',
    templateUrl: './error.component.html'
})
export class ErrorComponent extends BaseComponent implements OnInit {
    private readonly _router: Router;
    private readonly _activatedRoute: ActivatedRoute;
    private _message: string | undefined;
    private _tryAgainButton: boolean | undefined;

    constructor(router: Router, activatedRoute: ActivatedRoute) {
        super();

        this._router = router;
        this._activatedRoute = activatedRoute;
    }

    /**
     * Get the error message.
     */
    get message(): string | undefined {
        return this._message;
    }

    /**
     * Set the error message.
     * @param value - The error message.
     */
    set message(value: string | undefined) {
        this._message = value;
    }

    /**
     * Get the status of the "Try Again" button.
     */
    get tryAgainButton(): boolean | undefined {
        return this._tryAgainButton;
    }

    /**
     * Set the status of the "Try Again" button.
     * @param value - The status of the button.
     */
    set tryAgainButton(value: boolean | undefined) {
        this._tryAgainButton = value;
    }

    ngOnInit() {
        super.ngOnInit();

        this._activatedRoute
            .queryParams
            .subscribe({
                next: params => {
                    let message: string | undefined = params['message'];
                    let tryAgainButton: boolean = params['tryAgainButton'] ?? false;

                    if (message) {
                        this.message = message;
                        this.tryAgainButton= tryAgainButton;
                        this._router
                            .navigate(
                                [],
                                {
                                    relativeTo: this._activatedRoute,
                                    queryParams: {}
                                });
                    }
                }
            });
    }

    /**
     * Callback function when the "Try Again" button is clicked.
     * Navigates to the Google Authorization path.
     */
    public onTryAgain() {
        this._router.navigate([PATH_GOOGLE_AUTHORIZATION]);
    }
}