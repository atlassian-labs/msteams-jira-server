// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {Inject, Injectable, DOCUMENT} from '@angular/core';
import {HttpClient} from '@angular/common/http';

import {logger} from '@core/services/logger.service';
import {AppLoadService} from '@core/services/app-load.service';
import {ErrorService} from '@core/services/error.service';
import {StatusCode} from '@core/enums';
import {TeamsService} from '@core/services/teams.service';

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

@Injectable({ providedIn: 'root' })
export class AuthService {
    constructor(
        @Inject(DOCUMENT) private document: Document,
        private readonly http: HttpClient,
        private errorService: ErrorService,
        private appService: AppLoadService,
        private teamsService: TeamsService
    ) { }

    public async isAuthenticated(): Promise<boolean> {
        return !!(await this.getToken());
    }

    public authenticateMicrosoftAccount(): Promise<void | string> {
        return new Promise((resolve, reject) => {
            this.appService.getSettings().then(async (settings: any) => {
                if (!settings) {
                    logger('Cannot get Microsoft authentication URL');
                    return Promise.resolve();
                }

                const microsoftAuthScopes = 'openid profile offline_access';
                const params = {
                    client_id: settings.clientId,
                    response_type: 'code',
                    response_mode: 'query',
                    scope: microsoftAuthScopes,
                    redirect_uri: `${window.location.origin}/loginResult.html`,
                };

                const loginBaseUrl = settings.microsoftLoginBaseUrl ?
                    settings.microsoftLoginBaseUrl :
                    'https://login.microsoftonline.com';

                const microsoftAuthUrl = `${loginBaseUrl}/common/oauth2/v2.0/authorize?${new URLSearchParams(params)}`;

                const authenticateParameters: AuthenticateParameters = {
                    url: microsoftAuthUrl,
                    width: 460,
                    height: 640,
                    successCallback: resolve,
                    failureCallback: reject
                };

                try {
                    await this.teamsService.authenticate(authenticateParameters);

                    // force redirect to calling page
                    window.history.back();
                } catch (e) {
                    // in general is true only for mobile
                    // as far as microsoftTeams.authentication.authenticate()
                    // can not be called from the inside of 'authorization' context,
                    // try to redirect to another page in the same frame
                    if (this.document?.location) {

                        this.document.location.href = microsoftAuthUrl;
                        return Promise.resolve();
                    }
                    return Promise.reject();
                }
                return Promise.resolve();
            });
        });
    }

    public authenticateJiraAccount(authorizationUrl: string) {
        return new Promise(async (resolve, reject) => {
            const authenticateParameters: AuthenticateParameters = {
                url: authorizationUrl,
                width: 800,
                height: 600,
                successCallback: resolve,
                failureCallback: reject
            };

            try {
                await this.teamsService.authenticate(authenticateParameters);
            } catch (e) {
                // in general is true only for mobile
                // as far as microsoftTeams.authentication.authenticate()
                // can not be called from the inside of 'authorization' context,
                // try to redirect to another page in the same frame
                if (this.document?.location) {

                    this.document.location.href = authorizationUrl;
                    return Promise.resolve();
                }
                return Promise.reject();
            }
        });
    }

    public async getToken(): Promise<string | any> {
        const CONSENT_ERROR_CODES = ['resourceRequiresConsent', 'resourceRequiresMfa', 'tokenRevoked'];

        try {
            const token = await this.teamsService.getAuthToken({ silent: true });
            logger('Got new token from SSO');
            return token;
        } catch (error: any) {
            logger('Error from getToken: ', error?.message);

            if(CONSENT_ERROR_CODES.includes(error?.message)) {
                this.errorService.goToLoginWithStatusCode(StatusCode.Unauthorized);
            } else {
                return undefined;
            }
        }
    }
}
