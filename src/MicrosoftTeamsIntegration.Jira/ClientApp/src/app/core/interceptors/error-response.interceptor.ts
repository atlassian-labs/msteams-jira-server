import { Injectable } from '@angular/core';
import {
    HttpEvent,
    HttpInterceptor,
    HttpHandler,
    HttpRequest,
    HttpErrorResponse
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { ErrorService, AppInsightsService } from '@core/services';
import { logger } from '@core/services/logger.service';

import { StatusCode } from '@core/enums';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';

@Injectable({ providedIn: 'root' })
export class ErrorResponseInterceptor implements HttpInterceptor {

    private readonly ADDON_NOT_INSTALLED_ERROR_MESSAGE = 'Addon is not installed';
    private readonly JIRA_USER_NOT_AUTHORIZED_ERROR_MESSAGE = 'User not authorized';

    constructor(
        private errorService: ErrorService,
        private appInsights: AppInsightsService,
        private matDialog: MatDialog
    ) { }

    intercept(
        req: HttpRequest<any>,
        next: HttpHandler
    ): Observable<HttpEvent<any>> {
        return next.handle(req).pipe(
            tap(
                event => { },
                error => {
                    if (error instanceof HttpErrorResponse) {
                        this.logErrorResponse(error);
                        this.handleHttpError(error);
                    }
                }
            )
        );
    }

    private logErrorResponse(error: HttpErrorResponse): void {
        logger(`errorResponseInterceptor::handleHttpError:error:
                error.message: ${error.message}
                error.error: ${JSON.stringify(error.error, null, 4)}`
        );

        if (error.status === StatusCode.Unauthorized || error.status === StatusCode.Forbidden) {
            this.appInsights.trackEvent('Authentication failed.');
            return;
        }

        this.appInsights.trackException(
            new Error(
                `Tab app received error HTTP response: ${error.status} - ${error.statusText}`
            ),
            'ErrorResponseInterceptor::logErrorResponse',
            {
                headers: error.headers,
                url: error.url,
                status: error.status,
                statusText: error.statusText,
                message: error.message
            }
        );
    }

    private handleHttpError(error: HttpErrorResponse): void {
        const errorMessage = error.error && error.error.error;

        if (error.status === StatusCode.Forbidden) {
            if (errorMessage === this.ADDON_NOT_INSTALLED_ERROR_MESSAGE) {
                this.errorService.showAddonNotInstalledWindow();
                return;
            }

            if (errorMessage === this.JIRA_USER_NOT_AUTHORIZED_ERROR_MESSAGE) {
                this.errorService.goToLoginWithStatusCode(error.status);
                return;
            }
        }

        if (error.status === StatusCode.Unauthorized) {
            this.errorService.goToLoginWithStatusCode(error.status);
            return;
        }

        if (error.status === StatusCode.ServiceUnavailable) {
            this.errorService.showJiraUnavailableWindow(error.status);
            return;
        }

        this.matDialog.closeAll();
        this.errorService.showErrorModal(error);
    }
}
