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
    public buttonTitle: string;

    public get isJiraServerComposeApplication(): boolean {
        return this.application === ApplicationType.JiraServerCompose;
    }

    private jiraUrl: string;
    private status: number;
    private application: string;
    private staticTabChangeUrl = false;
    private authorizationUrl: string;

    // tslint:disable-next-line:max-line-length
    private readonly JIRA_SERVER_TAB_AUTH_REDIRECT_URL = `/#/config;application=jiraServerTab;endpoint=${encodeURIComponent('/loginResult.html')};jiraUrl=${this.jiraUrl}`;

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
            this.authorizationUrl = `/#/config;application=${this.application}`;
            if (this.application === ApplicationType.JiraServerTab) {
                this.authorizationUrl = this.JIRA_SERVER_TAB_AUTH_REDIRECT_URL;
            }

            localStorage.setItem('redirectUri', this.authorizationUrl);
            await this.authService.authenticate('./login.html');
            await this.onAuthenticationSucceeded();

        } catch (error) {
            await this.onAuthenticationFailure(error);
        }
    }

    private async getButtonTitle(): Promise<string> {
        const isAuthenticated = this.authService.isAuthenticated;

        if (this.status === StatusCode.Forbidden ||
            (this.status === StatusCode.Unauthorized && isAuthenticated)) {
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

        this.application = application || ApplicationType.JiraServerTab;

        this.staticTabChangeUrl = staticTabChangeUrl ? !!staticTabChangeUrl : false;
        this.jiraUrl = this.utilService.convertStringToNull(jiraUrl);

        if (tenantId) {
            this.utilService.setTeamsContext(tenantId);
        }
    }

    private async getAuthorizationUrl(staticTabChangeUrl: boolean = false): Promise<string> {

        const { jiraAuthUrl } = await this.authService.getAuthorizationUrl(
            this.jiraUrl,
            this.application,
            staticTabChangeUrl
        );

        logger(`LoginComponent::getAuthorizationUrl called: authorizationUrl: ${jiraAuthUrl}`);

        return decodeURIComponent(jiraAuthUrl);
    }

    private async onAuthenticationSucceeded(): Promise<void> {
        try {
            await this.getJiraUserDataAndNavigateToView();
        } catch (error) {
            this.errorService.showDefaultError(error);
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
        if (error === 'CancelledByUser' && this.authService.isAuthenticated) {
            this.jiraUrl = this.route.snapshot.params.previousJiraUrl || this.jiraUrl;

            await this.getJiraUserDataAndNavigateToView();
            return;
        }

        this.showLoginButton();
    }

    private async getJiraUserDataAndNavigateToView(): Promise<void> {
        const endpoint = this.route.snapshot.params.endpoint;
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
                error,
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
