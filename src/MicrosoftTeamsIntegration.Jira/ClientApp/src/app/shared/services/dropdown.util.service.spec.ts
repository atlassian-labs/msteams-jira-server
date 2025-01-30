import { TestBed } from '@angular/core/testing';
import { DropdownUtilService } from './dropdown.util.service';
import { Priority, IssueType, Project, IssueStatus } from '@core/models';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { JiraTransition, JiraTransitionTo } from '@core/models/Jira/jira-transition.model';

describe('DropdownUtilService', () => {
    let service: DropdownUtilService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(DropdownUtilService);
    });

    it('should map priority to dropdown option', () => {
        const priority: Priority = {
            id: '1',
            name: 'High',
            iconUrl: 'icon-url',
            statusColor: ''
        };
        const result = service.mapPriorityToDropdownOption(priority);
        expect(result).toEqual({
            id: '1',
            label: 'High',
            icon: 'icon-url',
            value: '1'
        });
    });

    it('should map user to dropdown option', () => {
        const user: JiraUser = {
            accountId: '123', name: 'user1', displayName: 'User One', avatarUrls: {
                '24x24': 'avatar-url',
                '16x16': '',
                '32x32': '',
                '48x48': ''
            },
            self: '',
            emailAddress: '',
            hashedEmailAddress: '',
            active: false,
            timeZone: ''
        };
        const result = service.mapUserToDropdownOption(user);
        expect(result).toEqual({
            id: '123',
            value: '123',
            icon: 'avatar-url',
            label: 'User One'
        });
    });

    it('should map status to dropdown option', () => {
        const status: IssueStatus = {
            id: '1', name: 'Open',
            description: '',
            iconUrl: ''
        };
        const result = service.mapStatusToDropdownOption(status);
        expect(result).toEqual({
            id: '1',
            label: 'Open',
            value: '1'
        });
    });

    it('should map issue type to dropdown option', () => {
        const issueType: IssueType = {
            id: '1', name: 'Bug', iconUrl: 'icon-url',
            description: ''
        };
        const result = service.mapIssueTypeToDropdownOption(issueType);
        expect(result).toEqual({
            id: '1',
            value: '1',
            label: 'Bug',
            icon: 'icon-url'
        });
    });

    it('should map project to dropdown option', () => {
        const project: Project = {
            id: '1', name: 'Project One', avatarUrls: {
                '24x24': 'avatar-url',
                '16x16': '',
                '32x32': '',
                '48x48': ''
            },
            key: '',
            projectTypeKey: 'software',
            issueTypes: [],
            simplified: false
        };
        const result = service.mapProjectToDropdownOption(project);
        expect(result).toEqual({
            id: '1',
            value: '1',
            label: 'Project One',
            icon: 'avatar-url'
        });
    });

    it('should map transition to dropdown option', () => {
        const transition: JiraTransition = {
            id: '1',
            name: 'Transition One',
            to: {} as JiraTransitionTo,
            hasScreen: false,
            isGlobal: false,
            isInitial: false,
            isConditional: false
        };
        const result = service.mapTransitionToDropdownOption(transition);
        expect(result).toEqual({
            id: '1',
            value: transition,
            label: 'Transition One'
        });
    });

    it('should map allowed value to select option', () => {
        const allowedValue = {
            id: '1',
            name: 'Allowed Value'
        };
        const result = service.mapAllowedValueToSelectOption(allowedValue);
        expect(result).toEqual({
            id: '1',
            name: 'Allowed Value'
        });
    });

    it('should map allowed value without name to select option', () => {
        const allowedValue = {
            id: '2',
            value: 'Allowed Value 2'
        };
        const result = service.mapAllowedValueToSelectOption(allowedValue);
        expect(result).toEqual({
            id: '2',
            name: 'Allowed Value 2'
        });
    });

    it('should map allowed value with children to select option', () => {
        const allowedValue = {
            id: '1',
            name: 'Allowed Value',
            children: [
                { id: '2', name: 'Child Value 1' },
                { id: '3', name: 'Child Value 2' }
            ]
        };
        const result = service.mapAllowedValueWithChildrenToSelectOption(allowedValue);
        expect(result).toEqual({
            id: '1',
            name: 'Allowed Value',
            children: [
                { id: '2', name: 'Child Value 1' },
                { id: '3', name: 'Child Value 2' }
            ]
        });
    });

    it('should map default value to option', () => {
        const defaultValue = { id: '1' };
        const result = service.mapDefaultValueToOption(defaultValue);
        expect(result).toEqual({ id: '1' });
    });

    it('should map autocomplete data to select option', () => {
        const autocompleteData = {
            value: '1',
            displayName: 'Autocomplete Value'
        };
        const result = service.mapAutocompleteDataToSelectOption(autocompleteData);
        expect(result).toEqual({
            id: '1',
            name: 'Autocomplete Value'
        });
    });

    it('should map sprint data to select option', () => {
        const sprint = {
            id: '1',
            name: 'Sprint 1',
            state: 'active'
        };
        const result = service.mapSprintDataToSelectOption(sprint);
        expect(result).toEqual({
            id: '1',
            name: 'Sprint 1',
            state: 'ACTIVE'
        });
    });

    it('should map epic data to select option', () => {
        const epic = {
            key: 'EPIC-1',
            name: 'Epic 1',
            summary: 'Epic Summary',
            color: { key: 'color-key' }
        };

        const result = service.mapEpicDataToSelectOption(epic);
        expect(result).toEqual({
            id: 'EPIC-1',
            key: 'EPIC-1',
            name: 'Epic 1',
            summary: 'Epic Summary',
            color: 'color-key'
        });
    });
});
