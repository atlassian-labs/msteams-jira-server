// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { Issue, NormalizedIssue, IssueCustomFields, NormalizedSlaField, SlaIconMapping } from '@core/models';
import { JqlOptions } from '@core/models/Jira/jql-settings.model';
import { JiraSlaField } from '@core/models/Jira/jira-sla-field.model';

@Injectable({ providedIn: 'root' })
export class IssuesService {

    public readonly CUSTOM_FIELDS_NAMES_MAP: { [fieldName: string]: keyof IssueCustomFields } = {
        'Time to resolution': 'timeToResolution',
        'Time to first response': 'timeToFirstResponse',
        'Request Type': 'requestType',
        'Satisfaction': 'satisfaction',
        'Impact': 'impact',
        'Change type': 'changeType',
        'Change completion date': 'changeCompletionDate',
        'Time to approve normal change': 'timeToApproveNormalChange',
        'Time to close after resolution': 'timeToCloseAfterResolution'
    };

    private readonly ALLOWED_PAGE_FILTERS: { [name: string]: string } = {
        IssuesAssigned: 'assignee=CURRENT_USER',
        IssuesReported: 'reporter=CURRENT_USER',
        IssuesWatched: 'assignee=CURRENT_USER OR watcher=CURRENT_USER'
    };

    /**
     * This are specific css classes from Jira ui library
     */
    private readonly JIRA_LOZENGES_CLASSES: { [name: string]: string } = {
        'medium-gray': '',
        'green': 'aui-lozenge-success',
        'yellow': 'aui-lozenge-current',
        'brown': 'aui-lozenge-moved',
        'warm-red': 'aui-lozenge-error',
        'blue-gray': 'aui-lozenge-complete'
    };

    private readonly FILTER_TYPE_TO_JQL_TRANSLATION_MAP: { [name: string]: string } = {
        'all-issues': 'order by created DESC',
        'open-issues': 'AND resolution = Unresolved order by priority DESC,updated DESC',
        'done-issues': 'AND statusCategory = Done order by updated DESC',
        'viewed-recently': 'AND issuekey in issueHistory() order by lastViewed DESC',
        'created-recently': 'AND created >= -1w order by created DESC',
        'resolved-recently': 'AND resolutiondate >= -1w order by updated DESC',
        'updated-recently': 'AND updated >= -1w order by updated DESC'
    };

    private readonly SLA_ICONS_CLASSES: {[name: string]: string} = {
        jiraIconsClasses: 'aui-icon aui-icon-small sla-status-icon',
        clockHalfHourRemain: 'aui-iconfont-ongoing sla-expired',
        clockBreached: 'aui-iconfont-ongoing sla-breached',
        clockWorkHours: 'aui-iconfont-ongoing sla-work-hours',

        pauseBreached: 'aui-iconfont-pause sla-breached',
        pausedNotBreached: 'aui-iconfont-pause',

        crossBreached: 'aui-iconfont-close-dialog sla-breached',
        succesArrow: 'aui-iconfont-success sla-success'
    };

    private readonly slaIconMappingOngoingCycle: SlaIconMapping[] = [
        {
            breached: true, paused: false, withinCalendarHours: true, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.clockBreached
        },
        {
            breached: false, paused: false, withinCalendarHours: true, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.clockWorkHours
        },
        {
            breached: false, paused: false, withinCalendarHours: true, halfHourRemain: true,
            class: this.SLA_ICONS_CLASSES.clockHalfHourRemain
        },
        {
            breached: false, paused: false, withinCalendarHours: false, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pausedNotBreached
        },
        {
            breached: false, paused: true, withinCalendarHours: true, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pausedNotBreached
        },
        {
            breached: false, paused: true, withinCalendarHours: true, halfHourRemain: true,
            class: this.SLA_ICONS_CLASSES.pausedNotBreached
        },
        {
            breached: false, paused: true, withinCalendarHours: false, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pausedNotBreached
        },
        {
            breached: true, paused: false, withinCalendarHours: false, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pauseBreached
        },
        {
            breached: true, paused: true, withinCalendarHours: false, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pauseBreached
        },
        {
            breached: true, paused: true, withinCalendarHours: true, halfHourRemain: false,
            class: this.SLA_ICONS_CLASSES.pauseBreached
        }
    ];

