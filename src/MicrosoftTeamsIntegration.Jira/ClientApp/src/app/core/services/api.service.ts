// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { UtilService } from '@core/services/util.service';

import {
    Project,
    Priority,
    IssueType,
    Filter,
    JiraIssuesSearch,
    Issue,
    IssueCreateOptions
} from '@core/models';

import { CurrentJiraUser, JiraUser } from '@core/models/Jira/jira-user.model';
import { IssueFields, IssueStatus} from '@core/models/Jira/issues.model';
import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';
import { CreateMeta } from '@core/models/Jira/jira-issue-create-meta.model';
import { EditIssueMetadata } from '@core/models/Jira/jira-issue-edit-meta.model';
import { JiraApiActionCallResponseWithContent } from '@core/models/Jira/jira-api-action-call-response-with-content.model';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import { JiraFieldAutocomplete } from '@core/models/Jira/jira-field-autocomplete-data.model';
import { JiraIssueSprint } from '@core/models/Jira/jira-issue-sprint.model';
import { JiraIssueEpic } from '@core/models/Jira/jira-issue-epic.model';
import { firstValueFrom } from 'rxjs';
import { NotificationSubscription } from '@core/models/NotificationSubscription';

export interface JiraAddonStatus {
    addonStatus: number;
    addonVersion: string;
}

export interface MyselfInfo {
    displayName: string;
    accountId: string;
}

export interface JiraUrlData {
    jiraUrl: string;
}

export interface JiraTenantInfo {
    cloudId: string;
}

export interface JiraAuthUrlData {
    jiraAuthUrl: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
    constructor(
        private readonly http: HttpClient,
        private readonly utilService: UtilService
    ) { }

    public getIssues(jiraUrl: string, jqlQuery: string, startAt: number = 0): Promise<JiraIssuesSearch> {
        const url = `/api/search?jiraUrl=${jiraUrl}&startAt=${startAt}&jql=${encodeURIComponent(jqlQuery)}`;
        return firstValueFrom(this.http
            .get<JiraIssuesSearch>(url));
    }

    public getProjects(jiraUrl: string, getAvatars: boolean = false): Promise<Project[]> {
        return firstValueFrom(this.http
            .get<Project[]>(`/api/projects-all?jiraUrl=${jiraUrl}&getAvatars=${getAvatars}`));
    }

    public getProject(jiraUrl: string, projectKey: string): Promise<Project> {
        return firstValueFrom(this.http
            .get<Project>(`/api/project?jiraUrl=${jiraUrl}&projectKey=${projectKey}`));
    }

    public findProjects(jiraUrl: string, filterName: string = '', getAvatars: boolean = false): Promise<Project[]> {
        return firstValueFrom(this.http
            .get<Project[]>(`/api/projects-search?jiraUrl=${jiraUrl}&filterName=${filterName}&getAvatars=${getAvatars}`));
    }

    public getPriorities(jiraUrl: string): Promise<Priority[]> {
        return firstValueFrom(this.http
            .get<Priority[]>(`/api/priorities?jiraUrl=${jiraUrl}`));
    }

    public getTypes(jiraUrl: string): Promise<IssueType[]> {
        return firstValueFrom(this.http
            .get<IssueType[]>(`/api/types?jiraUrl=${jiraUrl}`));
    }

    public getStatuses(jiraUrl: string): Promise<IssueStatus[]> {
        return firstValueFrom(this.http
            .get<IssueStatus[]>(`/api/statuses?jiraUrl=${jiraUrl}`));
    }

    public getStatusesByProject(jiraUrl: string, projectIdOrKey: string): Promise<IssueStatus[]> {
        return firstValueFrom(this.http
            .get<IssueStatus[]>(`/api/project-statuses?jiraUrl=${jiraUrl}&projectIdOrKey=${projectIdOrKey}`));
    }

    public getSavedFilters(jiraUrl: string): Promise<Filter[]> {
        return firstValueFrom(this.http
            .get<Filter[]>(`/api/filters?jiraUrl=${jiraUrl}`));
    }

