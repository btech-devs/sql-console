import {BrowserModule} from '@angular/platform-browser';
import {NgModule} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {HTTP_INTERCEPTORS, HttpClientModule} from '@angular/common/http';
import {RouterModule} from '@angular/router';
import {AppComponent} from './app.component';
import {getRoutes} from './utils';
import {ErrorComponent} from './components/error/error.component';
import {CopyClick} from './directives/copyClick.directive';
import {QueryConsoleComponent} from './components/queryConsole/queryConsole.component';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {ConnectionComponent} from './components/connection/connection.component';
import {ConnectionService} from './_services/connection.service';
import {AuthInterceptor} from './_auth/auth.interceptor';
import {JwtService} from './_services/jwt.service';
import {ResizableCol, ResizableRow} from './directives/resizable.directive';
import {QueryService} from './_services/query.service';
import {DatabaseService} from './_services/database.service';
import {GoogleAuthorizationComponent} from './components/google-authorization/google-authorization.component';
import {GoogleAuthService} from './_services/google-auth.service';
import {SessionAuthGuard} from './_auth/session-auth.guard';
import {GoogleAuthGuard} from './_auth/google-auth.guard';
import {DsvExporterComponent} from './components/queryConsole/dataExporters/dsvExporter/dsvExporter.component';
import {ConfirmModalComponent} from './components/confirmModal/confirmModal.component';
import {MetadataService} from './_services/metadata.service';

@NgModule({
    declarations: [
        AppComponent,
        QueryConsoleComponent,
        ErrorComponent,
        CopyClick,
        DsvExporterComponent,
        ConfirmModalComponent,
        ConnectionComponent,
        ResizableRow,
        ResizableCol,
        CopyClick,
        GoogleAuthorizationComponent
    ],
    imports: [
        BrowserModule.withServerTransition({appId: 'ng-cli-universal'}),
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        BrowserAnimationsModule,
        RouterModule.forRoot(getRoutes())
    ],
    providers: [
        ConnectionService,
        JwtService,
        SessionAuthGuard,
        GoogleAuthGuard,
        QueryService,
        DatabaseService,
        GoogleAuthService,
        MetadataService,
        {provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true}
    ],
    bootstrap: [AppComponent]
})
export class AppModule {
}