    private readonly statusIconMappingComplete: SlaIconMapping[] = [
        {
            breached: true, class: this.SLA_ICONS_CLASSES.crossBreached
        },
        {
            breached: false, class: this.SLA_ICONS_CLASSES.succesArrow
        }
    ];

    public normalizeIssues(
        issues: Issue[],
        prioritiesIdsInOrder?: string[]
    ): NormalizedIssue[] {
        if (!issues || !issues.length) {
            return [];
        }

        return issues.map((issue: Issue): NormalizedIssue => {
            const keyId = issue.key.split('-')[1];

            const keyPrefix = issue.key.split('-')[0];
            const keySuffix = Number(keyId) < 10 ? '0' + keyId : keyId;

            const { fields } = issue;


            const assigneeName = fields.assignee ? fields.assignee.displayName : 'Unassigned';
            const reporterName = fields.reporter ? fields.reporter.displayName : 'Unassigned';


            const priorityLevel = fields.priority && prioritiesIdsInOrder ? prioritiesIdsInOrder.indexOf(fields.priority.id) : 9999999;

            const normalizedIssue: NormalizedIssue = {
                id: issue.id,
                impact: fields.impact && fields.impact.value,
                issuetype: fields.issuetype && fields.issuetype.id,
                issueTypeIconUrl: fields.issuetype && fields.issuetype.iconUrl,
                issueTypeDescription: fields.issuetype &&
                    (fields.issuetype.name +
                        (fields.issuetype.description ? ` - ${fields.issuetype.description}` : '')
                    ),
                issueTypeName: fields.issuetype && fields.issuetype.name,
                issuekey: issue.key,
                issuekeySortString: keyPrefix + keySuffix,
                summary: fields.summary,
                assignee: assigneeName,
                reporter: reporterName,
                priority: priorityLevel,
                priorityIconUrl: fields.priority && fields.priority.iconUrl,
                priorityName: fields.priority && fields.priority.name,

                status: {
                    id: Number(fields.status.id),
                    name: fields.status && fields.status.name
                },
                statusCategory: fields.status && this.getStatusClass(fields.status.statusCategory.colorName),

                resolution: fields.resolution ? fields.resolution.name : 'Unresolved',
                created: fields.created,
                updated: fields.updated,
                duedate: fields.duedate,
                keyId: Number(keyId),
                components: fields.components && fields.components.map(c => c.name),
                requestType: fields.requestType && fields.requestType.requestType.name,
                satisfaction: fields.satisfaction && fields.satisfaction.rating,
                labels: fields.labels,
            };
            return normalizedIssue;
        });
    }

    /**
     * Maps custom field names to full field names and then maps full field names to our @interface Issue object field names
     * @param unmappedFieldsInOrder e.g. ['customfield_10021', 'issuetype']
     * @param fieldNamesMap e.g. { customfield_10021: 'Time to resolution', issuetype: 'Issue type' }
     * @returns e.g. ['timeToResolution', 'issueType']
     */
    public getDisplayedColumnsOrdered(
        unmappedFieldsInOrder: string[],
        fieldNamesMap: { [fieldName: string]: string }
    ): string[] {
        if (!unmappedFieldsInOrder || !unmappedFieldsInOrder.length) {
            return [];
        }

        return unmappedFieldsInOrder.map(unmappedFieldName => {
            if (!unmappedFieldName.startsWith('customfield_')) {
                return unmappedFieldName;
            }

            const fullFieldName = fieldNamesMap[unmappedFieldName];
            return this.CUSTOM_FIELDS_NAMES_MAP[fullFieldName];
        });
    }

    /**
     * @param issues issues with custom fields which may have custom fields
     * @param fieldNamesMap map of custom fields in format of object: { customfield_1231: 'Time to resolution', customfield_1222: 'Impact' }
     */
    public mapCustomFields(issues: Issue[], fieldNamesMap: { [fieldName: string]: string }): Issue[] {
        if (!issues || !issues.length) {
            return [];
        }

        const fieldNames = Object.keys(fieldNamesMap);

        return issues.map((issue: Issue) => {
            const newIssue = { ...issue }; // shallow cloning

            fieldNames.forEach((fieldName: string) => {
                if (!fieldName.startsWith('customfield_')) {
                    return;
                }

                const fieldInIssue = newIssue.fields[fieldName];
                const fullName = fieldNamesMap[fieldName];
                const mappedName = this.CUSTOM_FIELDS_NAMES_MAP[fullName];

                newIssue.fields[mappedName] = fieldInIssue;
                delete newIssue.fields[fieldName];
            });

            return newIssue;
        });
    }

