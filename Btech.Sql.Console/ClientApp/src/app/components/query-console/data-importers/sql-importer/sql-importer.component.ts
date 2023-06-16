import {AfterViewInit, Component, ElementRef, HostListener, Input, ViewChild} from '@angular/core';
import {QueryService} from '../../../../_services/query.service';
import {concatMap, Observable, of, Subscriber} from 'rxjs';
import {HttpEventType} from '@angular/common/http';
import {Response} from '../../../../_models/responses/base/response';
import {Query} from '../../../../_models/responses/query/query.model';
import {AlertStorage, getTransitionAnimation} from '../../../../utils';

declare var $: any;

/**
 * Component responsible for handling SQL file imports and displaying progress and response.
 * @implements {AfterViewInit}
 */
@Component({
    selector: 'app-sql-importer',
    animations: [getTransitionAnimation()],
    templateUrl: './sql-importer.component.html',
    styleUrls: ['./sql-importer.component.less']
})
export class SqlImporterComponent implements AfterViewInit {
    constructor(private queryService: QueryService) {
    }

    private _isImporting: boolean = false;
    private _isSent: boolean = false;
    private _file?: File;
    private _isDragover: boolean = false;
    private _database?: string;
    private _total?: number;
    private _loaded?: number;
    private _response?: Response<Query>;
    private _error?: string;
    private _showProgress: boolean = false;

    /**
     * Gets the value indicating whether a file is being dragged over the component.
     * @returns {boolean} The value indicating whether a file is being dragged over the component.
     */
    get isDragover(): boolean {
        return this._isDragover;
    }

    /**
     * Gets the value indicating whether to show the progress bar.
     * @returns {boolean} The value indicating whether to show the progress bar.
     */
    get showProgress(): boolean {
        return this._showProgress;
    }

    /**
     * Gets the value indicating whether the import request has been sent.
     * @returns {boolean} The value indicating whether the import request has been sent.
     */
    get isSent(): boolean {
        return this._isSent;
    }

    /**
     * Gets the error message in case of an import error.
     * @returns {string | undefined} The error message in case of an import error.
     */
    get error(): string | undefined {
        return this._error;
    }

    /**
     * Gets the selected file for import.
     * @returns {File | undefined} The selected file for import.
     */
    get file(): File | undefined {
        return this._file;
    }

    /**
     * Gets the number of bytes loaded during the import process.
     * @returns {number | undefined} The number of bytes loaded during the import process.
     */
    get loaded(): number | undefined {
        return this._loaded;
    }

    /**
     * Gets the total size of the file being imported.
     * @returns {number | undefined} The total size of the file being imported.
     */
    get total(): number | undefined {
        return this._total;
    }

    /**
     * Gets the response received from the import request.
     * @returns {Response<Query> | undefined} The response received from the import request.
     */
    get response(): Response<Query> | undefined {
        return this._response;
    }

    /**
     * Gets the value indicating whether an import operation is in progress.
     * @returns {boolean} The value indicating whether an import operation is in progress.
     */
    get isImporting(): boolean {
        return this._isImporting;
    }

    /**
     * Sets the database to import the SQL file into.
     * @param {string | undefined} value - The database to import the SQL file into.
     */
    @Input('database') set database(value: string | undefined) {
        this._database = value;
    }

    /**
     * Sets the selected file for import.
     * @param {File | undefined} value - The selected file for import.
     */
    set file(value: File | undefined) {
        const allowedFileExtensions: string[] = ['sql', 'pgsql', 'psql'];

        if (!value)
            this._file = value;
        else {
            let nameParts: string[] = value.name.split('.');
            let extension: string = nameParts[nameParts.length - 1];

            if (allowedFileExtensions.indexOf(extension) > -1)
                this._file = value;
        }
    }

    /**
     * The modal element reference.
     * @type {ElementRef}
     */
    @ViewChild('modal') modal!: ElementRef;

    ngAfterViewInit(): void {
        $(this.modal.nativeElement).on('hidden.bs.modal', () => {
            if (this.isImporting) {
                this._showProgress = true;
            } else {
                this.reset();
            }
        });
    }

    /**
     * Opens the modal dialog.
     */
    openModal() {
        this._showProgress = false;
        $(this.modal.nativeElement).modal('show');
    }

    /**
     * Shows the modal dialog.
     */
    show() {
        if (!this.isImporting) {
            this.reset();
        }

        this.openModal();
    }

    /**
     * Event listener for the dragover event on the window.
     * @param {DragEvent} event - The drag event.
     */
    @HostListener('window:dragover', ['$event'])
    onDragover(event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        this._isDragover = true;
    }

    /**
     * Event listener for the drop event on the window.
     * @param {DragEvent} event - The drop event.
     */
    @HostListener('window:drop', ['$event'])
    onDrop(event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();

        if (event.dataTransfer?.files?.length) {
            this._isDragover = false;
            this.file = event.dataTransfer.files[0];
        }
    }

    /**
     * Resets the component state.
     */
    reset() {
        if (!this._isImporting) {
            this._file = undefined;
            this._total = undefined;
            this._loaded = undefined;
            this._response = undefined;
            this._isDragover = false;
            this._isSent = false;
            this._error = undefined;
            this._showProgress = false;
        }
    }

    /**
     * Imports the selected SQL file.
     */
    import() {
        this._error = undefined;
        this._isSent = false;
        this._response = undefined;
        this._isImporting = true;

        if (this._file && this._database)
            of(this._file)
                .pipe(
                    concatMap((file: File) => {
                        return new Observable<ArrayBuffer>(
                            (subscriber: Subscriber<ArrayBuffer>) => {
                                let reader: FileReader = new FileReader();

                                reader.onloadend = (event: ProgressEvent<FileReader>) => {
                                    subscriber.next(reader.result as ArrayBuffer);
                                    subscriber.complete();
                                };
                                reader.readAsArrayBuffer(file);
                            });
                    }),
                    concatMap((value: ArrayBuffer) => {

                        let blob: Blob = new Blob([value]);

                        let formData: FormData = new FormData();
                        formData.append('file', blob, this.file?.name);

                        return this.queryService.importSql(this._database!, formData);
                    }))
                .subscribe({
                    next: (event) => {
                        if (event.type == HttpEventType.UploadProgress) {
                            this._total = event.total;
                            this._loaded = event.loaded;
                            this._isSent = event.total == event.loaded;
                        }

                        if (event.type == HttpEventType.Response) {
                            this._isImporting = false;
                            this._response = event.body ?? undefined;
                        }
                    },
                    error: (error: Error) => {
                        this._isImporting = false;
                        this._error = error.message;
                        AlertStorage.error = error.message;
                    }
                });
    }
}