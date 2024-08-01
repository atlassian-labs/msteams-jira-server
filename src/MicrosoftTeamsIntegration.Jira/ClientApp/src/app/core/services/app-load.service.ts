// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { logger } from '@core/services/logger.service';
import {firstValueFrom} from 'rxjs';

interface Settings {
    clientId: string;
    instrumentationKey: string;
    baseUrl: string;
    analyticsEnvironment: string;
    version: string;
    microsoftLoginBaseUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AppLoadService {
    constructor(private httpClient: HttpClient) { }

    public getSettings(): Promise<void | Settings | any> {
        return firstValueFrom(this.httpClient
            .get('/api/app-settings'))
            .then((settings: Settings | any) => {
                logger('Settings from API: ', settings);
                localStorage.setItem('userClientId', settings.clientId);
                localStorage.setItem('instrumentationKey', settings.instrumentationKey);
                localStorage.setItem('baseUrl', settings.baseUrl);
                localStorage.setItem('analyticsEnvironment', settings.analyticsEnvironment);
                localStorage.setItem('version', settings.version);
                localStorage.setItem('microsoftLoginBaseUrl', settings.microsoftLoginBaseUrl);

                return settings;
            })
            .catch((error: any) => {
                logger('APP_INITIALIZER ERROR', error);
            });
    }
}
