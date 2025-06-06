// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {Injectable} from '@angular/core';
import * as microsoftTeams from '@microsoft/teams-js';
import {HostClientType} from '@microsoft/teams-js';
import {compare} from 'compare-versions';

interface PredefinedFilters {
    id: number;
    value: string;
    label: string;
}

type IconSize = 'small' | 'medium';

@Injectable({ providedIn: 'root' })
export class UtilService {
    private readonly PREDEFINED_FILTERS: PredefinedFilters[] = [
        { id: 0, value: 'all-issues', label: 'All issues' },
        { id: 1, value: 'open-issues', label: 'Open issues' },
        { id: 2, value: 'done-issues', label: 'Done issues' },
        { id: 3, value: 'viewed-recently', label: 'Viewed recently' },
        { id: 4, value: 'created-recently', label: 'Created recently' },
        { id: 5, value: 'resolved-recently', label: 'Resolved recently' },
        { id: 6, value: 'updated-recently', label: 'Updated recently' }
    ];

    private readonly UPGRADE_ADDON_MESSAGE
        = 'Please upgrade Jira Data Center for Microsoft Teams app on your Jira Data Center to perform projects search.';
    private readonly NOTIFICATIONS_UPGRADE_ADDON_MESSAGE
        = 'Please upgrade Jira Data Center for Microsoft Teams app on your Jira Data Center' +
        ' to receive notifications from Jira.';
    private readonly ADDON_VERSION = '2022.08.103';
    private readonly NOTIFICATIONS_ADDON_VERSION = '2025.05.13';

    public isMobile = async (): Promise<boolean> => {
        const context = await microsoftTeams.app.getContext();
        return context.app.host.clientType === HostClientType.ios ||
            context.app.host.clientType === HostClientType.android;
    };

    public getFilters = (): PredefinedFilters[] => this.PREDEFINED_FILTERS;

    public encode(value: string): string {
        if (value.match(/[!'()*]/)) {
            return encodeURIComponent(value).replace(/[!'()*]/g, c =>
                // Also encode !, ', (, ), and *
                `%${c.charCodeAt(0).toString(16)}`
            );
        }

        return value;
    }

    public getMsTeamsContext = (): { tid: string; loginHint: string; userObjectId: string; locale: string } =>
        JSON.parse(localStorage.getItem('msTeamsContext') as string);

    public setTeamsContext = (tenantId: string): void => localStorage.setItem('msTeamsContext', JSON.stringify({ tid: tenantId }));

    public getUserClientId = (): string => localStorage.getItem('userClientId') as string;

    public getAADInstance = (): string => {
        const microsoftLoginBaseUrl = localStorage.getItem('microsoftLoginBaseUrl');
        const baseUrl = microsoftLoginBaseUrl ? microsoftLoginBaseUrl : 'https://login.microsoftonline.com';
        return `${baseUrl}/`;
    };

    public convertStringToNull = (value: any) => value === 'null' || value === 'undefined' ? null : value;

    public capitalizeFirstLetter = (value: string) => String(value).charAt(0).toUpperCase() + String(value).slice(1);

    public capitalizeFirstLetterAndJoin = (...value: string[]) => value.map(this.capitalizeFirstLetter).join('');

    public getDefaultUserIcon(size: IconSize = 'small'): string {
        const iconSizeInPixels = size === 'small' ? '24x24' : '32x32';
        return `/assets/useravatar${iconSizeInPixels}.png`;
    }

    /**
     * Be careful when using this type of copy, beacuse it will not copy function properties
     */
    public jsonCopy = <T>(obj: any): T => JSON.parse(JSON.stringify(obj)) as T;

    /**
     * Be careful: if object property is present but in different position it will show false result.
     * e.g. jsonEqual({ prop1: 1, prop2: 2 }, { prop2: 2, prop1: 1}) => false;
     *
     * Also it will not track function properties.
     */
    public jsonEqual = (obj1: any, obj2: any): boolean => JSON.stringify(obj1) === JSON.stringify(obj2);

    /**
     * Appends params to specified link
     * @param link with or without params
     * @param paramMap
    */
    public appendParamsToLink(link: string, paramMap: any): string {
        // if link doesn't contain params - append '?'
        // else if there is already some params - append '&' if necessary
        link += link.indexOf('?') === -1 ? '?' :
            link.endsWith('&') ? '' : '&';

        Object.keys(paramMap).forEach(key => {
            // if value is not empty or not undefined - append it
            if (paramMap[key]) {
                link += `${key}=${paramMap[key]}&`;
            }
        });

        return encodeURI(link);
    }

    public getJiraServerId = (): string => localStorage.getItem('jiraServer.jiraId') as string;

    public getQueryParam(paramName: string, url?: string): string {
        try {
            const query = url ? new URL(url).search : window.location.search;
            const params = new URLSearchParams(query);
            return params.get(paramName) || '';
        } catch (error) {
            console.error('Error parsing query parameter:', error);
            return '';
        }
    }

    public isElectron = (): boolean => {
        const userAgent = navigator.userAgent.toLowerCase();
        return userAgent.indexOf('electron') > -1;
    };

    public getMinAddonVersion = (): string => this.ADDON_VERSION;

    public getMinAddonVersionForNotifications = (): string => this.NOTIFICATIONS_ADDON_VERSION;

    public getUpgradeAddonMessage = (): string => this.UPGRADE_ADDON_MESSAGE;
    public getUpgradeAddonMessageForNotifications = (): string => this.NOTIFICATIONS_UPGRADE_ADDON_MESSAGE;

    public isAddonUpdated
        = (addonVersion: string): boolean => compare(addonVersion, this.getMinAddonVersion(), '>=');
    public isAddonUpdatedToVersion
        = (addonVersion: string, minAddonVersion: string): boolean => compare(addonVersion, minAddonVersion, '>=');
}
