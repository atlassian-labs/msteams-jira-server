export interface NotificationSubscription {
    subscriptionId?: string;
    jiraId: string;
    subscriptionType: SubscriptionType;
    conversationId: string;
    conversationReference?: string;
    conversationReferenceId: string;
    eventTypes: string[];
    projectId: string;
    microsoftUserId: string;
    filter: string;
    isActive: boolean;
}

export enum SubscriptionType {
    Personal = 0,
    Channel = 1
}

export enum PersonalEventType {
    ActivityIssueCreator = 'ActivityIssueCreator',
    CommentIssueCreator = 'CommentIssueCreator',
    ActivityIssueAssignee = 'ActivityIssueAssignee',
    CommentIssueAssignee = 'CommentIssueAssignee',
    IssueViewer = 'IssueViewer',
    MentionedOnIssue = 'MentionedOnIssue',
    CommentViewer = 'CommentViewer'
}

export enum ChannelEventType {
    IssueCreated = 'IssueCreated',
    IssueUpdated = 'IssueUpdated',
    CommentCreated = 'CommentCreated',
    CommentUpdated = 'CommentUpdated'
}
