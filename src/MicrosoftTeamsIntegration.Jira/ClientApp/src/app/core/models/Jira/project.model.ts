import { IssueType } from '@core/models/Jira/issues.model';
import { JiraIconUrls } from '@core/models/Jira/jira-avatar-urls.model';

export type ProjectTypeKey = 'software' | 'service_desk' | 'business';
export interface Project {
    id: string;
    key: string;
    name: string;
    projectTypeKey: ProjectTypeKey;
    issueTypes: IssueType[];
    avatarUrls: JiraIconUrls;
    simplified: boolean;
}
