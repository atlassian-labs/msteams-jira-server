import { Injectable } from '@angular/core';

import { Priority, IssueType, Project, IssueStatus } from '@core/models';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { JiraTransition } from '@core/models/Jira/jira-transition.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';

@Injectable({ providedIn: 'root' })
export class DropdownUtilService {

    public mapPriorityToDropdownOption(priority: Priority): DropDownOption<string> {
        return {
            id: priority.id,
            label: priority.name,
            icon: priority.iconUrl,
            value: priority.id
        };
    }

    public mapAllowedValueToSelectOption(allowedValue: any): any {
        return {
            id: allowedValue.id,
            name: allowedValue.name ? allowedValue.name : allowedValue.value
        }
    }

    public mapAllowedValueWithChildrenToSelectOption(allowedValue: any): any {
        return {
            id: allowedValue.id,
            name: allowedValue.name ? allowedValue.name : allowedValue.value,
            children: allowedValue.children
        }
    }

    public mapDefaultValueToOption(defaultValue: any): any {
        return {
            id: defaultValue.id
        }
    }

    public mapAutocompleteDataToSelectOption(autocompleteData: any): any {
        return {
            id: autocompleteData.value,
            name: autocompleteData.displayName
        }
    }

    public mapSprintDataToSelectOption(sprint: any): any {
        return {
            id: sprint.id,
            name: sprint.name,
            state: sprint.state ? sprint.state.toUpperCase() : null
        }
    }

    public mapEpicDataToSelectOption(epic: any): any {
        return {
            id: epic.key,
            key: epic.key,
            name: epic.name,
            summary: epic.summary,
            color: epic.color ? epic.color.key : null
        }
    }

    /**
     * Map user.accountId to dropdown id and value, avatarUrls[24x24] to label and user.displayName to label
     * @param assigneeUser user to map
     */
    public mapUserToDropdownOption(assigneeUser: JiraUser): DropDownOption<string> {
        const value = assigneeUser.accountId ? assigneeUser.accountId : assigneeUser.name;
        return  {
            id: value,
            value,
            icon: assigneeUser.avatarUrls['24x24'],
            label: assigneeUser.displayName
        };
    }

    // Do not map iconUrl to icon property! because it contains value of jiraUrl
    public mapStatusToDropdownOption(status: IssueStatus): DropDownOption<string> {
        return {
            id: status.id,
            label: status.name,
            value: status.id,
        };
    }

    public mapIssueTypeToDropdownOption(type: IssueType): DropDownOption<string> {
        return {
            id: type.id,
            value: type.id,
            label: type.name,
            icon: type.iconUrl
        };
    }

    public mapProjectToDropdownOption(project: Project): DropDownOption<string> {
        return {
            id: project.id,
            value: project.id,
            label: project.name,
            icon: project.avatarUrls['24x24']
        };
    }

    public mapTransitionToDropdonwOption(transition: JiraTransition): DropDownOption<JiraTransition> {
        return {
            id: transition.id,
            value: transition,
            label: transition.name
        };
    }
}