    public searchSavedFilters(jiraUrl: string, filterName: string = ''): Promise<Filter[]> {
        return firstValueFrom(this.http
            .get<Filter[]>(`/api/filters-search?jiraUrl=${jiraUrl}&filterName=${filterName}`));
    }

    public getFavouriteFilters(jiraUrl: string): Promise<Filter[]> {
        return firstValueFrom(this.http
            .get<Filter[]>(`/api/favourite-filters?jiraUrl=${jiraUrl}`));
    }

    public getFilter(jiraUrl: string, filterId: string): Promise<Filter> {
        return firstValueFrom(this.http
            .get<Filter>(`/api/filter?jiraUrl=${jiraUrl}&filterId=${filterId}`));
    }

    public getAddonStatus(jiraUrl: string): Promise<JiraAddonStatus> {
        return firstValueFrom(this.http
            .get<JiraAddonStatus>(`/api/addon-status?jiraUrl=${jiraUrl}`));
    }

    public getMyselfData(jiraUrl: string): Promise<MyselfInfo> {
        return firstValueFrom(this.http
            .get<MyselfInfo>(`/api/myself?jiraUrl=${jiraUrl}`));
    }

    public getCurrentUserData(jiraUrl: string): Promise<CurrentJiraUser> {
        return firstValueFrom(this.http
            .get<CurrentJiraUser>(`/api/myself/?jiraUrl=${jiraUrl}`));
    }

    public async getJiraUrlForPersonalScope(): Promise<JiraUrlData> {
        return firstValueFrom(this.http
            .get<JiraUrlData>('/api/personalScope/url'));
    }

    public getJiraTenantInfo(jiraUrl: string): Promise<JiraTenantInfo> {
        return firstValueFrom(this.http
            .get<JiraTenantInfo>(`/api/tenant-info/?jiraUrl=${jiraUrl}`));
    }

    public getIssueByIdOrKey(jiraUrl: string, issueIdOrKey: string): Promise<Issue> {
        return firstValueFrom(this.http
            .get<Issue>(`/api/issue?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`));
    }

    public getIssueTypeFieldsByProjectAndIssueIdOrKey(jiraUrl: string, projectKey: string, issueIdOrKey: string): Promise<any> {
        const link = this.utilService.appendParamsToLink('/api/issue/fields', { jiraUrl, projectKey, issueIdOrKey });

        return firstValueFrom(this.http
            .get<string[]>(link));
    }

    public getCreateMetaIssueTypes(jiraUrl: string, projectKeyOrId: string): Promise<CreateMeta.JiraIssueTypeMeta[]> {
        const link = this.utilService.appendParamsToLink('/api/issue/createmeta/issuetypes', { jiraUrl, projectKeyOrId });

        return firstValueFrom(this.http
            .get<CreateMeta.JiraIssueTypeMeta[]>(link));
    }

    public getCreateMetaFields(jiraUrl: string, projectKeyOrId: string, issueTypeId: string, issueTypeName: string):
    Promise<JiraIssueFieldMeta<any>[]> {
        const link =
            this.utilService.appendParamsToLink('/api/issue/createmeta/fields',
                { jiraUrl, projectKeyOrId, issueTypeId, issueTypeName });

        return firstValueFrom(this.http
            .get<JiraIssueFieldMeta<any>[]>(link));
    }

    public getEditIssueMetadata(jiraUrl: string, issueIdOrKey: string): Promise<EditIssueMetadata> {
        const link = this.utilService.appendParamsToLink('/api/issue/editmeta', { jiraUrl, issueIdOrKey });

        return firstValueFrom(this.http
            .get<EditIssueMetadata>(link));
    }

    public createIssue(jiraUrl: string, createIssueModel: IssueCreateOptions): Promise<JiraApiActionCallResponseWithContent<Issue>> {
        return firstValueFrom(this.http
            .post<JiraApiActionCallResponse>(`/api/issue?jiraUrl=${jiraUrl}`, createIssueModel));
    }

