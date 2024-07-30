// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatLegacyButtonModule as MatButtonModule } from '@angular/material/legacy-button';
import { MatLegacyCheckboxModule as MatCheckboxModule } from '@angular/material/legacy-checkbox';
import { MatLegacyDialogModule as MatDialogModule } from '@angular/material/legacy-dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatLegacyInputModule as MatInputModule } from '@angular/material/legacy-input';
import { MatLegacyRadioModule as MatRadioModule } from '@angular/material/legacy-radio';
import { MatMomentDateModule, MAT_MOMENT_DATE_ADAPTER_OPTIONS } from '@angular/material-moment-adapter';
import { MatLegacyPaginatorModule as MatPaginatorModule } from '@angular/material/legacy-paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatLegacyTableModule as MatTableModule } from '@angular/material/legacy-table';
import { MatLegacyTooltipModule as MatTooltipModule } from '@angular/material/legacy-tooltip';
import { NgSelectModule } from '@ng-select/ng-select';
import { CdkTableModule } from '@angular/cdk/table';
import { FlexLayoutModule } from '@angular/flex-layout';

import { CoreModule } from '@core/core.module';
import { SharedModule } from '@shared/shared.module';
import { RoutingModule } from '@app/app-routing.module';

import { TimeToResolutionIconDirective } from '@core/directives/time.to.resolution-derictive';

// Components
import { AppComponent } from '@app/components/app.component';
import {
    LoginComponent, StaticTabComponent,
    IssuesComponent, ErrorComponent, FavoriteFiltersEmptyComponent,
    EditIssueDialogComponent, CreateIssueDialogComponent,
    AssigneeDropdownComponent, PriorityDropdownComponent, StatusDropdownComponent,
    SettingsFiltersComponent as SettingsProjectComponent, SettingsComponent,
    IssueCommentComponent, NewCommentComponent,
    IssuesTableMobileComponent, ConnectJiraComponent, CreateCommentDialogComponent,
    IssueDetailsComponent, SignoutMaterialDialogComponent, SignoutDialogComponent,
    GoToWebsiteComponent, CommentIssueDialogComponent
} from '@app/components';
import { DynamicFieldsDirective } from './components/issues/fields/dynamic-fields.directive';
import { SelectFieldComponent } from './components/issues/fields/select-field.component';
import { TextFieldSingleComponent } from './components/issues/fields/text-field-single.component';
import { DynamicFieldsComponent } from './components/issues/fields/dynamic-fields.component';
import { DatePickerFieldComponent } from './components/issues/fields/datepicker-field.component';
import { TextFieldMultiComponent } from './components/issues/fields/text-field-multi.component';
import { RadioSelectFieldComponent } from './components/issues/fields/radio-select-field.component';
import { CheckboxSelectFieldComponent } from './components/issues/fields/checkbox-select-field';
import { TextFieldNumberComponent } from './components/issues/fields/text-field-number.component';
import { UserPickerFieldComponent } from './components/issues/fields/userpicker-field.component';
import { LabelsFieldComponent } from './components/issues/fields/labels-field.component';
import { SprintFieldComponent } from './components/issues/fields/sprint-field.component';
import { EpicFieldComponent } from './components/issues/fields/epic-field.component';
import { UrlFieldComponent } from './components/issues/fields/url-field.component';
import { SelectCascadingFieldComponent } from './components/issues/fields/select-cascading-field.component';
import { MatLegacySnackBarModule as MatSnackBarModule } from '@angular/material/legacy-snack-bar';
import { SuccessSnackbarComponent } from './components/snack-bars/success-bar/success-snackbar.component';
import { ErrorSnackbarComponent } from './components/snack-bars/error-bar/error-snackbar.component';

@NgModule({
    imports: [
        BrowserAnimationsModule,
        FormsModule,
        ReactiveFormsModule,
        RoutingModule,
        BrowserModule,
        HttpClientModule,
        MatCheckboxModule,
        MatDialogModule,
        MatDatepickerModule,
        MatInputModule,
        MatRadioModule,
        MatMomentDateModule,
        MatButtonModule,
        MatTooltipModule,
        MatTableModule,
        NgSelectModule,
        CdkTableModule,
        FlexLayoutModule,
        MatSortModule,
        MatPaginatorModule,
        CoreModule,
        SharedModule,
        MatSnackBarModule
    ],
    declarations: [
        AppComponent,
        LoginComponent,
        StaticTabComponent,
        IssuesComponent,
        ErrorComponent,
        FavoriteFiltersEmptyComponent,
        SettingsComponent,
        SettingsProjectComponent,
        EditIssueDialogComponent,
        AssigneeDropdownComponent,
        PriorityDropdownComponent,
        StatusDropdownComponent,
        CreateIssueDialogComponent,
        IssueCommentComponent,
        NewCommentComponent,
        TimeToResolutionIconDirective,
        IssuesTableMobileComponent,
        ConnectJiraComponent,
        CreateCommentDialogComponent,
        CommentIssueDialogComponent,
        IssueDetailsComponent,
        SignoutMaterialDialogComponent,
        SignoutDialogComponent,
        GoToWebsiteComponent,
        DynamicFieldsDirective,
        DynamicFieldsComponent,
        SelectFieldComponent,
        TextFieldSingleComponent,
        TextFieldMultiComponent,
        DatePickerFieldComponent,
        RadioSelectFieldComponent,
        CheckboxSelectFieldComponent,
        TextFieldNumberComponent,
        UserPickerFieldComponent,
        LabelsFieldComponent,
        SprintFieldComponent,
        EpicFieldComponent,
        UrlFieldComponent,
        SelectCascadingFieldComponent,
        SuccessSnackbarComponent,
        ErrorSnackbarComponent
    ],
    bootstrap: [AppComponent],
    providers: [
        { provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS, useValue: { useUtc: true } }
    ]
})
export class AppModule { }
