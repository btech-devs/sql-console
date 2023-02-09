import {Component, ElementRef, OnInit, ViewChild} from '@angular/core';

declare var $: any;

@Component({
    selector: 'app-confirm-modal',
    templateUrl: './confirmModal.component.html',
    styleUrls: ['./confirmModal.component.less']
})
export class ConfirmModalComponent implements OnInit {

    constructor() {
    }

    @ViewChild('confirmModal') modal?: ElementRef;

    private _question: string = 'Are you sure?';
    private _confirmButtonText: string = 'Confirm';
    private _declineButtonText: string = 'Decline';
    private _confirmCallback?: Function;
    private _declineCallback?: Function;

    get question(): string {
        return this._question;
    }

    get confirmButtonText(): string {
        return this._confirmButtonText;
    }

    get declineButtonText(): string {
        return this._declineButtonText;
    }

    ngOnInit(): void {
    }

    confirm(
        confirmCallback?: Function,
        declineCallback?: Function,
        question?: string,
        confirmButtonText?: string,
        declineButtonText?: string): void {
        this._confirmCallback = confirmCallback;
        this._declineCallback = declineCallback;

        if (question)
            this._question = question;

        if (confirmButtonText)
            this._confirmButtonText = confirmButtonText;

        if (declineButtonText)
            this._declineButtonText = declineButtonText;

        $(this.modal?.nativeElement).modal('toggle');
    }

    confirmAction(): void {
        if (this._confirmCallback) {
            this._confirmCallback();
        }

        $(this.modal?.nativeElement).modal('toggle');
    }

    declineAction(): void {
        if (this._declineCallback) {
            this._declineCallback();
        }

        $(this.modal?.nativeElement).modal('toggle');
    }

}