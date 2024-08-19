// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NgModule, APP_INITIALIZER } from '@angular/core';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

import { AuthGuard } from '@core/guards/auth.guard';
import { AuthInterceptor } from '@core/interceptors/auth.interceptor';
import { ErrorResponseInterceptor } from '@core/interceptors/error-response.interceptor';

import { AppLoadService } from '@core/services/app-load.service';

export function getSettingsFactory(appLoadService: AppLoadService): Function {
    return () => appLoadService.getSettings();
}

@NgModule(
    { imports: [], providers: [
        AuthGuard,
        {
            provide: APP_INITIALIZER,
            useFactory: getSettingsFactory,
            deps: [AppLoadService],
            multi: true
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: ErrorResponseInterceptor,
            multi: true
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true
        },
        provideHttpClient(withInterceptorsFromDi())
    ] })
export class CoreModule { }
