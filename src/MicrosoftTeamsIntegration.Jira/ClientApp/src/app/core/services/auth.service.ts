// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as microsoftTeams from '@microsoft/teams-js';

import { AdalService } from '@core/services/adal.service';
import { DOCUMENT } from '@angular/common';

interface AuthenticateParameters {
    /**
     * The URL for the authentication pop-up.
     */
    url: string;
    /**
     * The preferred width for the pop-up. This value can be ignored if outside the acceptable bounds.
     */
    width?: number;
    /**
     * The preferred height for the pop-up. This value can be ignored if outside the acceptable bounds.
     */
    height?: number;
    /**
     * A function that is called if the authentication succeeds, with the result returned from the authentication pop-up.
     */
    successCallback?: (result?: string) => void;
    /**
     * A function that is called if the authentication fails, with the reason for the failure returned from the authentication pop-up.
     */
    failureCallback?: (reason?: string) => void;
}

interface JiraAuthUrl {
    jiraAuthUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
    constructor(
        @Inject(DOCUMENT) private document: Document,
        private readonly http: HttpClient,
        private readonly adalService: AdalService,
    ) { }


    public get isAuthenticated(): boolean {
        return !!this.adalService.getCachedToken();
    }

    public getCachedToken(): string {
        return this.adalService.getCachedToken();
    }

    public authenticate(url: string, useReplaceMethod?: boolean): Promise<void | string> {
        if (useReplaceMethod || window.opener) {
            window.location.replace(url);
            return Promise.resolve();
        }

        return new Promise((resolve: any, reject: any): Promise<void> | any => {
            try {
                const authenticateParameters: AuthenticateParameters = {
                    url,
                    width: 1024,
                    height: 1024,
                    successCallback: resolve,
                    failureCallback: reject
                };

                microsoftTeams.authentication.authenticate(authenticateParameters);
            } catch (e) {
                // in general is true only for mobile
                // as far as microsoftTeams.authentication.authenticate() can not be called from the inside of 'authorization' context,
                // try to redirect to another page in the same frame
                if (this.document && this.document.location) {
                    this.document.location.href = url;
                    return Promise.resolve();
                }
                return Promise.reject();
            }
        });
    }

    public getAuthorizationUrl(
        jiraUrl: string = '',
        application: string = '',
        staticTabChangeUrl = false
    ): Promise<JiraAuthUrl> {
        let url = `/api/auth/url?application=${application}`;

        if (jiraUrl && jiraUrl !== 'null' && jiraUrl !== 'undefined') {
            url += `&jiraUrl=${jiraUrl}`;
        }

        if (staticTabChangeUrl) {
            url += `&staticTabChangeUrl=${staticTabChangeUrl}`;
        }

        return (this.http.get<JiraAuthUrl>(url).toPromise() as any);
    }
}
