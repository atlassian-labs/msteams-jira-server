import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { UtilService } from '@core/services/util.service';

import { JiraPermissionsResponse, JiraPermissionName } from '@core/models/Jira/jira-permission.model';

@Injectable({ providedIn: 'root' })
export class PermissionService {
    constructor(
        private readonly http: HttpClient,
        private readonly utilService: UtilService
    ) { }

    /**
	 *
	 * @param jiraUrl User's jiraUrl
	 * @param permissions comma separated string of values, or array of permissions,
     * e.g. 'ADD_COMMENT' or ['ADD_COMMENT', 'DELETE_COMMENT']
	 * @param issueIdOrKey - optional. if is used then then will be returned users' issue specific permissions
	 * @param projectId - optional. if is used then will be returned users' project specific permissions
	 */
    public getMyPermissions(
        jiraUrl: string,
        permissions: JiraPermissionName | JiraPermissionName[],
        issueId?: string,
        projectKey?: string
    ): Promise<JiraPermissionsResponse> {

        const finalPermissions = typeof permissions === 'string' ?
            permissions :
            permissions.join(',');

        const link = this.utilService.appendParamsToLink(
            '/api/mypermissions',
            { jiraUrl, permissions: finalPermissions, issueId, projectKey }
        );

        return this.http
            .get<JiraPermissionsResponse>(link)
            .toPromise() as any;
    }
}
