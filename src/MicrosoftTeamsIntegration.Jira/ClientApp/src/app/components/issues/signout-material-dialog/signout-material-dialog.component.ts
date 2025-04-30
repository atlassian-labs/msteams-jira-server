// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, OnInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import {
    ApiService,
    AuthService,
    UtilService,
    AppInsightsService,
} from '@core/services';

@Component({
    selector: 'app-signout',
    templateUrl: './signout-material-dialog.component.html',
    styleUrls: ['./signout-material-dialog.component.scss'],
    standalone: false
})
export class SignoutMaterialDialogComponent implements OnInit {

    private jiraUrl: string | undefined;

    public isSigningOut = false;
    public signOutMessage = 'Are you sure you want to sign out?';

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: any,
        public dialogRef: MatDialogRef<SignoutMaterialDialogComponent>,
        private authService: AuthService,
        private apiService: ApiService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService
    ) { }

    public async ngOnInit(): Promise<void> {
        try {
            this.jiraUrl = this.utilService.convertStringToNull(this.data.jiraUrl);
            this.signOutMessage =
                await this.apiService.getNotificationSettings(this.jiraUrl as string) ?
                    'Are you sure you want to sign out and stop receiving notifications from Jira?' :
                    'Are you sure you want to sign out?';
        } catch (error) {
            this.appInsightsService.trackException(
                new Error(error as any),
                'SignoutMaterialDialogComponent::ngOnInit'
            );

            if ((error as any).status && (error as any).status === 401) {
                this.dialogRef.close(false);
            }
        }
    }

    public async signOut(): Promise<void> {
        try {
            this.isSigningOut = true;
            // remove personal subscription if exists
            await this.apiService.removePersonalNotification(this.jiraUrl as string);
            await this.apiService.logOut(this.jiraUrl as string);
        } catch (error) {
            this.appInsightsService.trackException(
                new Error('Error while signout from Jira'),
                'SignoutMaterialDialogComponent::signOut',
                { originalErrorMessage: (error as any).message }
            );
        } finally {
            this.isSigningOut = false;
            this.dialogRef.close(true);
        }
    }
}
