import {BaseComponent} from '../../_base/base.component';
import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {PATH_GOOGLE_AUTHORIZATION} from '../../utils';

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

    get message(): string | undefined {
        return this._message;
    }

    set message(value: string | undefined) {
        this._message = value;
    }

    get tryAgainButton(): boolean | undefined {
        return this._tryAgainButton;
    }

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

    public onTryAgain() {
        this._router.navigate([PATH_GOOGLE_AUTHORIZATION]);
    }
}