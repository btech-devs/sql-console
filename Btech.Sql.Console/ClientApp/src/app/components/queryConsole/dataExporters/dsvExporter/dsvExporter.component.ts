import {Component, Input, OnInit, ViewChild} from '@angular/core';
import {Table} from '../../models/table';
import {saveAs} from 'file-saver';
import {QueryService} from '../../../../_services/query.service';
import {HttpEventType} from '@angular/common/http';
import {AlertStorage} from '../../../../utils';
import {
    AbstractControl,
    FormControl,
    FormGroup,
    ValidationErrors, Validators,
} from '@angular/forms';
import {Subscription} from 'rxjs';
import {ConfirmModalComponent} from '../../../confirmModal/confirmModal.component';

declare var $: any;

@Component({
    selector: 'dsv-exporter',
    templateUrl: './dsvExporter.component.html'
})
export class DsvExporterComponent implements OnInit {

    // region Readonly Public Fields

    readonly separators: {
        name: string,
        value: string,
        extension: string,
        mimeType: string
    }[] = [
        {
            name: 'Comma (,)',
            value: ',',
            extension: 'csv',
            mimeType: 'text/csv'
        },
        {
            name: 'Tab (\t)',
            value: '\t',
            extension: 'tsv',
            mimeType: 'text/tab-separated-values'
        },
        {
            name: 'Semicolon (;)',
            value: ';',
            extension: 'ssv',
            mimeType: 'text/csv'
        }
    ];

    readonly newLines: { name: string, value: string }[] = [
        {
            name: '\\n',
            value: '\n'
        },
        {
            name: '\\r',
            value: '\r'
        },
        {
            name: '\\r\\n',
            value: '\r\n'
        }
    ];

    readonly nullOutputs: { name: string, value: string }[] = [
        {
            name: 'Empty column',
            value: ''
        },
        {
            name: 'null',
            value: 'null'
        }
    ];

    // endregion Readonly Public Fields

    // region Private Fields

    private readonly _formGroup: FormGroup;
    private _isExporting: boolean = false;
    private _progress?: HttpEventType = undefined;
    private _blobSize?: number = undefined;
    private _isExecuteToDsv: boolean = false;
    private _sql?: string;
    private _database?: string;
    private _data?: Table;
    private _exporting$?: Subscription;

    // endregion Private Fields

    // region Public Getters

    get formGroup(): FormGroup {
        return this._formGroup;
    }

    get isExporting(): boolean {
        return this._isExporting;
    }

    get isExecuteToDsv(): boolean {
        return this._isExecuteToDsv;
    }

    get progress(): HttpEventType | undefined {
        return this._progress;
    }

    get blobSize(): number | undefined {
        return this._blobSize;
    }

    // endregion Public Getters

    @Input() modalId?: string;
    @ViewChild('confirmModalComponent') confirmModalComponent?: ConfirmModalComponent;

    constructor(private _queryService: QueryService) {
        this._formGroup = new FormGroup({
            filename: new FormControl<string>('table',
                [
                    Validators.required,
                    (control: AbstractControl): ValidationErrors | null => {
                        let validationError: string | null = null;

                        if (control.value.search('[\\\\\\/?%*:|"<>\\.]') > -1) {
                            validationError = `Please provide a valid filename. Not allowed characters: /\\.?%*:|"<>`;
                        }

                        return validationError == null ? null : {validationError: validationError};
                    }
                ]),
            includeHeader: new FormControl<boolean>(false),
            addQuotes: new FormControl<boolean>(false),
            separator: new FormControl<{
                name: string,
                value: string,
                extension: string,
                mimeType: string
            }>(this.separators[0]),
            newLine: new FormControl<{ name: string, value: string }>(this.newLines[0]),
            nullOutput: new FormControl<{ name: string, value: string }>(this.nullOutputs[0])
        });
    }

    // region Private Methods

