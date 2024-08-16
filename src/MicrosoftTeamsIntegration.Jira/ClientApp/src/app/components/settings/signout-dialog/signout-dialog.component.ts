// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
    ApiService,
    AuthService,
    UtilService,
    AppInsightsService,
} from '@core/services';

@Component({
    selector: 'app-signout-dialog',
    templateUrl: './signout-dialog.component.html',
    styleUrls: ['./signout-dialog.component.scss']
})
export class SignoutDialogComponent implements OnInit {

    private jiraUrl: string | any;

    public isSigningOut = false;

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private authService: AuthService,
        private apiService: ApiService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService
    ) { }

    public async ngOnInit(): Promise<void> {

        this.appInsightsService.logNavigation('SignoutDialogComponent', this.route);

        this.parseParams();
    }

    public async onSignOut(): Promise<void> {
        try {
            this.isSigningOut = true;
            await this.apiService.logOut(this.jiraUrl);
        } catch (error) {
            this.appInsightsService.trackException(
                new Error('Error while signout from Jira'),
                'SignoutComponent::signOut',
                { originalErrorMessage: (error as any).message }
            );
        } finally {
            this.isSigningOut = false;
            await this.router.navigate(['/config', { ...this.route.snapshot.params} ]);
        }
    }

    public async navigateBack(): Promise<void> {
        await this.router.navigate(['/settings', { ...this.route.snapshot.params, jiraUrl: this.jiraUrl} ]);
    }

    private parseParams(): void {
        const { jiraUrl } = this.route.snapshot.params;
        this.jiraUrl = jiraUrl;
    }
}
