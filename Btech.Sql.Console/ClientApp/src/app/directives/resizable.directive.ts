import {
    Directive,
    ElementRef, EventEmitter,
    HostListener,
    Input,
    OnChanges,
    OnInit, Output,
    Renderer2,
    SimpleChanges
} from '@angular/core';

@Directive({
    selector: '.row[resizable]',
})
export class ResizableRow implements OnInit, OnChanges {

    // region Private Fields

    private _minScreenWidthToShow = 991;
    private _resizableElement?: HTMLDivElement;
    private _currentHeight?: number;
    private _mouseDown: boolean = false;
    private _startY?: number;
    private _isActive: boolean = document.body.offsetWidth > this._minScreenWidthToShow;

    // endregion Private Fields

    /**
     * @description
     * Max height of resizable '.row' in pixels.
     */
    @Input() maxHeight?: number;

    /**
     * @description
     * Min height of resizable '.row' in pixels.
     */
    @Input() minHeight?: number;

    constructor(
        private _elementRef: ElementRef,
        private _renderer: Renderer2) {
    }

    @HostListener('window:resize', ['$event'])
    onResize() {

        if (document.body.offsetWidth <= this._minScreenWidthToShow && this._isActive) {
            this._isActive = false;

            this._resizableElement?.remove();
            this._elementRef.nativeElement.classList.remove('resizable__row');
        }

        if (document.body.offsetWidth > this._minScreenWidthToShow && !this._isActive) {
            this._isActive = true;
            this.ngOnInit();
        }

    }

    ngOnInit() {
        if (this._isActive) {
            this._elementRef.nativeElement.classList.add('resizable', 'resizable__row');

            this._resizableElement = document.createElement('div');
            this._resizableElement.classList.add('resizable__row__element');

            let child = document.createElement('div');
            child.classList.add('resizable__row__element-content');
            child.innerHTML = '<i class="bi bi-three-dots"></i>';
            this._resizableElement.appendChild(child);

            this._elementRef.nativeElement.appendChild(this._resizableElement);

            this._elementRef.nativeElement.style.height = this.minHeight + 'px';

            child.onpointerdown = (e) => {
                this._mouseDown = true;
                this._currentHeight = this._elementRef.nativeElement.offsetHeight;
                this._startY = e.clientY;
            };

            this._renderer.listen('document', 'pointerup', () => {
                this._mouseDown = false;
            });

            this._renderer.listen('document', 'pointermove', mouseEvent => {
                if (this._mouseDown) {
                    let height = Math
                        .max((this._currentHeight + mouseEvent.clientY - this._startY!), this.minHeight!);

                    if (this.maxHeight && this.maxHeight < height)
                        height = this.maxHeight;

                    this._elementRef.nativeElement.style.height = height + 'px';
                }
            });
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        this._elementRef.nativeElement.style.height = this.minHeight + 'px';
    }

}

@Directive({
    selector: '[resizableCol]',
})
export class ResizableCol implements OnInit, OnChanges {

    // region Private Fields
    private _minScreenWidthToShow = 991;
    private _resizableElement?: HTMLDivElement;
    private _currentWidth?: number;
    private _mouseDown: boolean = false;
    private _startX?: number;
    private _containerWidth?: number;
    private _isActive: boolean = document.body.offsetWidth > this._minScreenWidthToShow;

    // endregion Private Fields

    /**
     * @description
     * Max width of resizable '.col' in percents.
     */
    @Input() maxWidth?: number;

    /**
     * @description
     * Min width of resizable '.col' in percents.
     */
    @Input() minWidth?: number;

    @Output() onResize: EventEmitter<number> = new EventEmitter<number>();

    constructor(
        private _elementRef: ElementRef,
        private _renderer: Renderer2) {
    }

    @HostListener('window:resize', ['$event'])
    onResizeWindow() {
        if (document.body.offsetWidth <= this._minScreenWidthToShow && this._isActive) {
            this._isActive = false;

            this._resizableElement?.remove();
            this._elementRef.nativeElement.style.width = '';
            this._elementRef.nativeElement.classList.remove('resizable', 'resizable__col');
            this.onResize.emit(0);
        }

        if (document.body.offsetWidth > this._minScreenWidthToShow && !this._isActive) {
            this._isActive = true;
            this.ngOnInit();
        }
    }

    ngOnInit() {
        if (this._isActive) {
            this._elementRef.nativeElement.classList.add('resizable', 'resizable__col');

            this._containerWidth = this._elementRef.nativeElement.parentElement.offsetWidth;
            this._resizableElement = document.createElement('div');
            this._resizableElement.classList.add('resizable__col__element');

            let child = document.createElement('div');
            child.classList.add('resizable__col__element-content');
            child.innerHTML = '<i class="bi bi-three-dots-vertical"></i>';
            this._resizableElement.appendChild(child);

            this._elementRef.nativeElement.appendChild(this._resizableElement);

            this._elementRef.nativeElement.style.width = this.minWidth + '%';
            this.onResize.emit(this.minWidth);

            child.onpointerdown = (e) => {
                this._mouseDown = true;
                this._currentWidth = parseInt(this._elementRef.nativeElement.style.width);
                this._startX = e.clientX;
            };

            this._renderer.listen('document', 'pointerup', () => {
                this._mouseDown = false;
            });

            this._renderer.listen('document', 'pointermove', mouseEvent => {
                if (this._mouseDown) {
                    let width = Math
                        .max(this._currentWidth! + ((this._startX! - mouseEvent.clientX) / this._containerWidth! * 100), this.minWidth!);

                    if (this.maxWidth && this.maxWidth < width)
                        width = this.maxWidth;

                    this._elementRef.nativeElement.style.width = width + '%';
                    this.onResize.emit(width);
                }
            });
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        if (document.body.offsetWidth > this._minScreenWidthToShow) {
            this._elementRef.nativeElement.style.width = this.minWidth + '%';
        }
    }

}