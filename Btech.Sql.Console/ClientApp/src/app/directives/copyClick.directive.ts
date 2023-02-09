import {Directive, HostBinding, HostListener, Input} from '@angular/core';
import {AlertStorage} from '../utils';

@Directive({selector: '[copied]'})
export class CopyClick {
    @HostBinding('class')
    elementClass = 'cursor-pointer';

    @HostBinding('title')
    elementTitle = 'Click twice to copy';

    @Input()
    copiedMessage?: string = undefined;

    @HostListener('dblclick', ['$event'])
    onClick(event: MouseEvent) {
        // @ts-ignore
        let text: string = event.target?.innerText;

        if (text)
            navigator.clipboard
                .writeText(text)
                .then(
                    () => {
                        // @ts-ignore
                        if (window.getSelection) window.getSelection().removeAllRanges();
                        // @ts-ignore
                        else if (document.selection) document.selection.empty();
    
                        AlertStorage.info = this.copiedMessage == undefined || this.copiedMessage == ''
                            ? 'The text has been copied.'
                            : this.copiedMessage;
                    });
    }
}