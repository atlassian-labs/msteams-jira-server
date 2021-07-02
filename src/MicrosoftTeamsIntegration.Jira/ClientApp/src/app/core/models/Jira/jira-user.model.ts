import { JiraIconUrls } from '@core/models/Jira/jira-avatar-urls.model';

/**
 * Common user info, frequently used in different entities as de-normalized set of data
 */
export interface JiraUser {
    self: string;
    accountId: string;
    emailAddress: string;
    hashedEmailAddress: string;
    avatarUrls: JiraIconUrls;
    displayName: string;
    active: boolean;
    timeZone: string;
    name: string;
}

/**
 * Full user profile which can be gotten via https://your-domain.atlassian.net/rest/api/2/myself
 */
export interface CurrentJiraUser extends JiraUser {
    //  all groups, including nested groups, to which user belongs
    groups?: {
        size: number;
        items: any[];
    };

    // application roles defines to which application user has access
    applicationRoles?: {
        size: number;
        items: any[];
    };

    jiraServerInstanceUrl: string;
}

