import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AssigneeService } from './assignee.service';
import { UtilService } from '@core/services/util.service';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { SearchAssignableOptions } from '@core/models/Jira/search-assignable-options';

describe('AssigneeService', () => {
    let service: AssigneeService;
    let httpMock: HttpTestingController;
    let utilService: UtilService;
    let dropdownUtilService: DropdownUtilService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [AssigneeService, UtilService, DropdownUtilService]
        });

        service = TestBed.inject(AssigneeService);
        httpMock = TestBed.inject(HttpTestingController);
        utilService = TestBed.inject(UtilService);
        dropdownUtilService = TestBed.inject(DropdownUtilService);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should set assignee', () => {
        const dummyResponse = { isSuccess: true, content: '12345' };
        const jiraUrl = 'http://example.com';
        const issueIdOrKey = 'TEST-1';
        const assigneeAccountIdOrName = '12345';

        service.setAssignee(jiraUrl, issueIdOrKey, assigneeAccountIdOrName).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock
            .expectOne(
                `/api/issue/assignee?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}&assigneeAccountIdOrName=${assigneeAccountIdOrName}&`
            );
        expect(req.request.method).toBe('PUT');
        req.flush(dummyResponse);
    });

    it('should search assignable users', () => {
        const dummyUsers: JiraUser[] = [
            {
                accountId: '12345',
                displayName: 'John Doe',
                emailAddress: 'john.doe@example.com',
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                self: '',
                hashedEmailAddress: '',
                active: false,
                timeZone: '',
                name: ''
            }
        ];

        const options: SearchAssignableOptions = {
            jiraUrl: 'http://example.com',
            projectKey: 'TEST',
            issueKey: 'TEST-1',
            query: ''
        };

        service.searchAssignable(options).then(data => {
            expect(data).toEqual(dummyUsers);
        });

        const req = httpMock
            .expectOne(
                `/api/issue/searchAssignable?jiraUrl=${options.jiraUrl}&projectKey=${options.projectKey}&issueKey=${options.issueKey}&`
            );
        expect(req.request.method).toBe('GET');
        req.flush(dummyUsers);
    });

    it('should search assignable users for multiple projects', () => {
        const dummyUsers: JiraUser[] = [
            {
                accountId: '12345',
                displayName: 'John Doe',
                emailAddress: 'john.doe@example.com',
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                self: '',
                hashedEmailAddress: '',
                active: false,
                timeZone: '',
                name: ''
            }
        ];

        const jiraUrl = 'http://example.com';
        const projectKey = 'TEST';
        const username = 'John';

        service.searchAssignableMultiProject(jiraUrl, projectKey, username).then(data => {
            expect(data).toEqual(dummyUsers);
        });

        const req =
            httpMock.expectOne(
                `/api/user/assignable/multiProjectSearch?jiraUrl=${jiraUrl}&projectKey=${projectKey}&username=${username}&`
            );
        expect(req.request.method).toBe('GET');
        req.flush(dummyUsers);
    });

    it('should convert assignees to dropdown options', () => {
        const dummyUsers: JiraUser[] = [
            {
                accountId: '12345',
                displayName: 'John Doe',
                emailAddress: 'john.doe@example.com',
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                self: '',
                hashedEmailAddress: '',
                active: false,
                timeZone: '',
                name: ''
            }
        ];

        spyOn(dropdownUtilService, 'mapUserToDropdownOption').and.callFake((user: JiraUser) => ({
            id: 1,
            value: user.accountId,
            label: user.displayName,
            icon: user.avatarUrls['24x24']
        }));

        const dropdownOptions = service.assigneesToDropdownOptions(dummyUsers);
        expect(dropdownOptions.length).toBe(3);
        expect(dropdownOptions[0].label).toBe('Unassigned');
        expect(dropdownOptions[1].label).toBe('Automatic');
        expect(dropdownOptions[2].label).toBe('John Doe');
    });
});
