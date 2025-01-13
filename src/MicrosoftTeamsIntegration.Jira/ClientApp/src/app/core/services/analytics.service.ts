import { Injectable } from '@angular/core';
import { UtilService } from '@core/services/util.service';
import { AppInsightsService } from '@core/services/app-insights.service';
import { logger } from '@core/services/logger.service';

const ANALYTICS_PRODUCT = 'jiraMsTeams';

export enum EventType {
    track = 'track',
    screen = 'screen',
    ui = 'ui'
}

export enum UiEventSubject {
    button = 'button',
    link = 'link',
    dropdown = 'dropdown',
    taskModule = 'taskModule'
}

export enum EventAction {
    viewed = 'viewed',
    clicked = 'clicked',
    selected = 'selected',
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
    analyticsEnvironment: string;
    msUserId: string | undefined;
    locale: string | undefined;

    constructor(
        private utilService: UtilService,
        private appInsightService: AppInsightsService
    ) {
        this.analyticsEnvironment = this.getAnalyticsEnvironment();
        this.msUserId =
            this.utilService && this.utilService.getMsTeamsContext()
                ? this.utilService.getMsTeamsContext().userObjectId
                : undefined;
        this.locale = this.utilService && this.utilService.getMsTeamsContext()
            ? this.utilService.getMsTeamsContext().locale
            : undefined;
    }

    public sendUiEvent(source: string,
        action: string,
        actionSubject: string,
        actionSubjectId: string,
        attributes: any = undefined): void {
        const properties =
            this.getEventObject(EventType.ui, source, action, actionSubject, actionSubjectId, '', attributes);

        const pageViewEventName =
            this.utilService.capitalizeFirstLetterAndJoin(actionSubjectId, actionSubject, action);

        this.appInsightService.trackPageView(pageViewEventName, properties);
    }

    public sendTrackEvent(source: string,
        action: string,
        actionSubject: string,
        actionSubjectId: string,
        attributes: any = undefined): void {
        const properties =
            this.getEventObject(EventType.track, source, action, actionSubject, actionSubjectId, '', attributes);

        const pageViewEventName =
            this.utilService.capitalizeFirstLetterAndJoin(actionSubjectId, actionSubject, action);

        this.appInsightService.trackPageView(pageViewEventName, properties);
    }

    public sendScreenEvent(source: string,
        action: string,
        actionSubject: string,
        name: string,
        attributes: any = undefined): void {

        const properties =
            this.getEventObject(EventType.track, source, action, actionSubject, '', name, attributes);

        const pageViewEventName =
            this.utilService.capitalizeFirstLetterAndJoin(name, actionSubject, action);

        this.appInsightService.trackPageView(pageViewEventName, properties);
    }

    private getEventObject(
        eventType: EventType,
        source: string,
        action: string,
        actionSubject: string,
        actionSubjectId: string,
        name: string,
        attributes: any = undefined): any {
        return {
            event: {
                analyticsEnv: this.analyticsEnvironment,
                common: {
                    anonymousId: this.msUserId,
                    timestamp: new Date(),
                    product: ANALYTICS_PRODUCT,
                    locale: this.locale
                },
                data: {
                    type: eventType,
                    source,
                    action,
                    actionSubject,
                    actionSubjectId,
                    name,
                    attributes
                }
            }
        };
    }

    private getAnalyticsEnvironment(): string {
        // returns a valid Atlassian analytics environment
        const analyticsEnvironment: string = localStorage.getItem('analyticsEnvironment') || 'dev';
        logger('*** analyticsEnvironment: ', analyticsEnvironment);
        const environments = ['local', 'dev', 'staging', 'prod'];

        if (environments.includes(analyticsEnvironment)) {
            return analyticsEnvironment;
        }

        // Defaulting to dev if environment not properly set
        logger('*** AnalyticsEnvironment is not set or not valid so using \'dev\'');
        return 'dev';
    }
}