    private exportToDsv(): void {
        if (!this._isExporting) {
            this._isExporting = true;

            if (this._data && this._formGroup.valid) {
                let rawDsv = '';

                if (this.formGroup.value.includeHeader) {
                    rawDsv = this._data.header.map(cell =>
                        this.formGroup.value.addQuotes
                            ? `"${cell.columnName}"`
                            : cell.columnName).join(this.formGroup.value.separator.value.value);
                    rawDsv += this.formGroup.value.newLine.value;
                }

                this._data.body.forEach(row => {
                    rawDsv += row
                        .map(cell => {
                                let value;

                                if (cell.value == null)
                                    value = this.formGroup.value.nullOutput.value;
                                else
                                    value = cell.value;

                                return this.formGroup.value.addQuotes ? `"${value}"` : value;
                            }
                        )
                        .join(this.formGroup.value.separator.value);

                    rawDsv += this.formGroup.value.newLine.value;
                });

                let blob: Blob = new Blob([rawDsv], {type: this.formGroup.value.separator.mimeType});

                let fileName = `${this.formGroup.value.filename}.${this.formGroup.value.separator.extension}`;
                let fileSize = this.formatBytes(blob.size);

                AlertStorage.info = `'${fileName}' has been successfully exported. File size: '${fileSize}'.`;
                saveAs(blob, fileName);
            }

            this._isExporting = false;
        }
    }

    private executeToDsv(): void {
        if (!this.isExporting && this._sql && this._database) {
            this._isExporting = true;

            if (this.formGroup.valid) {
                this._exporting$ = this._queryService.executeDsv(
                    this._sql,
                    this._database,
                    this.formGroup.value.separator.value,
                    this.formGroup.value.newLine.value,
                    this.formGroup.value.includeHeader,
                    this.formGroup.value.addQuotes,
                    this.formGroup.value.nullOutput.value)
                    .subscribe({
                        next: (blobEvent) => {

                            if (blobEvent.type == HttpEventType.DownloadProgress) {
                                this._progress = blobEvent.type;
                                this._blobSize = blobEvent.loaded;
                            }

                            if (blobEvent.type === HttpEventType.Response) {
                                let fileName = `${this.formGroup.value.filename}.${this.formGroup.value.separator.extension}`;

                                if (blobEvent.body) {
                                    saveAs(blobEvent.body, fileName);

                                    let fileSize = this.formatBytes(blobEvent.body.size);

                                    AlertStorage.info = `'${fileName}' has been successfully exported. File size: '${fileSize}'.`;
                                }
                            }
                        }, complete: () => {
                            this._isExporting = false;
                            setTimeout(
                                () => {
                                    this._progress = undefined;
                                },
                                1000);
                        }
                    });
            }
        }
    }

    // endregion Private Methods

    ngOnInit(): void {
        $(`#${this.modalId}`).on(
            'hidden.bs.modal',
            () => {
                this._data = undefined;
                this._sql = undefined;
                this._database = undefined;
            });
    }

    // region Public Methods

    rejectExporting(): void {
        this.confirmModalComponent?.confirm(
            () => {
                this._progress = undefined;
                this._isExporting = false;
                this._exporting$?.unsubscribe();
            },
            undefined,
            'Are you sure to cancel the exporting?');
    }

    export(): void {
        if (this._isExecuteToDsv) {
            this.executeToDsv();
        } else {
            this.exportToDsv();
        }
    }

    formatBytes(size: number): string {
        const units = ['B', 'KB', 'MB', 'GB'];

        let length = 0;

        while (size >= 1024) {
            length++;
            size = size / 1024;
        }

        return (size.toFixed(size < 10 && size > 0 ? 1 : 0) + ' ' + units[length]);
    }

    openExportToDsvModal(data: Table | undefined): void {
        this._isExecuteToDsv = false;
        this._data = data;

        $(`#${this.modalId}`).modal('toggle');
    }

    openExecuteToDsvModal(sql: string | undefined, database: string | undefined): void {
        this._isExecuteToDsv = true;
        this._sql = sql;
        this._database = database;

        $(`#${this.modalId}`).modal('toggle');
    }

    getErrors(errors: ValidationErrors | null): string[] {
        let errorList: string[] = [];

        if (errors)
            errorList = Object.keys(errors).map(errorKey => {
                let error: string;

                if (errorKey == 'required') {
                    error = 'The field is required';
                } else
                    error = errors[errorKey].toString();

                return error;
            });

        return errorList;
    }

    // endregion Public Methods
}