import {AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {QueryService} from '../../../../_services/query.service';
import {concatMap, Observable, of, Subscriber} from 'rxjs';
import {HttpEventType} from '@angular/common/http';
import {Response} from '../../../../_models/responses/base/response';
import {Query} from '../../../../_models/responses/query/query.model';
import {AlertStorage, getTransitionAnimation, separators} from '../../../../utils';
import {Table} from '../../../../_models/responses/database/table.model';
import {AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators} from '@angular/forms';

declare var $: any;

type TableSchemaViewModel = Table & { schema: string };
@Component({
    selector: 'app-dsv-importer',
    animations: [getTransitionAnimation()],
    templateUrl: './dsv-importer.component.html',
    styleUrls: ['./dsv-importer.component.less']
})
export class DsvImporterComponent implements OnInit, AfterViewInit {

    constructor(private queryService: QueryService) {
    }

    private _isImporting: boolean = false;
    private _isSent: boolean = false;
    private _file?: File;
    private _isDragover: boolean = false;
    private _database?: string;
    private _tableList?: TableSchemaViewModel[];
    private _total?: number;
    private _loaded?: number;
    private _response?: Response<Query>;
    private _error?: string;
    private _showProgress: boolean = false;
    private _form!: FormGroup;
    private _tableSearch: string = '';

    get tableSearch(): string {
        return this._tableSearch;
    }

    set tableSearch(value: string) {
        this._tableSearch = value;
    }

    get isDragover(): boolean {
        return this._isDragover;
    }

    get form(): FormGroup {
        return this._form;
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

    get separators() {
        return separators;
    }

    get tableList(): TableSchemaViewModel[] | undefined {
        return this._tableList;
    }

    @Input('database') set database(value: string | undefined) {
        this._database = value;
    }

    @Input('tableList') set tableList(value: TableSchemaViewModel[] | undefined) {
        this._tableList = value;
    }

    set file(value: File | undefined) {
        const allowedFileExtensions: string[] = ['csv', 'dsv', 'tsv'];

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

    private numberRangeValidator(min: number, max: number): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            let validationError: string | null = null;

            let value: string = control.value;

            if (value === null || value.length == 0)
                validationError = 'The field is required.';
            else if (+value > max || +value < min)
                validationError = `The field must be between ${min} and ${max}.`;

            return !validationError ? null : {validationError: validationError};
        };
    }

    numberOnly(control?: AbstractControl) {
        if (control) {
            let value: string = control.value?.toString();

            control.setValue(+value?.replace(/[^0-9.]/g, ''));
        }
    }

    ngOnInit() {
        this._form = new FormGroup<any>({
            table: new FormControl(null, Validators.required),
            separator: new FormControl(separators[0], Validators.required),
            chunkSize: new FormControl<number>(
                1000,
                [
                    Validators.required,
                    this.numberRangeValidator(1, 10000)
                ]
            ),
            doubleQuotes: new FormControl<boolean>(false),
            rollbackOnError: new FormControl<boolean>(false),
            rowsToSkip: new FormControl<number>(
                0,
                [
                    Validators.required,
                    this.numberRangeValidator(0, Number.MAX_VALUE)
                ]
            ),
        });
    }

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
            this._tableSearch = '';
            this._form.controls.table.reset();
        }
    }

    import() {
        this._error = undefined;
        this._isSent = false;
        this._response = undefined;
        this._isImporting = true;
        this._form.disable();

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

                        return this.queryService.importDsv(
                            this._database!,
                            this.form.value.table.name,
                            this.form.value.separator.value,
                            blob,
                            this.file?.name,
                            this.form.value.chunkSize,
                            this.form.value.doubleQuotes,
                            this.form.value.rollbackOnError,
                            this.form.value.rowsToSkip);
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
                            this._form.enable();
                            this._response = event.body ?? undefined;
                        }
                    },
                    error: (error: Error) => {
                        this._isImporting = false;
                        this._form.enable();
                        this._error = error.message;
                        AlertStorage.error = error.message;
                    }
                });
    }

    onSearchInput(event: any): void {
        this._tableSearch = event.target.value;
    }

    onSelectTable(table: TableSchemaViewModel) {
        this.form.controls.table.setValue(table);
    }

    applyFilter(): TableSchemaViewModel[] {
        return this._tableList
            ?.filter(table =>
                `${table.schema}.${table.name}`.toLowerCase().includes(this.tableSearch.toLowerCase())) ?? [];
    }
}