    public getNormalizedSlaField(jiraField: JiraSlaField): NormalizedSlaField {
        const normalizedField: NormalizedSlaField = {
            remainingTimeMillis: this.getMillisecondsOfTimeToField(jiraField),
            remainingTimeFriendly: this.getFriendlyTimeOfTimeToField(jiraField),
            iconClassesName: this.resolveIconsInSlaFields(jiraField)
        };

        return normalizedField;
    }

    public resolveIconsInSlaFields(field: JiraSlaField): string {
        const millisInMinute = 60000;

        if (!field) {
            return undefined;
        }

        if (field.ongoingCycle) {
            const millis = field.ongoingCycle.remainingTime.millis;
            const halfHourRemain = millis > 0 && (millis / millisInMinute) <= 30 ? true : false;

            const result = this.slaIconMappingOngoingCycle.find( (rule) => rule.breached === field.ongoingCycle.breached &&
                rule.paused === field.ongoingCycle.paused &&
                rule.withinCalendarHours === field.ongoingCycle.withinCalendarHours &&
                rule.halfHourRemain === halfHourRemain);

            if (result) {
                return this.SLA_ICONS_CLASSES.jiraIconsClasses + ' ' + result.class;
            } else {
                return '';
            }
        }

        if (field.completedCycles.length) {
            const breached = field.completedCycles[field.completedCycles.length - 1].breached;
            const result = this.statusIconMappingComplete.find( (rule) => rule.breached === breached);
            if (result) {
                return this.SLA_ICONS_CLASSES.jiraIconsClasses + ' ' + result.class;
            } else {
                return '';
            }
        }

        return '';
    }

    /**
     * @returns formatted time for table from ongoingCycle.remainingTime or compeletedCycles[0].remainingTime fields of the timeTo field
     */
    public getFriendlyTimeOfTimeToField(timeToField: JiraSlaField): string | null | undefined {
        if (!timeToField) {
            return undefined;
        }

        if (timeToField.ongoingCycle) {
            const friendlyTime = timeToField.ongoingCycle.remainingTime.friendly;
            return this.formatFriendlyTimeToForTable(friendlyTime);
        }

        if (timeToField.completedCycles.length) {
            // get value of the last completed cycle
            const friendlyTime = timeToField.completedCycles[timeToField.completedCycles.length - 1].remainingTime.friendly;
            return this.formatFriendlyTimeToForTable(friendlyTime);
        }

        return null;
    }

    /**
     * @param time in following format '12h 1m' or '11h' or '-122h 32m' from a '.friendly' field
     * @returns time in following format '12:01' or '11:00' or '-122:32'
    */
    private formatFriendlyTimeToForTable(time: string): string {
        if (!time) {
            throw new Error('time is not defined');
        }

        let hours = '';
        let minutes = '';

        time = time.replace(' ', '');
        if (time.includes('h')) {
            // this will remove space between numbers and 'm' from minutes, if exists, and split them by 'h'
            [hours, minutes] = time.replace('m', '').split('h');
            hours = hours.replace(',', '');
        } else {
            minutes = time.replace('m', '');
        }

        hours = hours ? hours : '0';
        minutes = minutes ? (Number(minutes) < 10 ? '0' + minutes : minutes) : '00';

        return `${hours}:${minutes}`;
    }

    // to determine time issue status: whether enough time to
    // resolution/first response, expiring time to resolution/first response, breached time
    public getMillisecondsOfTimeToField(timeToField: JiraSlaField): number | undefined {
        if (!timeToField) {
            return undefined;
        }

        if (timeToField.ongoingCycle) {
            return timeToField.ongoingCycle.remainingTime.millis;
        }

        if (timeToField.completedCycles.length) {
            // get value of the last completed cycle
            return timeToField.completedCycles[timeToField.completedCycles.length - 1].remainingTime.millis;
        }

        return null;
    }

    public isCalendarHoursOfTimeToField(timeToField: JiraSlaField): boolean {
        if (!timeToField || !timeToField.ongoingCycle) {
            return false;
        }

        return timeToField.ongoingCycle.withinCalendarHours;
    }

    public isBreachedOfTimeToField(timeToField: JiraSlaField): boolean {
        if (!timeToField || !timeToField.ongoingCycle) {
            return false;
        }

        return timeToField.ongoingCycle.breached;
    }



