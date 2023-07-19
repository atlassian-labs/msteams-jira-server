import { StatusCategory } from '@core/models/Jira/issues.model';
import { JiraFieldSchema } from '@core/models/Jira/jira-field-schema.model';

export interface JiraTransitionTo {
    self: string;
    description: string;
    iconUrl: string;
    name: string;
    id: string;
    statusCategory: StatusCategory;
}

export interface JiraTransitionField {
    required: boolean;
    schema: JiraFieldSchema;
    name: string;
    key: string;
    hasDefaultValue: boolean;
    operations: string[];
    allowedValues: string[];
    defaultValue: string;
}

export interface JiraTransitionFields {
    summary: JiraTransitionField;
    colour: JiraTransitionField;
}

export interface JiraTransitionsResponse {
    expand: string;
    transitions: JiraTransition[];
}

export interface JiraTransition {
    id: string;
    name: string;
    to: JiraTransitionTo;
    hasScreen: boolean;
    isGlobal: boolean;
    isInitial: boolean;
    isConditional: boolean;
    // Specify expand=transitions.fields parameter to retrieve the fields required for a transition together with their types.
    fields?: JiraTransitionFields;
}
