import {ApplicationRef, ComponentRef, Injectable, ViewContainerRef} from '@angular/core';
import {ConfirmModalComponent} from './confirm-modal.component';
import {AppComponent} from '../../app.component';
import {finalize, Observable} from 'rxjs';

/**
 * Service for displaying a confirmation modal dialog.
 */
@Injectable({
    providedIn: 'root'
})
export class ConfirmModalService {
    constructor(private _appRef: ApplicationRef) {
    }

    private get _viewContainerRef(): ViewContainerRef {
        return (this._appRef.components[0]?.instance as AppComponent).viewContainerRef;
    }

    /**
     * Displays a confirmation modal dialog.
     * @param question The question or message to display in the dialog.
     * @param confirmButtonText The text for the confirm button.
     * @param declineButtonText The text for the decline button.
     * @returns An Observable that emits a boolean value indicating whether the user confirmed or declined the dialog.
     */
    confirm(question?: string,
            confirmButtonText?: string,
            declineButtonText?: string): Observable<boolean> {
        let componentRef: ComponentRef<ConfirmModalComponent> = this._viewContainerRef
            .createComponent<ConfirmModalComponent>(ConfirmModalComponent);

        componentRef.instance.setup(question, confirmButtonText, declineButtonText);

        return componentRef.instance.subject
            .pipe(finalize(() => this._appRef.detachView(componentRef.hostView)));
    }
}