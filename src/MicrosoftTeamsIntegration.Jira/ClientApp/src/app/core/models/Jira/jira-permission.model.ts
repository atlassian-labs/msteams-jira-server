
export declare type JiraCommentPermissionName = 'ADD_COMMENTS' | 'EDIT_ALL_COMMENTS' |
    'EDIT_OWN_COMMENTS' | 'DELETE_ALL_COMMENTS' | 'DELETE_OWN_COMMENTS';

export declare type JiraIssuePermissionName = 'ASSIGNABLE_USER' | 'ASSIGN_ISSUES' | 'CLOSE_ISSUES' |
    'CREATE_ISSUES' | 'DELETE_ISSUES' | 'EDIT_ISSUES' | 'LINK_ISSUES' | 'MODIFY_REPORTER' | 'MOVE_ISSUES' |
    'RESOLVE_ISSUES' | 'SCHEDULE_ISSUES' | 'SET_ISSUE_SECURITY' | 'TRANSITION_ISSUES' | 'BROWSE';

export declare type JiraPermissionName = JiraCommentPermissionName | JiraIssuePermissionName;

export interface JiraPermission {
    id: string;
    key: string;
    name: string;
    type: string;
    description: string;
    havePermission: boolean;
}

export type JiraPermissions = { [permissionName in JiraPermissionName]: JiraPermission };

export interface JiraPermissionsResponse {
    permissions: JiraPermissions;
}

