import {AfterViewInit, Component, ElementRef, ViewChild} from '@angular/core';
import {Subject} from 'rxjs';

declare var $: any;

/**
 * Component for displaying a confirmation modal dialog.
 */
@Component({
    selector: 'app-confirm-modal',
    templateUrl: './confirm-modal.component.html',
    styleUrls: ['./confirm-modal.component.less']
})
export class ConfirmModalComponent implements AfterViewInit {
    constructor() {
    }

    @ViewChild('confirmModal') modal?: ElementRef;

    private _question: string = 'Are you sure?';
    private _confirmButtonText: string = 'Confirm';
    private _declineButtonText: string = 'Decline';
    private _subject: Subject<boolean> = new Subject<boolean>();

    /**
     * Gets the subject that emits a boolean value indicating whether the user confirmed or declined the dialog.
     */
    get subject(): Subject<boolean> {
        return this._subject;
    }

    /**
     * Gets the question or message displayed in the confirmation dialog.
     */
    get question(): string {
        return this._question;
    }

    /**
     * Gets the text for the confirm button.
     */
    get confirmButtonText(): string {
        return this._confirmButtonText;
    }

    /**
     * Gets the text for the decline button.
     */
    get declineButtonText(): string {
        return this._declineButtonText;
    }

    ngAfterViewInit() {
        $(this.modal?.nativeElement).modal('show');

        $(this.modal?.nativeElement).on('hidden.bs.modal', () => {
            this._subject.complete();
        });
    }

    /**
     * Sets up the confirmation dialog with custom question and button texts.
     * @param question The question or message to display in the dialog.
     * @param confirmButtonText The text for the confirm button.
     * @param declineButtonText The text for the decline button.
     */
    setup(
        question?: string,
        confirmButtonText?: string,
        declineButtonText?: string): void {

        if (question)
            this._question = question;

        if (confirmButtonText)
            this._confirmButtonText = confirmButtonText;

        if (declineButtonText)
            this._declineButtonText = declineButtonText;
    }

    /**
     * Handles the confirm action and emits a `true` value through the subject.
     * Hides the confirmation dialog.
     */
    confirmAction(): void {
        this._subject.next(true);
        $(this.modal?.nativeElement).modal('hide');
    }

    /**
     * Handles the decline action and emits a `false` value through the subject.
     * Hides the confirmation dialog.
     */
    declineAction(): void {
        this._subject.next(false);
        $(this.modal?.nativeElement).modal('hide');
    }

}