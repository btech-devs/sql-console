import {Component, Input, OnInit} from '@angular/core';
import {ResultTable} from '../../models/resultTable';
import {saveAs} from 'file-saver';
import {QueryService} from '../../../../_services/query.service';
import {HttpErrorResponse, HttpEventType} from '@angular/common/http';
import {AlertStorage, formatBytes, separators} from '../../../../utils';
import {
    AbstractControl,
    FormControl,
    FormGroup,
    ValidationErrors, Validators,
} from '@angular/forms';
import {Subscription} from 'rxjs';
import {ConfirmModalService} from '../../../confirm-modal/confirm-modal.service';
import {ResponseBase} from '../../../../_models/responses/base/response';

declare var $: any;

@Component({
    selector: 'dsv-exporter',
    templateUrl: './dsv-exporter.component.html'
})
export class DsvExporterComponent implements OnInit {

    // region Readonly Public Fields

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
    private _data?: ResultTable;
    private _exporting$?: Subscription;
    private _error?: string = undefined;

    // endregion Private Fields

    // region Public Getters

    get formGroup(): FormGroup {
        return this._formGroup;
    }

    get error(): string | undefined {
        return this._error;
    }

    get separators() {
        return separators;
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

    constructor(private _queryService: QueryService, private _confirmService: ConfirmModalService) {
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
        this._error = undefined;

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
                let fileSize = formatBytes(blob.size);

                AlertStorage.info = `'${fileName}' has been successfully exported. File size: '${fileSize}'.`;
                saveAs(blob, fileName);
            }

            this._isExporting = false;
        }
    }

    private executeToDsv(): void {
        if (!this.isExporting && this._sql && this._database) {
            this._error = undefined;
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

                                    let fileSize = formatBytes(blobEvent.body.size);

                                    AlertStorage.info = `'${fileName}' has been successfully exported. File size: '${fileSize}'.`;
                                }
                            }
                        },
                        error: (error: HttpErrorResponse) => {
                          this._isExporting = false;
                          setTimeout(
                            () => {
                                this._progress = undefined;
                            },
                            1000);

                          if (error.error instanceof Blob && error.error.type == 'application/json'){
                              error.error.text().then(value => {
                                  let response = JSON.parse(value) as ResponseBase;
                                  this._error = response.errorMessage;
                              })
                          }

                          if (!this._error)
                              this._error = error.message;
                        },
                        complete: () => {
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
                this._error = undefined;
                this._data = undefined;
                this._sql = undefined;
                this._database = undefined;
            });
    }

    // region Public Methods

    rejectExporting(): void {
        this._confirmService.confirm('Are you sure to cancel the exporting?')
            .subscribe(result => {
                if (result){
                    this._progress = undefined;
                    this._isExporting = false;
                    this._exporting$?.unsubscribe();
                }
            })
    }

    export(): void {
        if (this._isExecuteToDsv) {
            this.executeToDsv();
        } else {
            this.exportToDsv();
        }
    }

    openExportToDsvModal(data: ResultTable | undefined): void {
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