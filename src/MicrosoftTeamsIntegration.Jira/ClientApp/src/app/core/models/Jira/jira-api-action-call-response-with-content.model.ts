import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';

export interface JiraApiActionCallResponseWithContent<T> extends JiraApiActionCallResponse {
    content?: T;
}
