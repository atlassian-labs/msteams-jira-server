import { JiraIssueTypeFieldMetaSchema } from '@core/models/Jira/jira-issue-type-field-meta-schema.model';

export interface JiraIssueFieldMeta<T> {
    allowedValues: T[];
    defaultValue: T[];
    hasDefaultValue: boolean;
    key: string;
    name: string;
    operations: string[];
    required: boolean;
    schema: JiraIssueTypeFieldMetaSchema;
    fieldId: string;
}
