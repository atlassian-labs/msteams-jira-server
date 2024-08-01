import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UtilService } from '@core/services/util.service';

import { JiraUser } from '@core/models/Jira/jira-user.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { JiraApiActionCallResponseWithContent } from '@core/models/Jira/jira-api-action-call-response-with-content.model';
import { SearchAssignableOptions } from '@core/models/Jira/search-assignable-options';

@Injectable({
    providedIn: 'root'
})
export class AssigneeService {
    public readonly AUTOMATIC_ASSIGNEE_VALUE = '-1';
    public readonly UNASSIGNED_ASSIGNEE_VALUE = null;

    public readonly unassignedOption: DropDownOption<string> = {
        id: -2,
        value: this.UNASSIGNED_ASSIGNEE_VALUE,
        label: 'Unassigned',
        icon: '/assets/useravatar24x24.png'
    };

    public readonly automaticOption: DropDownOption<string> = {
        id: -1,
        value: this.AUTOMATIC_ASSIGNEE_VALUE,
        label: 'Automatic',
        icon: '/assets/useravatar24x24.png'
    };

    constructor(
        private readonly http: HttpClient,
        private readonly utilService: UtilService,
        private readonly dropdownUtilService: DropdownUtilService
    ) { }

    /**
     * @returns response with content of assigned user accountId or null if Unassigned.
     */
    public setAssignee(
        jiraUrl: string,
        issueIdOrKey: string,
        assigneeAccountIdOrName: string | null | '-1'
    ): Promise<JiraApiActionCallResponseWithContent<string | null>> {
        const link = this.utilService.appendParamsToLink('/api/issue/assignee', { jiraUrl, issueIdOrKey, assigneeAccountIdOrName });

        return this.http
            .put<JiraApiActionCallResponseWithContent<string | null>>(link, null)
            .toPromise() as any;
    }

    public searchAssignable(options: SearchAssignableOptions): Promise<JiraUser[]> {
        const link = this.utilService.appendParamsToLink('/api/issue/searchAssignable', options);

        return this.http.get<JiraUser[]>(link).toPromise() as any;
    }

    public searchAssignableMultiProject(jiraUrl: string, projectKey: string, username: string = ''): Promise<JiraUser[]> {
        const link = this.utilService.appendParamsToLink('/api/user/assignable/multiProjectSearch', { jiraUrl, projectKey, username });

        return this.http.get<JiraUser[]>(link).toPromise() as any;
    }

    public assigneesToDropdownOptions(assignees: JiraUser[], username: string = ''): DropDownOption<string>[] {
        const assigneeOptions = assignees ? assignees.map(this.dropdownUtilService.mapUserToDropdownOption) : [];

        if (!username) {
            return [this.unassignedOption, this.automaticOption, ...assigneeOptions];
        }

        username = username.toLowerCase();
        if ('unassigned'.startsWith(username)) {
            assigneeOptions.unshift(this.unassignedOption);
        }
        if ('automatic'.startsWith(username)) {
            assigneeOptions.unshift(this.automaticOption);
        }

        return assigneeOptions;
    }
}
