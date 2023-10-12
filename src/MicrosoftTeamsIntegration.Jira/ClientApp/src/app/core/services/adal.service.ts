// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';

import { UtilService } from '@core/services/util.service';
import { logger } from './logger.service';

// eslint-disable-next-line @typescript-eslint/naming-convention
declare const AuthenticationContext: any;

interface Settings {
    redirectUri: string;
    cacheLocation: string;
    navigateToLoginRequestUrl: boolean;
    instance: string;
    [key: string]: any;
}

@Injectable({ providedIn: 'root' })
export class AdalService {

    public settings: Settings = {
        instance: this.utilService.getAADInstance(),
        redirectUri: `https://${window.location.host}/loginResult.html`,
        cacheLocation: 'localStorage',
        navigateToLoginRequestUrl: false
    };

    private authContext: any;

    constructor(private readonly utilService: UtilService) { }

    public acquireToken(): Promise<string> {
        return new Promise(
            (resolve: (value: string) => void, reject) => {
                logger('AdalService::acquireToken is called');

                this.init();

                this.authContext.acquireToken(
                    this.settings.clientId,
                    (error: any, token: string) => {
                        logger(`AdalService::acquireToken result: error: ${error}, token: ${token}`);

                        if (!token || error) {
                            reject(error);
                        }

                        resolve(token);
                    });
            }
        );
    }

    public getCachedToken(): string {
        this.init();
        return this.authContext.getCachedToken(this.settings.clientId);
    }

    public renewToken(): Promise<string> {
        return new Promise(
            (resolve: (value: string) => void, reject) => {
                logger('AdalService::renewToken is called');

                this.init();
                this.authContext._renewIdToken((error: any, token: string) => {
                    logger(`AdalService::renewToken result: error: ${error}, token: ${token}`);

                    if (!token || error) {
                        reject(error);
                    }

                    resolve(token);
                });
            }
        );
    }

    private init(): void {
        const context = this.utilService.getMsTeamsContext();
        this.settings.clientId = this.utilService.getUserClientId();

        if (context && context.tid) {
            this.settings.tenant = context.tid;
        }

        this.settings.extraQueryParameters = 'scope=openid+profile';
        if (context && context.loginHint) {
            this.settings.extraQueryParameters += `&login_hint=${encodeURIComponent(context.loginHint)}`;
        }

        this.authContext = new AuthenticationContext(this.settings);
    }
}
