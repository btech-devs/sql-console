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

/**
 * Represents the ViewModel for the table schema, which includes the table information and its schema.
 */
type TableSchemaViewModel = Table & { schema: string };

/**
 * Represents the DsvImporterComponent class.
 * This component is responsible for importing DSV (Delimiter-Separated Values) files into a database.
 */
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

    /**
     * Gets or sets the table search query.
     */
    get tableSearch(): string {
        return this._tableSearch;
    }

    /**
     * Gets the flag indicating whether a file is being dragged over the component.
     */
    get isDragover(): boolean {
        return this._isDragover;
    }

    /**
     * Gets the form group used for the DSV import configuration.
     */
    get form(): FormGroup {
        return this._form;
    }

    /**
     * Gets the flag indicating whether the progress bar should be shown.
     */
    get showProgress(): boolean {
        return this._showProgress;
    }

    /**
     * Gets the flag indicating whether the DSV import process has been initiated.
     */
    get isSent(): boolean {
        return this._isSent;
    }

    /**
     * Gets the error message occurred during the DSV import process, if any.
     */
    get error(): string | undefined {
        return this._error;
    }

    /**
     * Gets or sets the file to be imported.
     */
    get file(): File | undefined {
        return this._file;
    }

    /**
     * Gets the number of bytes loaded during the import process.
     */
    get loaded(): number | undefined {
        return this._loaded;
    }

    /**
     * Gets the total number of bytes to be loaded during the import process.
     */
    get total(): number | undefined {
        return this._total;
    }

    /**
     * Gets the response object received after completing the DSV import process.
     */
    get response(): Response<Query> | undefined {
        return this._response;
    }

    /**
     * Gets the flag indicating whether a DSV import process is currently in progress.
     */
    get isImporting(): boolean {
        return this._isImporting;
    }

    /**
     * Gets the list of supported separators for the DSV file.
     */
    get separators() {
        return separators;
    }

    /**
     * Gets or sets the list of tables available in the database.
     * @param value The list of table schema view models.
     */
    get tableList(): TableSchemaViewModel[] | undefined {
        return this._tableList;
    }

    /**
     * Sets the database name for the DSV import process.
     * @param value The name of the database.
     */
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

    /**
     * ViewChild decorator to get a reference to the modal element in the component's template.
     */
    @ViewChild('modal') modal!: ElementRef;

    /**
     * Creates a validator function for checking if a number falls within a specified range.
     * @param min The minimum value of the range.
     * @param max The maximum value of the range.
     * @returns A validator function for the number range.
     */
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

    /**
     * Converts the value of a control to a valid number by removing non-numeric characters.
     * @param control The form control to convert.
     */
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

    /**
     * Opens the modal dialog for the DSV importer.
     */
    openModal() {
        this._showProgress = false;
        $(this.modal.nativeElement).modal('show');
    }

    /**
     * Shows the DSV importer modal dialog.
     * If an import process is not in progress, resets the form and other properties.
     */
    show() {
        if (!this.isImporting) {
            this.reset();
        }

        this.openModal();
    }

    /**
     * Event listener for the 'dragover' event on the window.
     * @param event The drag event.
     */
    @HostListener('window:dragover', ['$event'])
    onDragover(event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        this._isDragover = true;
    }

    /**
     * Event listener for the 'drop' event on the window.
     * @param event The drop event.
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
     * Resets the form and other properties of the DSV importer component.
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
            this._tableSearch = '';
            this._form.controls.table.reset();
        }
    }

    /**
     * Initiates the DSV import process.
     * Sends the DSV file and import configuration to the server.
     */
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

    /**
     * Event handler for the search input in the DSV importer.
     * Updates the table search value based on the input value.
     *
     * @param event - The input event object.
     */
    onSearchInput(event: any): void {
        this._tableSearch = event.target.value;
    }

    /**
     * Selects a table in the form control.
     *
     * @param table - The table to be selected.
     */
    onSelectTable(table: TableSchemaViewModel) {
        this.form.controls.table.setValue(table);
    }

    /**
     * Applies a filter to the table list based on the table search value.
     *
     * @returns An array of TableSchemaViewModel objects that match the filter criteria.
     */
    applyFilter(): TableSchemaViewModel[] {
        return this._tableList
            ?.filter(table =>
                `${table.schema}.${table.name}`.toLowerCase().includes(this.tableSearch.toLowerCase())) ?? [];
    }
}