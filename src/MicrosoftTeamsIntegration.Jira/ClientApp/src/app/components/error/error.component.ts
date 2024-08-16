// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ErrorService, AppInsightsService } from '@core/services';
import { ApplicationType } from '@core/enums';
import { UtilService } from '@core/services';

@Component({
    selector: 'app-error',
    templateUrl: './error.component.html',
    styles: [`
        .retry-button {
            margin: 10px;
        }

        .error__container {
            padding: 10px;
        }
    `]
})
export class ErrorComponent implements OnInit {
    public message: string | undefined;
    public wrapperClass = '';
    public showRetryButton = false;
    public status: number | undefined;

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private errorService: ErrorService,
        private appInsightService: AppInsightsService,
        private utilService: UtilService
    ) { }

    public ngOnInit() {
        this.appInsightService.logNavigation('ErrorComponent', this.route);

        this.showRetryButton = this.errorService.showRetryButton;
        this.message = this.route.snapshot.queryParams['message'];
        this.status = this.route.snapshot.queryParams['status'];
    }

    public retry(): void {
        this.router.navigate([
            this.errorService.redirectOnRetryRoute,
            this.route.snapshot.params
        ]);
    }
}
