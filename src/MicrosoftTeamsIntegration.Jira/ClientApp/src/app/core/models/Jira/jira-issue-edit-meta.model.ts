import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import {Priority} from '@core/models';

export interface EditIssueMetadata {
    fields: EditIssueMetadataFields;
}

export interface EditIssueMetadataFields {
    assignee: JiraIssueFieldMeta<string>;
    attachment: JiraIssueFieldMeta<string>;
    comment: JiraIssueFieldMeta<null>;
    components: JiraIssueFieldMeta<string>;
    description: JiraIssueFieldMeta<string>;
    issuelinks: JiraIssueFieldMeta<string>;
    issuetype: JiraIssueFieldMeta<string>;
    labels: JiraIssueFieldMeta<string>;
    priority: JiraIssueFieldMeta<Priority>;
    status: JiraIssueFieldMeta<string>;
    summary: JiraIssueFieldMeta<string>;
}
