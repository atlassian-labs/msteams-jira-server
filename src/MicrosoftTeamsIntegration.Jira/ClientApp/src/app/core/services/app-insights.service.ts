
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';

import { logger } from '@core/services/logger.service';
import {IEventTelemetry, IExceptionTelemetry, IPageViewTelemetry} from '@microsoft/applicationinsights-common';

@Injectable({ providedIn: 'root' })
export class AppInsightsService {
    private appInsights: ApplicationInsights | undefined;

    constructor() {
        logger(
            'AppInsightsService::constructor setting up AppInsights'
        );

        try {
            const instrumentationKey = localStorage.getItem('instrumentationKey') as string;
            this.appInsights = new ApplicationInsights({
                config: {
                    connectionString: instrumentationKey
                }
            });
            this.appInsights.loadAppInsights();
        } catch (e) {
            logger('AppInsightsService::cannot set up AppInsights', e);
        }
    }

    public trackPageView(
        name: string,
        properties: object = {},
        uri?: string,
    ): void {
        logger('AppInsightsService::trackPageView', name);
        this.appInsights?.trackPageView({
            name,
            uri,
            properties: this.stringifyValuesOf(properties)
        } as IPageViewTelemetry);
    }

    public trackException(
        exception: Error,
        handledAt: string,
        errorDetails: object = {},
    ): void {
        logger('AppInsightsService::trackException: ', {
            exception,
            handledAt,
            errorDetails
        });
        this.appInsights?.trackException(
            {
                id: handledAt,
                exception,
                severityLevel: 3
            } as IExceptionTelemetry, {
                handledAt,
                ...this.stringifyValuesOf(errorDetails)
            }
        );
    }

    public trackEvent(
        name: string,
        properties?: object,
    ): void {
        this.appInsights?.trackEvent(
            {name} as IEventTelemetry,
            this.stringifyValuesOf(properties),
        );
    }

    public trackWarning(
        exception: Error,
        handledAt: string,
        properties: object = {}
    ): void {
        logger('AppInsightsService::trackWarning: ', {
            handledAt,
            exception,
            properties
        });
        this.appInsights?.trackException(
            {
                id: handledAt,
                exception,
                severityLevel: 2
            } as IExceptionTelemetry,
            properties
        );
    }

    public logNavigation(componentName: string, route: ActivatedRoute): void {
        this.trackPageView(componentName, {
            params: `${JSON.stringify(route.snapshot.params)}`
        });
    }

    private stringifyValuesOf(eventDetails: { [key: string]: any } = {}): { [name: string]: string } {
        const stringifierEventDetailsObj: { [key: string]: string } = {};
        for (const eventDetailsKey of Object.keys(eventDetails)) {
            stringifierEventDetailsObj[eventDetailsKey] = JSON.stringify(
                eventDetails[eventDetailsKey]
            );
        }
        return stringifierEventDetailsObj;
    }
}
