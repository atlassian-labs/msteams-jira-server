import { JiraIssueEpicColor } from "./jira-issue-epic-color.model";

export interface JiraIssueEpic {
    id: string;
    self: string;
    key: string;
    name: string;
    summary: string;
    done: string;
    color: JiraIssueEpicColor
}