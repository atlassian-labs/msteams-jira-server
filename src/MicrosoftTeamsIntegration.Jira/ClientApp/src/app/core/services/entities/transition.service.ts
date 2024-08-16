import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JiraTransitionsResponse } from '@core/models/Jira/jira-transition.model';
import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';
import {firstValueFrom} from 'rxjs';

@Injectable({ providedIn: 'root' })
export class IssueTransitionService {

    constructor(
        private readonly http: HttpClient
    ) { }

    public getTransitions(jiraUrl: string, issueIdOrKey: string): Promise<JiraTransitionsResponse> {
        return firstValueFrom(this.http
            .get<JiraTransitionsResponse>(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`));
    }

    public doTransition(jiraUrl: string, issueIdOrKey: string, transitionId: string): Promise<JiraApiActionCallResponse> {
        return firstValueFrom(this.http
            .post<JiraApiActionCallResponse>(
            `/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`,
            {
                transition:
                    {
                        id: transitionId
                    }
            }
        ));
    }
}
