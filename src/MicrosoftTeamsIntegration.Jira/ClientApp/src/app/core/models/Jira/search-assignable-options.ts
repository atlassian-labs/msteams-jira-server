export interface SearchAssignableOptions {
    jiraUrl: string;
    issueKey: string;
    projectKey: string;
    //TODO: remove if search will work only with accountId
    query: string;
    accountId?: string;
}
