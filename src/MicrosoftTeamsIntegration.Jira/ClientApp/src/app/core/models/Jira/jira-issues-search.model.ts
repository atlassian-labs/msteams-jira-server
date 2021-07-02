import { Issue } from '@core/models/Jira/issues.model';

export interface JiraIssuesSearch {
    expand: string;
    startAt: number;
    maxResults: number;
    total: number;
    issues: Issue[];
    names?: { [fieldName: string]: string };
    fieldsInOrder?: string[];
    prioritiesIdsInOrder?: string[];
    errorMessages?: string[];
    pageSize: number;
}
