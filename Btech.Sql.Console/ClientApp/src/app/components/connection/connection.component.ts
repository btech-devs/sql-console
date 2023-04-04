import {Component} from '@angular/core';
import {AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators} from '@angular/forms';
import {ConnectionService} from '../../_services/connection.service';
import {ActivatedRoute, NavigationExtras, Router} from '@angular/router';
import {BaseComponent} from '../../_base/base.component';
import {
    AlertStorage,
    IS_STATIC_CONNECTION_KEY,
    JWT_PUBLIC_KEY_KEY,
    PATH_CONSOLE,
    PATH_ERROR,
    REFRESH_TOKEN_KEY,
    SESSION_TOKEN_KEY
} from '../../utils';
import {catchError, throwError} from 'rxjs';
import {JwtService} from '../../_services/jwt.service';
import {InstanceType} from '../../_models/instance-type';
import {SessionStorageService} from '../../_services/sessionStorageService';
import {Response} from '../../_models/responses/base/response';
import {LocalStorageService} from '../../_services/localStorageService';
import {MetadataService} from '../../_services/metadata.service';

@Component({
    selector: 'app-connection',
    templateUrl: './connection.component.html',
    styleUrls: ['./connection.component.less']
})
export class ConnectionComponent extends BaseComponent {
    readonly hostMaxLength = 255;

    readonly portMaxLength = 5;

    readonly usernameMaxLength = 63;

    readonly passwordMaxLength = 99;

    readonly defaultPortPgSql = 5432;
    readonly defaultPortMsSql = 1433;

    readonly validHostRegex: RegExp = new RegExp('^[a-z0-9]+([\\-\.][a-z0-9]+)*\\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$');
    readonly validCloudSqlRouteRegex: RegExp = new RegExp('^\/cloudsql\/[\\w_\-]+:[\\w_\-]+:[\\w_\-]+\/?$');
    readonly validIpRegex: RegExp = new RegExp('^((((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4})|localhost)$');

    readonly options: { description: string; value: string }[] = [
        {value: InstanceType.PgSql.toString(), description: 'PostgreSQL (AlloyDB)'},
        {value: InstanceType.MsSql.toString(), description: 'Microsoft SQL Server'}
    ];

    private _returnUrl: string;

    private _error?: string = undefined;
    get error(): string | undefined {
        return this._error;
    }

    private _isStaticConnectionLoading: boolean = true;
    get isStaticConnectionLoading(): boolean {
        return this._isStaticConnectionLoading;
    }

    private readonly _connectionForm: FormGroup;
    get connectionForm(): FormGroup {
        return this._connectionForm;
    }

    private _loading: boolean = false;
    get loading(): boolean {
        return this._loading;
    }

    set loading(value: boolean) {
        this._loading = value;
    }

    private _jwtPublicKeyLoaded: boolean;
    get jwtPublicKeyLoaded(): boolean {
        this._jwtPublicKeyLoaded = !!LocalStorageService.get(JWT_PUBLIC_KEY_KEY);

        return this._jwtPublicKeyLoaded;
    }

    constructor(
        private _jwtService: JwtService,
        private _formBuilder: FormBuilder,
        private _connectionService: ConnectionService,
        private _route: ActivatedRoute,
        private _router: Router,
        private _metadataService: MetadataService) {
        super();
        this._returnUrl = this._route.snapshot.queryParams['returnUrl'] || PATH_CONSOLE;
        this._connectionForm = this._formBuilder.group({
            instanceType: [null, [Validators.required]],
            host: [null, [Validators.required, Validators.maxLength(this.hostMaxLength), this.hostValidator()]],
            port: [null, [Validators.required, Validators.maxLength(this.portMaxLength), this.portValidator()]],
            username: [null, [Validators.required, Validators.maxLength(this.usernameMaxLength)]],
            password: [null, [Validators.required, Validators.maxLength(this.passwordMaxLength)]]
        });
        this._jwtPublicKeyLoaded = LocalStorageService.get(JWT_PUBLIC_KEY_KEY) != undefined;
    }

    async ngOnInit() {

        await this.checkJwtPublicKey();

        this._error = undefined;
        this._isStaticConnectionLoading = true;

        this._connectionService.getStaticConnection()
            .subscribe({
                next: response => {

                    if (response.errorMessage) {
                        this._error = response.errorMessage;
                    } else {
                        if (response?.data?.sessionToken?.length) {
                            SessionStorageService.set(SESSION_TOKEN_KEY, response.data.sessionToken);
                            SessionStorageService.set(REFRESH_TOKEN_KEY, response.data.refreshToken);
                            SessionStorageService.set(IS_STATIC_CONNECTION_KEY, true);
                        }

                        if (SessionStorageService.get(SESSION_TOKEN_KEY) && SessionStorageService.get(REFRESH_TOKEN_KEY)) {
                            this._router.navigate([PATH_CONSOLE]);
                        } else {
                            SessionStorageService.clear();
                        }
                    }

                    this._isStaticConnectionLoading = false;
                },
                error: (error: Error) => {
                    this._error = error.message;
                    this._isStaticConnectionLoading = false;
                }
            });
    }

