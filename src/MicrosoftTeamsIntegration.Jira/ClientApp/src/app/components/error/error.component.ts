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

        .error__message {
            padding: 10px;
            font-size: 14px;
            max-width: 500px;
            text-align: center;
        }
    `],
    standalone: false
})
export class ErrorComponent implements OnInit {
    public message: string | undefined;
    public wrapperClass = '';
    public showRetryButton = false;
    public status: number | undefined;
    public iconUrl: string | undefined;
    public iconType: string | undefined;

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
        this.iconType = this.route.snapshot.queryParams['iconType'];

        if(this.iconType === 'connect') {
            this.iconUrl = 'https://product-integrations-cdn.atl-paas.net/jira-teams/jira-integration-2.png';
        } else {
            this.iconUrl = 'https://product-integrations-cdn.atl-paas.net/jira-teams/error-window.png';
        }
    }

    public retry(): void {
        this.router.navigate([
            this.errorService.redirectOnRetryRoute,
            this.route.snapshot.params
        ]);
    }
}