    public updateIssue(jiraUrl: string, issueIdOrKey: string, editIssueModel: Partial<IssueFields>): Promise<JiraApiActionCallResponse> {
        return firstValueFrom(this.http
            .put<JiraApiActionCallResponse>(`/api/issue?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`, editIssueModel));
    }

    public updateIssueDescription(jiraUrl: string, issueIdOrKey: string, description: string): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/description', { jiraUrl, issueIdOrKey });

        return firstValueFrom(this.http
            .put<JiraApiActionCallResponse>(link, { description }));
    }

    public updateIssueSummary(jiraUrl: string, issueIdOrKey: string, summary: string): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/summary', { jiraUrl, issueIdOrKey, summary });

        return firstValueFrom(this.http
            .put<JiraApiActionCallResponse>(link, null));
    }

    public updatePriority(jiraUrl: string, issueIdOrKey: string, priority: Priority): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/updatePriority', { jiraUrl, issueIdOrKey });

        return firstValueFrom(this.http
            .put<JiraApiActionCallResponse>(link, priority));
    }

    public submitLoginInfo(jiraId: string, requestToken: string = '', verificationCode: string = ''):
    Promise<{ isSuccess: boolean; message: string }> {
        return firstValueFrom(this.http
            .post<any>('/api/submit-login-info', { atlasId: jiraId, verificationCode: verificationCode, requestToken: requestToken }));
    }

    public logOut(jiraId: string): Promise<{isSuccess: boolean}> {
        return firstValueFrom(this.http
            .post<any>(`/api/logout?jiraId=${jiraId}`, {}));
    }

    public searchUsers(jiraUrl: string, username: string = ''): Promise<JiraUser[]> {

        const link = this.utilService.appendParamsToLink('/api/user/search', { jiraUrl, username });

        return firstValueFrom(this.http
            .get<JiraUser[]>(link));
    }

    public getAutocompleteData(jiraUrl: string, fieldName: string = ''): Promise<JiraFieldAutocomplete[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/autocompletedata', { jiraUrl, fieldName });

        return firstValueFrom(this.http
            .get<JiraFieldAutocomplete[]>(link));
    }

    public getSprints(jiraUrl: string, projectKeyOrId: string): Promise<JiraIssueSprint[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/sprint', { jiraUrl, projectKeyOrId });

        return firstValueFrom(this.http
            .get<JiraIssueSprint[]>(link));
    }

    public getEpics(jiraUrl: string, projectKeyOrId: string): Promise<JiraIssueEpic[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/epic', { jiraUrl, projectKeyOrId });

        return firstValueFrom(this.http
            .get<JiraIssueEpic[]>(link));
    }

    /* saves jira Data Center id for personal scope using
       returns jiraInstanceUrl */
    public saveJiraServerId(jiraServerId: string): Promise<{ isSuccess: boolean; message: string }> {
        return firstValueFrom(this.http
            .post<any>(`/api/save-jira-server-id?jiraServerId=${jiraServerId}`, {}));
    }

    public validateConnection(jiraServerId: string): Promise<{ isSuccess: boolean }> {
        return firstValueFrom(this.http
            .get<any>(`/api/validate-connection?jiraServerId=${jiraServerId}`));
    }

    public async getJiraId(jiraBaseUrl: string | undefined): Promise<string> {
        return firstValueFrom(this.http
            .get(`/api/getJiraId?jiraUrl=${jiraBaseUrl}`, { responseType: 'text' }));
    }

    public addNotification(notification: NotificationSubscription): Promise<any> {
        return firstValueFrom(this.http.post('/api/notifications/add', notification));
    }

    public getNotificationSettings(microsoftUserId: string): Promise<NotificationSubscription> {
        return firstValueFrom(this.http.get<NotificationSubscription>(
            `/api/notifications/get?microsoftUserId=${microsoftUserId}`));
    }

    public updateNotification(notification: NotificationSubscription): Promise<any> {
        return firstValueFrom(this.http.put('/api/notifications/update', notification));
    }
}
