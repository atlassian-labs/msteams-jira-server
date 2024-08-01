
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';

import { logger } from '@core/services/logger.service';

@Injectable({ providedIn: 'root' })
export class AppInsightsService {

    constructor() {
        if (!AppInsights.config) {
            logger(
                'AppInsightsService::constructor downloading and setting up library'
            );

            const instrumentationKey = localStorage.getItem('instrumentationKey') as string;
            if (AppInsights?.downloadAndSetup) {
                AppInsights?.downloadAndSetup({ instrumentationKey });
            }
        }
    }

    public trackPageView(
        name: string,
        properties: object = {},
        url?: string,
        measurements?: { [name: string]: number },
        duration?: number
    ): void {
        logger('AppInsightsService::trackPageView', name);
        AppInsights.trackPageView(
            name,
            url,
            this.stringifyValuesOf(properties),
            measurements,
            duration
        );
    }

    public trackException(
        exception: Error,
        handledAt: string,
        errorDetails: object = {},
        measurements?: { [name: string]: number },
        severityLevel: number = 3
    ): void {
        logger('AppInsightsService::trackException: ', {
            exception,
            handledAt,
            errorDetails
        });
        AppInsights.trackException(
            exception,
            handledAt,
            {
                handledAt,
                ...this.stringifyValuesOf(errorDetails)
            },
            measurements,
            severityLevel
        );
    }

    public trackEvent(
        name: string,
        properties?: object,
        measurements?: { [name: string]: number }
    ): void {
        AppInsights.trackEvent(
            name,
            this.stringifyValuesOf(properties),
            measurements
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
        AppInsights.trackException(
            exception,
            handledAt,
            {
                ...this.stringifyValuesOf(properties),
                handledAt
            },
            undefined,
            2
        );
    }

    public logNavigation(componentName: string, route: ActivatedRoute): void {
        this.trackPageView(componentName, {
            params: `${JSON.stringify(route.snapshot.params)}`
        });
    }

    private stringifyValuesOf(eventDetails: { [key: string]: any } = {} = {}): { [name: string]: string } {
        const stringifiedEventDetailsObj: { [key: string]: string } = {};
        for (const eventDetailsKey of Object.keys(eventDetails)) {
            stringifiedEventDetailsObj[eventDetailsKey] = JSON.stringify(
                eventDetails[eventDetailsKey]
            );
        }
        return stringifiedEventDetailsObj;
    }
}
