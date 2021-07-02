export interface EditIssueDialogData {
    jiraUrl: string;
    issueId: string;
    projectKey: string;
}

export interface CreateIssueDialogData {
    jiraUrl: string;
    description: string;
}

export interface CreateCommentDialogData {
    jiraUrl: string;
    jiraId: string;
    comment: string;
}

export interface ConfirmationDialogData {
    title: string;
    subtitle: string;
    buttonText: string;
    dialogType: DialogType;
}

export enum DialogType {
    SuccessSmall,
    SuccessLarge,
    ErrorSmall,
    ErrorLarge,
    WarningLarge,
    WarningSmall
}

