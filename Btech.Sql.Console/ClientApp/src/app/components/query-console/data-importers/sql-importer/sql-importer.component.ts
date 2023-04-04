import {AfterViewInit, Component, ElementRef, HostListener, Input, ViewChild} from '@angular/core';
import {QueryService} from '../../../../_services/query.service';
import {concatMap, Observable, of, Subscriber} from 'rxjs';
import {HttpEventType} from '@angular/common/http';
import {Response} from '../../../../_models/responses/base/response';
import {Query} from '../../../../_models/responses/query/query.model';
import {AlertStorage, getTransitionAnimation} from '../../../../utils';

declare var $: any;

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

    get isDragover(): boolean {
        return this._isDragover;
    }

    get showProgress(): boolean {
        return this._showProgress;
    }

    get isSent(): boolean {
        return this._isSent;
    }

    get error(): string | undefined {
        return this._error;
    }

    get file(): File | undefined {
        return this._file;
    }

    get loaded(): number | undefined {
        return this._loaded;
    }

    get total(): number | undefined {
        return this._total;
    }

    get response(): Response<Query> | undefined {
        return this._response;
    }

    get isImporting(): boolean {
        return this._isImporting;
    }

    @Input('database') set database(value: string | undefined) {
        this._database = value;
    }

    set showProgress(value: boolean) {
        this._showProgress = value;
    }

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

    openModal() {
        this._showProgress = false;
        $(this.modal.nativeElement).modal('show');
    }

    show() {
        if (!this.isImporting) {
            this.reset();
        }

        this.openModal();
    }

    @HostListener('window:dragover', ['$event'])
    onDragover(event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        this._isDragover = true;
    }

    @HostListener('window:drop', ['$event'])
    onDrop(event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();

        if (event.dataTransfer?.files?.length) {
            this._isDragover = false;
            this.file = event.dataTransfer.files[0];
        }
    }

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