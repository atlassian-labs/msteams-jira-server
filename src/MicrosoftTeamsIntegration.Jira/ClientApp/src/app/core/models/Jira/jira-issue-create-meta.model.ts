import { Project } from '@core/models/Jira/project.model';
import { IssueType } from '@core/models/Jira/issues.model';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';

export declare module CreateMeta {
    export interface JiraIssueCreateMeta {
        projects: JiraProjectMeta[];
    }

    export interface JiraProjectMeta extends Project {
        issueTypes: JiraIssueTypeMeta[];
    }

    export interface JiraIssueTypeMeta extends IssueType {
        fields: JiraIssueFieldMeta<any>[];
    }
}
