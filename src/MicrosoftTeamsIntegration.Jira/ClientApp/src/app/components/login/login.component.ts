// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import {
    AuthService,
    UtilService,
    ErrorService,
    AppInsightsService,
    RoutingState
} from '@core/services';

import { logger } from '@core/services/logger.service';
import { StatusCode, ApplicationType } from '@core/enums';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
    public isLoginButtonVisible = false;
    public buttonTitle: string | undefined;

    private jiraUrl: string | undefined;
    private status: number | undefined;
    private application: string | undefined;
    private authorizationUrl: string | undefined;
    private staticTabChangeUrl: boolean | undefined;

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private authService: AuthService,
        private routingState: RoutingState,
        private utilService: UtilService,
        private errorService: ErrorService,
        private appInsightsService: AppInsightsService,
    ) { }

    public async ngOnInit(): Promise<void> {
        this.appInsightsService.logNavigation('LoginComponent', this.route);

        this.parseParams();

        this.buttonTitle = await this.getButtonTitle();

        this.showLoginButton();
    }

    public async getAuthentication(): Promise<void> {
        try {
            if (!(await this.isJiraUserAuthorized())) {
                this.authorizationUrl = `/#/config;application=${this.application}`;

                if (this.application === ApplicationType.JiraServerTab) {
                    this.authorizationUrl =
                        '/#/config;application=jiraServerTab;' +
                        `endpoint=${encodeURIComponent('/loginResult.html')};jiraUrl=${this.jiraUrl as string}`;
                }
                await this.authService.authenticateJiraAccount(this.authorizationUrl);
            } else {
                await this.authService.authenticateMicrosoftAccount();
            }
            await this.onAuthenticationSucceeded();

        } catch (error) {
            await this.onAuthenticationFailure(error);
        }
    }

    private async getButtonTitle(): Promise<string> {
        if (!(await this.isJiraUserAuthorized())) {
            return 'Authorize in Jira';
        }

        return 'Sign in with Microsoft account';
    }

    private parseParams(): void {
        logger('LoginComponent::parseParams()::route.snapshot.params: ', this.route.snapshot.params);

        const {
            status,
            application,
            staticTabChangeUrl,
            jiraUrl,
            tenantId
        } = this.route.snapshot.params;

        this.status = Number(status);

        this.application = application || ApplicationType.JiraServerStaticTab;

        this.staticTabChangeUrl = staticTabChangeUrl ? !!staticTabChangeUrl : false;
        this.jiraUrl = this.utilService.convertStringToNull(jiraUrl);

        if (tenantId) {
            this.utilService.setTeamsContext(tenantId);
        }
    }

    private async isJiraUserAuthorized(): Promise<boolean> {
        const isMicrosoftUserAuthenticated = await this.authService.isAuthenticated();

        return this.status !== StatusCode.Forbidden &&
            (this.status !== StatusCode.Unauthorized || !isMicrosoftUserAuthenticated);
    }

    private async onAuthenticationSucceeded(): Promise<void> {
        try {
            await this.getJiraUserDataAndNavigateToView();
        } catch (error) {
            this.errorService.showDefaultError(error as any);
        }
    }

    private async onAuthenticationFailure(error: any): Promise<void> {
        this.appInsightsService.trackWarning(
            error,
            'LoginComponent::onAuthenticationFailure ',
            {
                jiraUrl: this.jiraUrl,
                application: this.application
            }
        );

        // Handle case if user finished microsoft authorization but has closed a window.
        if (error === 'CancelledByUser' && await this.authService.isAuthenticated()) {
            this.jiraUrl = this.route.snapshot.params['previousJiraUrl'] || this.jiraUrl;

            await this.getJiraUserDataAndNavigateToView();
            return;
        }

        this.showLoginButton();
    }

    private async getJiraUserDataAndNavigateToView(): Promise<void> {
        const endpoint = this.route.snapshot.params['endpoint'];
        let redirectRoute: string;
        let params = {};

        if (endpoint) {
            redirectRoute = endpoint;
        } else {
            redirectRoute = this.routingState.previousUrl;
            params = this.route.snapshot.params;
        }

        redirectRoute = decodeURIComponent(redirectRoute);

        try {
            logger(
                `LoginComponent::getJiraUserDataAndNavigateToView called:routerState.route: ${redirectRoute}`
            );

            await this.router.navigate([
                redirectRoute,
                {
                    ...params,
                    jiraUrl: this.jiraUrl
                }
            ]);
        } catch (error) {
            this.appInsightsService.trackException(
                error as any,
                'LoginComponent::GetJiraUserDataAndNavigateToView',
                {
                    redirectRoute,
                    params
                }
            );

            logger(`getJiraUserDataAndNavigateToView called: Caught error: ${error}`);
            this.showLoginButton();
        }
    }

    private showLoginButton(): void {
        this.isLoginButtonVisible = true;
    }
}
