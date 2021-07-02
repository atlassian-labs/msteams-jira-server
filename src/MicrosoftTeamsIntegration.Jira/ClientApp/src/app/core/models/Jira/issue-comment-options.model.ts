export interface IssueUpdateCommentOptions {
    jiraUrl: string;
    issueIdOrKey: string;
    commentId: string;
    comment: string;
}

export interface IssueAddCommentOptions {
    jiraUrl: string;
    issueIdOrKey: string;
    comment: string;
    metadataRef: string;
}
