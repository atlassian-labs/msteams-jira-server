// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable, Injector, NgZone } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatDialogConfig, MatDialog } from '@angular/material/dialog';

import { AppInsightsService } from '@core/services/app-insights.service';
import { StatusCode } from '@core/enums';
import { ConfirmationDialogData, DialogType } from '@core/models/dialogs/issue-dialog.model';
import { ApplicationType } from './../enums/application-type.enum';
import { ConfirmationDialogComponent } from '@app/components/issues/confirmation-dialog/confirmation-dialog.component';

@Injectable({ providedIn: 'root' })
export class ErrorService {
    public showRetryButton = false;
    public redirectOnRetryRoute = '/login';

    public readonly DEFAULT_ERROR_MESSAGE = 'Something went wrong.';
    public readonly UNAVAILABLE_SITE_MESSAGE = 'Your Jira site currently unavailable.';
    public readonly ADDON_NOT_INSTALLED_MESSAGE = ' is not installed on your Jira.';
    public readonly PROJECT_PERMISSIONS_ERROR = 'This project isn\'t available. ' +
        'It may have been deleted or your permissions may have changed.';
    private dialogDefaultSettings: MatDialogConfig = {
        width: '400px',
        height: 'auto',
        minWidth: '200px',
        minHeight: '150px',
        ariaLabel: 'Error dialog',
        closeOnNavigation: true,
        autoFocus: false,
        role: 'dialog'
    };

    constructor(
        private readonly zone: NgZone,
        private readonly injector: Injector,
        private readonly appInsightsService: AppInsightsService,
        private errorDialog: MatDialog
    ) { }

    public showJiraUnavailableWindow(status: number): void {
        const router = this.injector.get(Router);

        this.appInsightsService.trackWarning(
            new Error('Jira site currently unavailable'),
            'ErrorService::showJiraUnavailableWindow'
        );

        this.showRetryButton = true;
        this.redirectOnRetryRoute = '/issues';

        router.navigate(['/error'], { queryParams: { message: this.UNAVAILABLE_SITE_MESSAGE, status }, queryParamsHandling: 'merge' });
    }

    public showAddonNotInstalledWindow(): void {
        const router = this.injector.get(Router);

        this.appInsightsService.trackWarning(
            new Error('Addon is not installed on Jira'),
            'ErrorService::showAddonNotInstalledWindow'
        );

        router.routerState.root.children[0].params.subscribe(params => {
            router.navigate(['/error'], { queryParams: { message: this.ADDON_NOT_INSTALLED_MESSAGE, ...params } });
        });
    }

    public showDefaultError(error: Error | HttpErrorResponse): void {
        const router = this.injector.get(Router);

        this.appInsightsService.trackException(error as Error, 'ErrorService::handleError');

        let message = this.getErrorMessage(error);
        router.routerState.root.children[0].params.subscribe(params => {
            const application = params['application'];
            message = (
                (application === ApplicationType.JiraServerCompose || application === ApplicationType.JiraServerTab) &&
                    error instanceof HttpErrorResponse
            )
                ? error.error.error
                : message;

            router.navigate(['/error'], { queryParams: { message, ...params } });
        });

        message = error instanceof HttpErrorResponse && error.error && error.error.error ?
            error.error.error
            : '';

        router.navigate(['error'], { queryParams: { message } });
    }

    public showErrorModal(error: Error | HttpErrorResponse): void {
        this.appInsightsService.trackException(error as Error, 'ErrorService::handleError');
        const message = this.getHttpErrorMessage(error);

        const dialogConfig = {
            ...this.dialogDefaultSettings,
            ...{
                data: {
                    title: 'Error',
                    subtitle: message,
                    buttonText: 'Dismiss',
                    dialogType: DialogType.ErrorLarge
                } as ConfirmationDialogData
            }
        };

        this.errorDialog.open(ConfirmationDialogComponent, dialogConfig);
    }

    public goToLoginWithStatusCode(status: StatusCode): void {
        const router = this.injector.get(Router);
        router.routerState.root.children[0].params.subscribe(params => {
            router.navigate(['/login', { ...params, status: status }]);
        });
    }

    public async showMyFiltersEmptyError(): Promise<boolean> {
        const router = this.injector.get(Router);
        return this.zone.run(async () => await router.navigate(['/favorite-filters-empty']));
    }

    public getHttpErrorMessage(error: Error | HttpErrorResponse): string {
        let message = this.getErrorMessage(error);
        message = error instanceof HttpErrorResponse && error.error && error.error.error ?
            error.error.error
            : message;

        try {
            const parsedObj = JSON.parse(message);

            // Some error messages are coming in next format {"errorMessages":[],"errors":{}}
            // try to get message from list of messages
            if(parsedObj && parsedObj.errorMessages && parsedObj.errorMessages.length > 0 ) {
                return parsedObj.errorMessages[0];
            }
        } catch (err) {
        }
        // or return original message
        return message;
    }

    private getErrorMessage(error: Error | HttpErrorResponse): string {
        if (error instanceof HttpErrorResponse) {
            return `${error.status}: ${error.statusText}`;
        }

        return this.DEFAULT_ERROR_MESSAGE;
    }
}
