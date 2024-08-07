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
        return this.http
            .get<JiraIssuesSearch>(url)
            .toPromise();
    }

    public getProjects(jiraUrl: string, getAvatars: boolean = false): Promise<Project[]> {
        return this.http
            .get<Project[]>(`/api/projects-all?jiraUrl=${jiraUrl}&getAvatars=${getAvatars}`)
            .toPromise();
    }

    public getProject(jiraUrl: string, projectKey: string): Promise<Project> {
        return this.http
            .get<Project>(`/api/project?jiraUrl=${jiraUrl}&projectKey=${projectKey}`)
            .toPromise();
    }

    public findProjects(jiraUrl: string, filterName: string = '', getAvatars: boolean = false): Promise<Project[]> {
        return this.http
            .get<Project[]>(`/api/projects-search?jiraUrl=${jiraUrl}&filterName=${filterName}&getAvatars=${getAvatars}`)
            .toPromise();
    }

    public getPriorities(jiraUrl: string): Promise<Priority[]> {
        return this.http
            .get<Priority[]>(`/api/priorities?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getTypes(jiraUrl: string): Promise<IssueType[]> {
        return this.http
            .get<IssueType[]>(`/api/types?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getStatuses(jiraUrl: string): Promise<IssueStatus[]> {
        return this.http
            .get<IssueStatus[]>(`/api/statuses?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getStatusesByProject(jiraUrl: string, projectIdOrKey: string): Promise<IssueStatus[]> {
        return this.http
            .get<IssueStatus[]>(`/api/project-statuses?jiraUrl=${jiraUrl}&projectIdOrKey=${projectIdOrKey}`)
            .toPromise();
    }

    public getSavedFilters(jiraUrl: string): Promise<Filter[]> {
        return this.http
            .get<Filter[]>(`/api/filters?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public searchSavedFilters(jiraUrl: string, filterName: string = ''): Promise<Filter[]> {
        return this.http
            .get<Filter[]>(`/api/filters-search?jiraUrl=${jiraUrl}&filterName=${filterName}`)
            .toPromise();
    }

    public getFavouriteFilters(jiraUrl: string): Promise<Filter[]> {
        return this.http
            .get<Filter[]>(`/api/favourite-filters?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getFilter(jiraUrl: string, filterId: string): Promise<Filter> {
        return this.http
            .get<Filter>(`/api/filter?jiraUrl=${jiraUrl}&filterId=${filterId}`)
            .toPromise();
    }

    public getAddonStatus(jiraUrl: string): Promise<JiraAddonStatus> {
        return this.http
            .get<JiraAddonStatus>(`/api/addon-status?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getMyselfData(jiraUrl: string): Promise<MyselfInfo> {
        return this.http
            .get<MyselfInfo>(`/api/myself?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getCurrentUserData(jiraUrl: string): Promise<CurrentJiraUser> {
        return this.http
            .get<CurrentJiraUser>(`/api/myself/?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public async getJiraUrlForPersonalScope(): Promise<JiraUrlData> {
        return this.http
            .get<JiraUrlData>('/api/personalScope/url')
            .toPromise();
    }

    public getJiraTenantInfo(jiraUrl: string): Promise<JiraTenantInfo> {
        return this.http
            .get<JiraTenantInfo>(`/api/tenant-info/?jiraUrl=${jiraUrl}`)
            .toPromise();
    }

    public getIssueByIdOrKey(jiraUrl: string, issueIdOrKey: string): Promise<Issue> {
        return this.http
            .get<Issue>(`/api/issue?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`)
            .toPromise();
    }

    public getIssueTypeFieldsByProjectAndIssueIdOrKey(jiraUrl: string, projectKey: string, issueIdOrKey: string): Promise<any> {
        const link = this.utilService.appendParamsToLink('/api/issue/fields', { jiraUrl, projectKey, issueIdOrKey });

        return this.http
            .get<string[]>(link)
            .toPromise();
    }

    public getCreateMetaIssueTypes(jiraUrl: string, projectKeyOrId: string): Promise<CreateMeta.JiraIssueTypeMeta[]> {
        const link = this.utilService.appendParamsToLink('/api/issue/createmeta/issuetypes', { jiraUrl, projectKeyOrId });

        return this.http
            .get<CreateMeta.JiraIssueTypeMeta[]>(link)
            .toPromise();
    }

    public getCreateMetaFields(jiraUrl: string, projectKeyOrId: string, issueTypeId: string, issueTypeName: string):
    Promise<JiraIssueFieldMeta<any>[]> {
        const link =
            this.utilService.appendParamsToLink('/api/issue/createmeta/fields',
                { jiraUrl, projectKeyOrId, issueTypeId, issueTypeName });

        return this.http
            .get<JiraIssueFieldMeta<any>[]>(link)
            .toPromise();
    }

    public getEditIssueMetadata(jiraUrl: string, issueIdOrKey: string): Promise<EditIssueMetadata> {
        const link = this.utilService.appendParamsToLink('/api/issue/editmeta', { jiraUrl, issueIdOrKey });

        return this.http
            .get<EditIssueMetadata>(link)
            .toPromise();
    }

    public createIssue(jiraUrl: string, createIssueModel: IssueCreateOptions): Promise<JiraApiActionCallResponseWithContent<Issue>> {
        return this.http
            .post<JiraApiActionCallResponse>(`/api/issue?jiraUrl=${jiraUrl}`, createIssueModel)
            .toPromise();
    }

    public updateIssue(jiraUrl: string, issueIdOrKey: string, editIssueModel: Partial<IssueFields>): Promise<JiraApiActionCallResponse> {
        return this.http
            .put<JiraApiActionCallResponse>(`/api/issue?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`, editIssueModel)
            .toPromise();
    }

    public updateIssueDescription(jiraUrl: string, issueIdOrKey: string, description: string): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/description', { jiraUrl, issueIdOrKey });

        return this.http
            .put<JiraApiActionCallResponse>(link, { description })
            .toPromise();
    }

    public updateIssueSummary(jiraUrl: string, issueIdOrKey: string, summary: string): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/summary', { jiraUrl, issueIdOrKey, summary });

        return this.http
            .put<JiraApiActionCallResponse>(link, null)
            .toPromise();
    }

    public updatePriority(jiraUrl: string, issueIdOrKey: string, priority: Priority): Promise<JiraApiActionCallResponse> {
        const link = this.utilService.appendParamsToLink('/api/issue/updatePriority', { jiraUrl, issueIdOrKey });

        return this.http
            .put<JiraApiActionCallResponse>(link, priority)
            .toPromise();
    }

    public submitLoginInfo(jiraId: string, requestToken: string = '', verificationCode: string = ''):
    Promise<{ isSuccess: boolean; message: string }> {
        return this.http
            .post<any>('/api/submit-login-info', { atlasId: jiraId, verificationCode: verificationCode, requestToken: requestToken })
            .toPromise();
    }

    public logOut(jiraId: string): Promise<{isSuccess: boolean}> {
        return this.http
            .post<any>(`/api/logout?jiraId=${jiraId}`, {})
            .toPromise();
    }

    public searchUsers(jiraUrl: string, username: string = ''): Promise<JiraUser[]> {

        const link = this.utilService.appendParamsToLink('/api/user/search', { jiraUrl, username });

        return this.http
            .get<JiraUser[]>(link)
            .toPromise();
    }

    public getAutocompleteData(jiraUrl: string, fieldName: string = ''): Promise<JiraFieldAutocomplete[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/autocompletedata', { jiraUrl, fieldName });

        return this.http
            .get<JiraFieldAutocomplete[]>(link)
            .toPromise();
    }

    public getSprints(jiraUrl: string, projectKeyOrId: string): Promise<JiraIssueSprint[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/sprint', { jiraUrl, projectKeyOrId });

        return this.http
            .get<JiraIssueSprint[]>(link)
            .toPromise();
    }

    public getEpics(jiraUrl: string, projectKeyOrId: string): Promise<JiraIssueEpic[]> {

        const link = this.utilService.appendParamsToLink('/api/issue/epic', { jiraUrl, projectKeyOrId });

        return this.http
            .get<JiraIssueEpic[]>(link)
            .toPromise();
    }

    /* saves jira Data Center id for personal scope using
       returns jiraInstanceUrl */
    public saveJiraServerId(jiraServerId: string): Promise<{ isSuccess: boolean; message: string }> {
        return this.http
            .post<any>(`/api/save-jira-server-id?jiraServerId=${jiraServerId}`, {})
            .toPromise();
    }

    public validateConnection(jiraServerId: string): Promise<{ isSuccess: boolean }> {
        return this.http
            .get<any>(`/api/validate-connection?jiraServerId=${jiraServerId}`)
            .toPromise();
    }
}
