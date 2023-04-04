import {AfterViewInit, Component, ElementRef, ViewChild} from '@angular/core';
import {Subject} from 'rxjs';

declare var $: any;

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

    get subject(): Subject<boolean> {
        return this._subject;
    }

    get question(): string {
        return this._question;
    }

    get confirmButtonText(): string {
        return this._confirmButtonText;
    }

    get declineButtonText(): string {
        return this._declineButtonText;
    }

    ngAfterViewInit() {
        $(this.modal?.nativeElement).modal('show');

        $(this.modal?.nativeElement).on('hidden.bs.modal', () => {
            this._subject.complete();
        });
    }

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

    confirmAction(): void {
        this._subject.next(true);
        $(this.modal?.nativeElement).modal('hide');
    }

    declineAction(): void {
        this._subject.next(false);
        $(this.modal?.nativeElement).modal('hide');
    }

}