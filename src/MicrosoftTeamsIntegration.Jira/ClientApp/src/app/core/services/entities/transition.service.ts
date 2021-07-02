import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JiraTransitionsResponse } from '@core/models/Jira/jira-transition.model';
import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';

@Injectable({ providedIn: 'root' })
export class IssueTransitionService {

    constructor(
        private readonly http: HttpClient
    ) { }

    public getTransitions(jiraUrl: string, issueIdOrKey: string): Promise<JiraTransitionsResponse> {
        return this.http
            .get<JiraTransitionsResponse>(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`)
            .toPromise();
    }

    public doTransition(jiraUrl: string, issueIdOrKey: string, transitionId: string): Promise<JiraApiActionCallResponse> {
        return this.http
            .post<JiraApiActionCallResponse>(
                `/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`,
                {
                    transition:
                    {
                        id: transitionId
                    }
                }
            ).toPromise();
    }
}