    onConnect() {
        this._connectionForm?.markAllAsTouched();

        if (!this._connectionForm?.valid) {
            AlertStorage.error = 'Please fill all fields correctly';
        } else {
            this._loading = true;

            this._connectionService.open(this._connectionForm.value)
                .pipe(catchError(this.handleError.bind(this)))
                .subscribe(
                    {
                        next: response => {
                            // validation error
                            if (response.validationErrorMessages != undefined) {
                                AlertStorage.error = 'Please fill all fields correctly';

                                response.validationErrorMessages.forEach((error) => {
                                    this._connectionForm?.controls[error.key].setErrors({validationError: error.value});
                                });
                            }
                            // other errors
                            else if (response.errorMessage != undefined) {
                                AlertStorage.error = response.errorMessage;
                            } else if (response.errorMessages != undefined) {
                                response.errorMessages.forEach((error) => {
                                    AlertStorage.error = error;
                                });
                            } else if (response.data?.sessionToken != undefined &&
                                response.data.refreshToken != undefined) {

                                let validationResult = this._jwtService.validateToken(response.data.sessionToken);

                                if (validationResult.isValid) {
                                    this._router.navigateByUrl(this._returnUrl);
                                    SessionStorageService.set(SESSION_TOKEN_KEY, response.data.sessionToken);
                                    SessionStorageService.set(REFRESH_TOKEN_KEY, response.data.refreshToken);
                                } else {
                                    this.checkJwtPublicKey().then();
                                    AlertStorage.error = 'Token validation failed. Try again or contact the administrator.';
                                }
                            } else {
                                AlertStorage.error = 'Unexpected content';
                            }
                        }
                    })
                // finally
                .add(() => {
                    this.loading = false;
                });
        }
    }

    hostValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            let validationError: string | null = null;

            if (control.value != '' && control.value != null) {
                if (!this.validHostRegex.test(control.value) &&
                    !this.validIpRegex.test(control.value) &&
                    !this.validCloudSqlRouteRegex.test(control.value)) {
                    validationError = 'The value does not match the rules of the host address';
                }
            } else {
                validationError = 'Host is not specified';
            }

            return validationError == null ? null : {validationError: validationError};
        };
    }

    portValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            let validationError: string | null = null;

            if (control.value != null && control.value != '' && control.value > 65535) {
                validationError = 'Port can not be greater than 65535';
            } else if (control.value != null && control.value != '' && control.value < 1) {
                validationError = 'Port can not be less than 1';
            }

            return validationError == null ? null : {validationError: validationError};
        };
    }

    // TODO: should be replaced by centralized error handler interceptor
    private handleError(error: any) {
        let errorMessage = '';

        if (error.error instanceof ErrorEvent) {
            errorMessage = error.error.message;
        } else if (error.error?.errorMessage != undefined) {
            errorMessage = `<b>Error Code</b>: ${error.status}. <br/>
                      <b>Message</b>: ${error.error.errorMessage}`;
        } else if (error.status != 401 && error.status != 403) {
            errorMessage = 'Something went wrong. Please, call the administrator.';

            console.error('Something went wrong. Error: ', error);
        }

        if (errorMessage != '') {
            AlertStorage.error = errorMessage;
        }

        return throwError(() => {
            return errorMessage;
        });
    }

    selected(event: Event): void {
        // @ts-ignore
        event.target?.classList?.add('selected');

        if (this._connectionForm?.controls['port']?.value == this.defaultPortPgSql ||
            this._connectionForm?.controls['port']?.value == this.defaultPortMsSql ||
            !this._connectionForm?.controls['port']?.value) {
            if (this._connectionForm?.controls['instanceType']?.value == InstanceType.PgSql)
                this._connectionForm?.controls['port'].setValue(this.defaultPortPgSql);
            else if (this._connectionForm?.controls['instanceType']?.value == InstanceType.MsSql)
                this._connectionForm?.controls['port'].setValue(this.defaultPortMsSql);
        }

    }

    private getJwtPublicKey(): Promise<Response<string>> {

        return new Promise<Response<string>>((resolve, reject) => {
            this._metadataService
                .getJwtPublicKey()
                .subscribe({
                    next: (response) => resolve(response),
                    error: (error) => reject(error)
                });
        });
    }

    private async checkJwtPublicKey() {

        let result: boolean = false;
        let errorMessageNavigationExtras: NavigationExtras | undefined = undefined;

        if (this.jwtPublicKeyLoaded) {
            result = true;
        } else {
            let response = await this.getJwtPublicKey();

            if (response.errorMessage != undefined) {
                errorMessageNavigationExtras = {
                    queryParams: {
                        message: response.errorMessage,
                        tryAgainButton: true
                    }
                };
            } else if (response.errorMessages != undefined) {
                let errorMessage: string = '';

                response.errorMessages.forEach((error) => {
                    errorMessage += `${error}\n`;
                });

                errorMessageNavigationExtras = {
                    queryParams: {
                        message: errorMessage,
                        tryAgainButton: true
                    }
                };
            }

            if (errorMessageNavigationExtras != undefined) {
                await this._router.navigate([PATH_ERROR], errorMessageNavigationExtras);
            } else {

                if (response.data != undefined) {
                    LocalStorageService.set(JWT_PUBLIC_KEY_KEY, response.data.replace(/\\n/gm, '\n'));
                    result = true;
                } else {
                    errorMessageNavigationExtras = {
                        queryParams: {
                            message: 'The server did not send required jwt public key. Try again in a few minutes, or contact the administrator.',
                            tryAgainButton: true
                        }
                    };
                    await this._router.navigate([PATH_ERROR, errorMessageNavigationExtras]);
                }
            }

        }

        return result;
    }
}