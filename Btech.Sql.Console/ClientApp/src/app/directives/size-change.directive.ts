import {AfterViewInit, Directive, ElementRef, EventEmitter, OnDestroy, OnInit, Output} from '@angular/core';

@Directive({
    selector: '[onSizeChange]'
})
export class SizeChangeDirective implements OnDestroy, AfterViewInit {
    private changes!: ResizeObserver;

    @Output()
    public onSizeChange = new EventEmitter();

    constructor(private elementRef: ElementRef) {
    }

    ngOnDestroy(): void {
        this.changes.disconnect();
    }

    ngAfterViewInit(): void {
        const element = this.elementRef.nativeElement;

        this.changes = new ResizeObserver(() => {
            this.onSizeChange.emit();
        });

        this.changes.observe(element);
    }
}