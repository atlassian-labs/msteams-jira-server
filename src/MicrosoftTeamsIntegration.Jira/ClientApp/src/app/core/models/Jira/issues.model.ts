import { NormalizedStatusField } from './issues.model';
import { Project } from '@core/models/Jira/project.model';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { JiraSlaField } from '@core/models/Jira/jira-sla-field.model';
import { JiraIconUrls } from '@core/models/Jira/jira-avatar-urls.model';

export interface Issue {
    expand: string;
    id: string;
    self: string;
    key: string;
    fields: IssueFields;
}

export interface IssueSatisfaction {
    rating: 0 | 1 | 2 | 3 | 4 | 5;
}

export interface IssueCustomFields {
    timeToResolution?: JiraSlaField;
    timeToFirstResponse?: JiraSlaField;
    timeToApproveNormalChange?: JiraSlaField;
    timeToCloseAfterResolution?: JiraSlaField;
    satisfaction?: IssueSatisfaction;
    requestType: IssueRequestType;
    impact?: IssueImpact;
    changeType?: any;
    changeCompletionDate?: any;
}

export interface IssueRequestType {
    requestType: IssueRequestTypeInner;
}

export interface IssueRequestTypeInner {
    id: string;
    name: string;
    description: string;
    helpTest: string;
    issueTypeId: string;
    groupIds: string[];
    icon: {
        id: string;
        _links: {
            iconUrls: JiraIconUrls;
        };
    };
}

export interface IssueFields extends IssueCustomFields {
    issuetype: IssueType;
    timespent?: any;
    project: Project;
    aggregatetimespent?: any;
    resolution?: JiraSlaField;
    resolutiondate?: any;
    workratio: number;
    lastViewed?: any;
    watches: Watches;
    created: string;
    priority: Priority;
    labels: string[];
    timeestimate?: any;
    aggregatetimeoriginalestimate?: any;
    versions: any[];
    issuelinks: any[];
    assignee: JiraUser;
    updated: string;
    status: IssueStatus;
    components: IssueComponent[];
    timeoriginalestimate?: any;
    description: string;
    timetracking?: any;
    security?: any;
    aggregatetimeestimate?: any;
    attachment: any[];
    summary: string;
    creator: JiraUser;
    subtasks: any[];
    reporter: JiraUser;
    aggregateprogress: Progress;
    environment?: any;
    duedate?: any;
    progress: Progress;
    comment: {
        comments: JiraComment[];
        maxResults: number;
        total: number;
        startAt: number;
    };
    votes: Votes;
    worklog?: any;
}

export interface JiraComment {
    self?: string;
    id: string;
    author: JiraUser;
    body: string;
    updateAuthor?: JiraUser;
    created: string;
    updated: string;
}

interface Progress {
    progress: number;
    total: number;
}

export interface Priority {
    id: string;
    iconUrl: string;
    name: string;
    statusColor: string;
    description?: string;
}


export interface IssueType {
    id: string;
    description: string;
    iconUrl: string;
    name: string;
    subtask?: boolean;
    avatarId?: number;
}

export interface IssueStatus {
    id: string;
    description: string;
    name: string;
    iconUrl: string;
    statusCategory?: StatusCategory;
}

export interface StatusCategory {
    id: number;
    key: string;
    colorName: string;
    name: string;
}

export interface IssueComponent {
    id: string;
    name: string;
    description: string;
}

interface Votes {
    self: string;
    votes: number;
    hasVoted: boolean;
}

interface Watches {
    self: string;
    watchCount: number;
    isWatching: boolean;
}

export interface IssueImpact {
    id: string;
    value: string;
}

export interface NormalizedIssue {
    id: string;

    /* used to contain issuetype.id */
    issuetype: string;
    issueTypeIconUrl: string;
    issueTypeDescription: string;
    issueTypeName: string;

    issuekey: string;
    issuekeySortString: string;
    summary: string;
    assignee: string;
    reporter: string;

    /* used to contain priority level */
    priority: number;
    priorityIconUrl: string;
    priorityName: string;

    status: NormalizedStatusField;
    statusCategory: string;

    resolution: string;

    created: string;
    updated: string;
    duedate?: string;

    keyId: number;
    impact?: string;
    satisfaction?: number;
    requestType?: string;

    // in milliseconds
    timeToResolution?: NormalizedSlaField;
    timeToFirstResponse?: NormalizedSlaField;
    timeToApproveNormalChange?: NormalizedSlaField;
    timeToCloseAfterResolution?: NormalizedSlaField;
    components?: string[];
    labels?: string[];
}

export interface NormalizedSlaField {
    // in milliseconds
    remainingTimeMillis: number;
    remainingTimeFriendly: string;
    iconClassesName?: string;
}

export interface SlaIconMapping {
    breached?: boolean;
    paused?: boolean;
    withinCalendarHours?: boolean;
    halfHourRemain?: boolean;
    class: string;
}

export interface NormalizedStatusField {
    id: number;
    name: string;
}

export interface IssueCreateOptions {
    fields: any;
    metadataRef: string;
}