    public getFiltersFromQuery(query: string): string {
        if (!query) {
            return '';
        }

        const filters = query.split(' AND ');
        const savedFilter = filters.map((x: string) => x.indexOf('filter')).toString();
        if (savedFilter === '0') {
            return savedFilter;
        }

        return filters
            // jql should be of the following format `type in ("Task", "Sub-task")`
            .map((jql: string) => {
                // get `("Task", "Sub-task")` from `type in ("Task", "Sub-task")`
                const match = jql.match(/\(.*\)/);

                if (!match) {
                    return '';
                }

                // we should got the following `"Task,Sub-task"` from the `("Task", "Sub-task")`
                const commaSeparatedValues = match[0].replace(/\(|\)|"/g, '');
                return commaSeparatedValues.split(',').join(', ');
            }).join(', ');
    }

    public createJqlQuery(options: JqlOptions): string {
        const { accountId, page, projectKey, jqlSuffix } = options;
        // if jql stays undefined instead of '', then following will occur: undefined + 'text' = 'undefinedtext'
        let { jql = '' } = options;

        let createdJql = '';
        if (projectKey) {
            if (jql.includes('project')) {
                console.warn('IssuesService::createJqlQuery:: JqlQuery already contains definition for a project property. ' +
                    `Jql: ${jql}. \nProjectKey prop: ${projectKey}.`);
            } else {
                createdJql = `project="${projectKey}" `;
            }
        }

        const isInTranslatedMap = this.FILTER_TYPE_TO_JQL_TRANSLATION_MAP.hasOwnProperty(jql);
        if (isInTranslatedMap) {
            createdJql += `${this.FILTER_TYPE_TO_JQL_TRANSLATION_MAP[jql]}`;
            return createdJql
                ? createdJql.replace(/\s+/g, ' ').trim()
                : '';
        }

        if (createdJql) {
            if (jql && !jql.trim().endsWith('AND')) {
                jql += ' AND ';
            }

            jql += createdJql;
        }

        // For static tabs only.
        if (page) {
            if (jql && this.ALLOWED_PAGE_FILTERS[page]) {
                jql += ' AND ';
            }
            jql = this.ALLOWED_PAGE_FILTERS[page] ?
                `${jql}(${this.ALLOWED_PAGE_FILTERS[page].replace(/CURRENT_USER/gi, 'currentUser()')}) `
                : jql;
        }

        if (!jql.toLowerCase().includes('order by')) {
            jql += ' order by created DESC';
        }

        // if jql contains 'order by' and we're making custom order by with jqlSuffix - rewrite order by in jql
        if (jql.toLowerCase().includes('order by') && (jqlSuffix && jqlSuffix.toLowerCase().includes('order by'))) {
            jql = jql.slice(0, jql.toLowerCase().indexOf('order by'));
        }

        jql += jqlSuffix ? ` ${jqlSuffix}` : '';

        return jql ? jql.replace(/\s+/g, ' ').trim() : '';
    }

    public createFullFilterJql(baseJqlQuery: string, filterJql: string): string {
        const additionalJql = filterJql.trim();
        const index = additionalJql.toLowerCase().indexOf('order by');

        let jqlBeforeOrderBy = additionalJql;
        let jqlAfterOrderBy = '';

        if (index !== -1) {
            jqlBeforeOrderBy = additionalJql.slice(0, index);
            jqlAfterOrderBy = additionalJql.slice(index);
        }

        // if jql starts from ORDER BY - do not add AND to the beginning of jql
        const jqlConjuction = index === 0 ? '' : 'AND';
        return `${baseJqlQuery} ${jqlConjuction} ${jqlBeforeOrderBy ? `(${jqlBeforeOrderBy.trim()})` : ''} ${jqlAfterOrderBy}`;
    }

    public adjustOrderByQueryStringWithIssueProperty(orderByQuery: string, propToBeReplaced: string, propToReplaceWith: string): string {
        if (!orderByQuery) {
            return '';
        }

        orderByQuery = orderByQuery.indexOf(propToReplaceWith) === -1 ?
            orderByQuery.replace(propToBeReplaced, propToReplaceWith) : orderByQuery;

        return orderByQuery;
    }

    private getStatusClass = (jiraClassName: string): string => this.JIRA_LOZENGES_CLASSES[jiraClassName] || '';
}
