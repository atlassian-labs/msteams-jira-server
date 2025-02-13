import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiService, JiraAddonStatus, JiraTenantInfo, JiraUrlData, MyselfInfo } from './api.service';
import { UtilService } from './util.service';
import { Project, Priority, IssueType, Filter, JiraIssuesSearch, Issue, IssueCreateOptions } from '@core/models';
import { CurrentJiraUser, JiraUser } from '@core/models/Jira/jira-user.model';
import { IssueFields, IssueStatus } from '@core/models/Jira/issues.model';
import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';
import { CreateMeta } from '@core/models/Jira/jira-issue-create-meta.model';
import { EditIssueMetadata } from '@core/models/Jira/jira-issue-edit-meta.model';
import { JiraApiActionCallResponseWithContent } from '@core/models/Jira/jira-api-action-call-response-with-content.model';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import { JiraFieldAutocomplete } from '@core/models/Jira/jira-field-autocomplete-data.model';
import { of } from 'rxjs';
import { JiraIssueSprint } from '@core/models/Jira/jira-issue-sprint.model';
import { JiraIssueEpic } from '@core/models/Jira/jira-issue-epic.model';

describe('ApiService', () => {
    let service: ApiService;
    let httpMock: HttpTestingController;
    let utilService: UtilService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ApiService, UtilService]
        });
        service = TestBed.inject(ApiService);
        httpMock = TestBed.inject(HttpTestingController);
        utilService = TestBed.inject(UtilService);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should fetch issues as an Observable', () => {
        const dummyIssues: JiraIssuesSearch = {
            expand: '',
            startAt: 0,
            maxResults: 0,
            total: 0,
            issues: [],
            pageSize: 0
        };

        service.getIssues('http://example.com', 'project=TEST').then(data => {
            expect(data).toEqual(dummyIssues);
        });

        const req = httpMock.expectOne('/api/search?jiraUrl=http://example.com&startAt=0&jql=project%3DTEST');
        expect(req.request.method).toBe('GET');
        req.flush(dummyIssues);
    });

    it('should fetch projects as an Observable', () => {
        const dummyProjects: Project[] = [
            { id: '1', key: 'TEST', name: 'Test Project', projectTypeKey: 'software', issueTypes: [], simplified: false, avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            } }
        ];

        service.getProjects('http://example.com', true).then(data => {
            expect(data).toEqual(dummyProjects);
        });

        const req = httpMock.expectOne('/api/projects-all?jiraUrl=http://example.com&getAvatars=true');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProjects);
    });

    it('should fetch projects without avatars as an Observable', () => {
        const dummyProjects: Project[] = [
            { id: '1', key: 'TEST', name: 'Test Project', projectTypeKey: 'software', issueTypes: [], simplified: false, avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            } }
        ];

        service.getProjects('http://example.com').then(data => {
            expect(data).toEqual(dummyProjects);
        });

        const req = httpMock.expectOne('/api/projects-all?jiraUrl=http://example.com&getAvatars=false');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProjects);
    });

    it('should fetch a single project as an Observable', () => {
        const dummyProject: Project = {
            id: '1',
            key: 'TEST',
            name: 'Test Project',
            projectTypeKey: 'software',
            issueTypes: [],
            simplified: false,
            avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            }
        };

        service.getProject('http://example.com', 'TEST').then(data => {
            expect(data).toEqual(dummyProject);
        });

        const req = httpMock.expectOne('/api/project?jiraUrl=http://example.com&projectKey=TEST');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProject);
    });

    it('should find projects as an Observable', () => {
        const dummyProjects: Project[] = [
            { id: '1', key: 'TEST', name: 'Test Project', projectTypeKey: 'software', issueTypes: [], simplified: false, avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            } }
        ];

        service.findProjects('http://example.com', 'Test', true).then(data => {
            expect(data).toEqual(dummyProjects);
        });

        const req = httpMock.expectOne('/api/projects-search?jiraUrl=http://example.com&filterName=Test&getAvatars=true');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProjects);
    });

    it('should find projects without avatars as an Observable', () => {
        const dummyProjects: Project[] = [
            { id: '1', key: 'TEST', name: 'Test Project', projectTypeKey: 'software', issueTypes: [], simplified: false, avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            } }
        ];

        service.findProjects('http://example.com', 'Test').then(data => {
            expect(data).toEqual(dummyProjects);
        });

        const req = httpMock.expectOne('/api/projects-search?jiraUrl=http://example.com&filterName=Test&getAvatars=false');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProjects);
    });

    it('should find projects with empty filter name as an Observable', () => {
        const dummyProjects: Project[] = [
            { id: '1', key: 'TEST', name: 'Test Project', projectTypeKey: 'software', issueTypes: [], simplified: false, avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            } }
        ];

        service.findProjects('http://example.com').then(data => {
            expect(data).toEqual(dummyProjects);
        });

        const req = httpMock.expectOne('/api/projects-search?jiraUrl=http://example.com&filterName=&getAvatars=false');
        expect(req.request.method).toBe('GET');
        req.flush(dummyProjects);
    });

    it('should fetch priorities as an Observable', () => {
        const dummyPriorities: Priority[] = [
            {
                id: '1', name: 'High', description: 'High Priority', iconUrl: '',
                statusColor: ''
            },
            {
                id: '2', name: 'Medium', description: 'Medium Priority', iconUrl: '',
                statusColor: ''
            },
            {
                id: '3', name: 'Low', description: 'Low Priority', iconUrl: '',
                statusColor: ''
            }
        ];

        service.getPriorities('http://example.com').then(data => {
            expect(data).toEqual(dummyPriorities);
        });

        const req = httpMock.expectOne('/api/priorities?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyPriorities);
    });

    it('should fetch issue types as an Observable', () => {
        const dummyIssueTypes: IssueType[] = [
            { id: '1', name: 'Bug', description: 'A problem which impairs or prevents the functions of the product.', iconUrl: '' },
            { id: '2', name: 'Task', description: 'A task that needs to be done.', iconUrl: '' }
        ];

        service.getTypes('http://example.com').then(data => {
            expect(data).toEqual(dummyIssueTypes);
        });

        const req = httpMock.expectOne('/api/types?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyIssueTypes);
    });

    it('should fetch statuses as an Observable', () => {
        const dummyStatuses: IssueStatus[] = [
            {
                id: '1',
                name: 'To Do',
                description: 'To Do',
                iconUrl: '',
                statusCategory: {
                    id: 1,
                    key: 'new',
                    colorName: 'blue',
                    name: 'New'
                }
            },
            {
                id: '2',
                name: 'In Progress',
                description: 'In Progress',
                iconUrl: '',
                statusCategory: {
                    id: 2,
                    key: 'indeterminate',
                    colorName: 'yellow',
                    name: 'In Progress'
                }
            },
            {
                id: '3',
                name: 'Done',
                description: 'Done',
                iconUrl: '',
                statusCategory: {
                    id: 3,
                    key: 'done',
                    colorName: 'green',
                    name: 'Done'
                }
            }
        ];

        service.getStatuses('http://example.com').then(data => {
            expect(data).toEqual(dummyStatuses);
        });

        const req = httpMock.expectOne('/api/statuses?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyStatuses);
    });

    it('should fetch statuses by project as an Observable', () => {
        const dummyStatuses: IssueStatus[] = [
            {
                id: '1',
                name: 'To Do',
                description: 'To Do',
                iconUrl: '',
                statusCategory: {
                    id: 1,
                    key: 'new',
                    colorName: 'blue',
                    name: 'New'
                }
            },
            {
                id: '2',
                name: 'In Progress',
                description: 'In Progress',
                iconUrl: '',
                statusCategory: {
                    id: 2,
                    key: 'indeterminate',
                    colorName: 'yellow',
                    name: 'In Progress'
                }
            },
            {
                id: '3',
                name: 'Done',
                description: 'Done',
                iconUrl: '',
                statusCategory: {
                    id: 3,
                    key: 'done',
                    colorName: 'green',
                    name: 'Done'
                }
            }
        ];

        service.getStatusesByProject('http://example.com', 'TEST').then(data => {
            expect(data).toEqual(dummyStatuses);
        });

        const req = httpMock.expectOne('/api/project-statuses?jiraUrl=http://example.com&projectIdOrKey=TEST');
        expect(req.request.method).toBe('GET');
        req.flush(dummyStatuses);
    });

    it('should fetch saved filters as an Observable', () => {
        const dummyFilters: Filter[] = [
            {
                id: '1',
                name: 'Filter 1',
                jql: 'project = TEST',
                description: 'Filter 1 description'
            },
            {
                id: '2',
                name: 'Filter 2',
                jql: 'project = TEST AND status = "In Progress"',
                description: 'Filter 2 description'
            }
        ];

        service.getSavedFilters('http://example.com').then(data => {
            expect(data).toEqual(dummyFilters);
        });

        const req = httpMock.expectOne('/api/filters?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFilters);
    });

    it('should search saved filters as an Observable', () => {
        const dummyFilters: Filter[] = [
            {
                id: '1',
                name: 'Filter 1',
                jql: 'project = TEST',
                description: 'Filter 1 description'
            },
            {
                id: '2',
                name: 'Filter 2',
                jql: 'project = TEST AND status = "In Progress"',
                description: 'Filter 2 description'
            }
        ];

        service.searchSavedFilters('http://example.com', 'Filter').then(data => {
            expect(data).toEqual(dummyFilters);
        });

        const req = httpMock.expectOne('/api/filters-search?jiraUrl=http://example.com&filterName=Filter');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFilters);
    });

    it('should search saved filters with empty filter name as an Observable', () => {
        const dummyFilters: Filter[] = [
            {
                id: '1',
                name: 'Filter 1',
                jql: 'project = TEST',
                description: 'Filter 1 description'
            },
            {
                id: '2',
                name: 'Filter 2',
                jql: 'project = TEST AND status = "In Progress"',
                description: 'Filter 2 description'
            }
        ];

        service.searchSavedFilters('http://example.com').then(data => {
            expect(data).toEqual(dummyFilters);
        });

        const req = httpMock.expectOne('/api/filters-search?jiraUrl=http://example.com&filterName=');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFilters);
    });

    it('should fetch favourite filters as an Observable', () => {
        const dummyFilters: Filter[] = [
            {
                id: '1',
                name: 'Filter 1',
                jql: 'project = TEST',
                description: 'Filter 1 description'
            },
            {
                id: '2',
                name: 'Filter 2',
                jql: 'project = TEST AND status = "In Progress"',
                description: 'Filter 2 description'
            }
        ];

        service.getFavouriteFilters('http://example.com').then(data => {
            expect(data).toEqual(dummyFilters);
        });

        const req = httpMock.expectOne('/api/favourite-filters?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFilters);
    });

    it('should fetch a single filter as an Observable', () => {
        const dummyFilter: Filter = {
            id: '1',
            name: 'Filter 1',
            jql: 'project = TEST',
            description: 'Filter 1 description'
        };

        service.getFilter('http://example.com', '1').then(data => {
            expect(data).toEqual(dummyFilter);
        });

        const req = httpMock.expectOne('/api/filter?jiraUrl=http://example.com&filterId=1');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFilter);
    });

    it('should fetch addon status as an Observable', () => {
        const dummyAddonStatus: JiraAddonStatus = {
            addonStatus: 1,
            addonVersion: '1.0.0'
        };

        service.getAddonStatus('http://example.com').then(data => {
            expect(data).toEqual(dummyAddonStatus);
        });

        const req = httpMock.expectOne('/api/addon-status?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyAddonStatus);
    });

    it('should fetch myself data as an Observable', () => {
        const dummyMyselfInfo: MyselfInfo = {
            displayName: 'John Doe',
            accountId: '12345'
        };

        service.getMyselfData('http://example.com').then(data => {
            expect(data).toEqual(dummyMyselfInfo);
        });

        const req = httpMock.expectOne('/api/myself?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyMyselfInfo);
    });

    it('should fetch current user data as an Observable', () => {
        const dummyCurrentUser: CurrentJiraUser = {
            accountId: '12345',
            displayName: 'John Doe',
            emailAddress: 'john.doe@example.com',
            avatarUrls: {
                '16x16': '',
                '24x24': '',
                '32x32': '',
                '48x48': ''
            },
            jiraServerInstanceUrl: '',
            self: '',
            hashedEmailAddress: '',
            active: false,
            timeZone: '',
            name: ''
        };

        service.getCurrentUserData('http://example.com').then(data => {
            expect(data).toEqual(dummyCurrentUser);
        });

        const req = httpMock.expectOne('/api/myself/?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyCurrentUser);
    });

    it('should fetch Jira URL for personal scope as an Observable', () => {
        const dummyJiraUrlData: JiraUrlData = {
            jiraUrl: 'http://example.com'
        };

        service.getJiraUrlForPersonalScope().then(data => {
            expect(data).toEqual(dummyJiraUrlData);
        });

        const req = httpMock.expectOne('/api/personalScope/url');
        expect(req.request.method).toBe('GET');
        req.flush(dummyJiraUrlData);
    });

    it('should fetch Jira tenant info as an Observable', () => {
        const dummyTenantInfo: JiraTenantInfo = {
            cloudId: 'dummy-cloud-id'
        };

        service.getJiraTenantInfo('http://example.com').then(data => {
            expect(data).toEqual(dummyTenantInfo);
        });

        const req = httpMock.expectOne('/api/tenant-info/?jiraUrl=http://example.com');
        expect(req.request.method).toBe('GET');
        req.flush(dummyTenantInfo);
    });

    it('should fetch issue by ID or key as an Observable', () => {
        const dummyIssue: Issue = {
            id: '1',
            key: 'TEST-1',
            fields: {
                summary: 'Test Issue',
                description: 'This is a test issue',
                issuetype: {
                    id: '1',
                    name: 'Bug',
                    description: 'A problem which impairs or prevents the functions of the product.',
                    iconUrl: ''
                },
                priority: {
                    id: '1',
                    name: 'High',
                    description: 'High Priority',
                    iconUrl: '',
                    statusColor: ''
                },
                status: {
                    id: '1',
                    name: 'To Do',
                    description: 'To Do',
                    iconUrl: '',
                    statusCategory: {
                        id: 1,
                        key: 'new',
                        colorName: 'blue',
                        name: 'New'
                    }
                },
                project: {
                    id: '1',
                    key: 'TEST',
                    name: 'Test Project',
                    projectTypeKey: 'software',
                    issueTypes: [],
                    simplified: false,
                    avatarUrls: {
                        '16x16': '',
                        '24x24': '',
                        '32x32': '',
                        '48x48': ''
                    }
                },
                workratio: 0,
                watches: {
                    self: '',
                    watchCount: 0,
                    isWatching: false
                },
                created: '',
                labels: [],
                versions: [],
                issuelinks: [],
                assignee: {
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
                },
                updated: '',
                components: [],
                attachment: [],
                creator: {
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
                },
                subtasks: [],
                reporter: {
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
                },
                aggregateprogress: {
                    progress: 0,
                    total: 0
                },
                progress: {
                    progress: 0,
                    total: 0
                },
                comment: {
                    comments: [],
                    maxResults: 0,
                    total: 0,
                    startAt: 0
                },
                votes: {
                    self: '',
                    votes: 0,
                    hasVoted: false
                },
                editIssueMetadata: undefined,
                requestType: {
                    requestType: {
                        id: '1',
                        name: 'Service Request',
                        description: 'Service Request',
                        helpTest: '',
                        issueTypeId: '',
                        groupIds: [],
                        icon: {
                            id: '',
                            _links: {
                                iconUrls: {
                                    '16x16': '',
                                    '24x24': '',
                                    '32x32': '',
                                    '48x48': ''
                                }
                            }
                        }
                    }
                }
            },
            expand: '',
            self: ''
        };

        service.getIssueByIdOrKey('http://example.com', 'TEST-1').then(data => {
            expect(data).toEqual(dummyIssue);
        });

        const req = httpMock.expectOne('/api/issue?jiraUrl=http://example.com&issueIdOrKey=TEST-1');
        expect(req.request.method).toBe('GET');
        req.flush(dummyIssue);
    });

    it('should fetch issue type fields by project and issue ID or key as an Observable', () => {
        const dummyFields: string[] = ['field1', 'field2', 'field3'];

        service.getIssueTypeFieldsByProjectAndIssueIdOrKey('http://example.com', 'TEST', '1').then(data => {
            expect(data).toEqual(dummyFields);
        });

        const req = httpMock.expectOne('/api/issue/fields?jiraUrl=http://example.com&projectKey=TEST&issueIdOrKey=1&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFields);
    });

    it('should fetch create meta issue types as an Observable', () => {
        const dummyIssueTypes: CreateMeta.JiraIssueTypeMeta[] = [
            {
                id: '1',
                name: 'Bug',
                description: 'A problem which impairs or prevents the functions of the product.',
                iconUrl: '',
                subtask: false,
                fields: []
            },
            {
                id: '2',
                name: 'Task',
                description: 'A task that needs to be done.',
                iconUrl: '',
                subtask: false,
                fields: []
            }
        ];

        service.getCreateMetaIssueTypes('http://example.com', 'TEST').then(data => {
            expect(data).toEqual(dummyIssueTypes);
        });

        const req = httpMock.expectOne('/api/issue/createmeta/issuetypes?jiraUrl=http://example.com&projectKeyOrId=TEST&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyIssueTypes);
    });

    it('should fetch create meta fields as an Observable', () => {
        const dummyFields: JiraIssueFieldMeta<any>[] = [
            {
                fieldId: 'customfield_10000',
                name: 'Custom Field 1',
                required: false,
                schema: {
                    type: 'string',
                    items: 'string',
                    system: 'system_field'
                },
                allowedValues: [],
                defaultValue: [],
                hasDefaultValue: false,
                key: '',
                operations: []
            },
            {
                fieldId: 'customfield_10001',
                name: 'Custom Field 2',
                required: true,
                schema: {
                    type: 'string',
                    items: 'string',
                    system: 'system_field'
                },
                allowedValues: [],
                defaultValue: [],
                hasDefaultValue: false,
                key: '',
                operations: []
            }
        ];

        service.getCreateMetaFields('http://example.com', 'TEST', '1', 'Bug').then(data => {
            expect(data).toEqual(dummyFields);
        });

        const req = httpMock
            .expectOne('/api/issue/createmeta/fields?jiraUrl=http://example.com&projectKeyOrId=TEST&issueTypeId=1&issueTypeName=Bug&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyFields);
    });

    it('should fetch edit issue metadata as an Observable', () => {
        const dummyEditIssueMetadata: EditIssueMetadata = {
            fields: {
                summary: {
                    required: true,
                    schema: {
                        type: 'string',
                        system: 'summary',
                        items: ''
                    },
                    name: 'Summary',
                    key: 'summary',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                assignee: {
                    required: false,
                    schema: {
                        type: 'string',
                        system: 'assignee',
                        items: ''
                    },
                    name: 'Assignee',
                    key: 'assignee',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                attachment: {
                    required: false,
                    schema: {
                        type: 'array',
                        items: 'attachment',
                        system: 'attachment'
                    },
                    name: 'Attachment',
                    key: 'attachment',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                comment: {
                    required: false,
                    schema: {
                        type: 'array',
                        items: 'comment',
                        system: 'comment'
                    },
                    name: 'Comment',
                    key: 'comment',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                components: {
                    required: false,
                    schema: {
                        type: 'array',
                        items: 'component',
                        system: 'components'
                    },
                    name: 'Components',
                    key: 'components',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                description: {
                    required: false,
                    schema: {
                        type: 'string',
                        system: 'description',
                        items: ''
                    },
                    name: 'Description',
                    key: 'description',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                issuelinks: {
                    required: false,
                    schema: {
                        type: 'array',
                        items: 'issuelink',
                        system: 'issuelinks'
                    },
                    name: 'Issue Links',
                    key: 'issuelinks',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                issuetype: {
                    required: true,
                    schema: {
                        type: 'string',
                        system: 'issuetype',
                        items: ''
                    },
                    name: 'Issue Type',
                    key: 'issuetype',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                labels: {
                    required: false,
                    schema: {
                        type: 'array',
                        items: 'string',
                        system: 'labels'
                    },
                    name: 'Labels',
                    key: 'labels',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                priority: {
                    required: false,
                    schema: {
                        type: 'priority',
                        system: 'priority',
                        items: ''
                    },
                    name: 'Priority',
                    key: 'priority',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                },
                status: {
                    required: true,
                    schema: {
                        type: 'string',
                        system: 'status',
                        items: ''
                    },
                    name: 'Status',
                    key: 'status',
                    operations: ['set'],
                    allowedValues: [],
                    defaultValue: [],
                    hasDefaultValue: false,
                    fieldId: ''
                }
            }
        };

        service.getEditIssueMetadata('http://example.com', 'TEST-1').then(data => {
            expect(data).toEqual(dummyEditIssueMetadata);
        });

        const req = httpMock.expectOne('/api/issue/editmeta?jiraUrl=http://example.com&issueIdOrKey=TEST-1&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyEditIssueMetadata);
    });

    it('should create an issue as an Observable', () => {
        const dummyIssueCreateOptions: IssueCreateOptions = {
            fields: {
                summary: 'Test Issue',
                issuetype: { id: '1' },
                project: { id: '1' }
            },
            metadataRef: ''
        };

        const dummyResponse: JiraApiActionCallResponseWithContent<Issue> = {
            isSuccess: true,
            content: {
                id: '1',
                key: 'TEST-1',
                fields: {
                    summary: 'Test Issue',
                    description: 'This is a test issue',
                    issuetype: {
                        id: '1',
                        name: 'Bug',
                        description: 'A problem which impairs or prevents the functions of the product.',
                        iconUrl: ''
                    },
                    priority: {
                        id: '1',
                        name: 'High',
                        description: 'High Priority',
                        iconUrl: '',
                        statusColor: ''
                    },
                    status: {
                        id: '1',
                        name: 'To Do',
                        description: 'To Do',
                        iconUrl: '',
                        statusCategory: {
                            id: 1,
                            key: 'new',
                            colorName: 'blue',
                            name: 'New'
                        }
                    },
                    project: {
                        id: '1',
                        key: 'TEST',
                        name: 'Test Project',
                        projectTypeKey: 'software',
                        issueTypes: [],
                        simplified: false,
                        avatarUrls: {
                            '16x16': '',
                            '24x24': '',
                            '32x32': '',
                            '48x48': ''
                        }
                    },
                    workratio: 0,
                    watches: {
                        self: '',
                        watchCount: 0,
                        isWatching: false
                    },
                    created: '',
                    labels: [],
                    versions: [],
                    issuelinks: [],
                    assignee: {
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
                    },
                    updated: '',
                    components: [],
                    attachment: [],
                    creator: {
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
                    },
                    subtasks: [],
                    reporter: {
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
                    },
                    aggregateprogress: {
                        progress: 0,
                        total: 0
                    },
                    progress: {
                        progress: 0,
                        total: 0
                    },
                    comment: {
                        comments: [],
                        maxResults: 0,
                        total: 0,
                        startAt: 0
                    },
                    votes: {
                        self: '',
                        votes: 0,
                        hasVoted: false
                    },
                    editIssueMetadata: undefined,
                    requestType: {
                        requestType: {
                            id: '1',
                            name: 'Service Request',
                            description: 'Service Request',
                            helpTest: '',
                            issueTypeId: '',
                            groupIds: [],
                            icon: {
                                id: '',
                                _links: {
                                    iconUrls: {
                                        '16x16': '',
                                        '24x24': '',
                                        '32x32': '',
                                        '48x48': ''
                                    }
                                }
                            }
                        }
                    }
                },
                expand: '',
                self: ''
            }
        };

        service.createIssue('http://example.com', dummyIssueCreateOptions).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/issue?jiraUrl=http://example.com');
        expect(req.request.method).toBe('POST');
        req.flush(dummyResponse);
    });

    it('should update an issue as an Observable', () => {
        const dummyResponse: JiraApiActionCallResponse = {
            isSuccess: true,
        };

        const dummyIssueFields: Partial<IssueFields> = {
            summary: 'Updated Summary',
            description: 'Updated Description'
        };

        service.updateIssue('http://example.com', 'TEST-1', dummyIssueFields).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/issue?jiraUrl=http://example.com&issueIdOrKey=TEST-1');
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(dummyIssueFields);
        req.flush(dummyResponse);
    });

    it('should update issue description as an Observable', () => {
        const dummyResponse: JiraApiActionCallResponse = {
            isSuccess: true
        };

        service.updateIssueDescription('http://example.com', 'TEST-1', 'Updated description')
            .then(data => {
                expect(data).toEqual(dummyResponse);
            });

        const req = httpMock.expectOne('/api/issue/description?jiraUrl=http://example.com&issueIdOrKey=TEST-1&');
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ description: 'Updated description' });
        req.flush(dummyResponse);
    });

    it('should update issue summary as an Observable', () => {
        const dummyResponse: JiraApiActionCallResponse = {
            isSuccess: true
        };

        service.updateIssueSummary('http://example.com', 'TEST-1', 'Updated Summary').then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/issue/summary?jiraUrl=http://example.com&issueIdOrKey=TEST-1&summary=Updated%20Summary&');
        expect(req.request.method).toBe('PUT');
        req.flush(dummyResponse);
    });

    it('should update issue priority as an Observable', () => {
        const dummyResponse: JiraApiActionCallResponse = {
            isSuccess: true
        };

        const dummyPriority: Priority = {
            id: '1',
            name: 'High',
            description: 'High Priority',
            iconUrl: '',
            statusColor: ''
        };

        service.updatePriority('http://example.com', 'TEST-1', dummyPriority).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/issue/updatePriority?jiraUrl=http://example.com&issueIdOrKey=TEST-1&');
        expect(req.request.method).toBe('PUT');
        req.flush(dummyResponse);
    });

    it('should submit login info successfully', () => {
        const dummyResponse = { isSuccess: true, message: 'Login successful' };
        const jiraId = 'dummyJiraId';
        const requestToken = 'dummyRequestToken';
        const verificationCode = 'dummyVerificationCode';

        service.submitLoginInfo(jiraId, requestToken, verificationCode).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/submit-login-info');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ atlasId: jiraId, verificationCode, requestToken });
        req.flush(dummyResponse);
    });

    it('should submit login info with default parameters', () => {
        const dummyResponse = { isSuccess: true, message: 'Login successful' };
        const jiraId = 'dummyJiraId';

        service.submitLoginInfo(jiraId).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/submit-login-info');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ atlasId: jiraId, verificationCode: '', requestToken: '' });
        req.flush(dummyResponse);
    });

    it('should handle login info submission failure', () => {
        const dummyResponse = { isSuccess: false, message: 'Login failed' };
        const jiraId = 'dummyJiraId';
        const requestToken = 'dummyRequestToken';
        const verificationCode = 'dummyVerificationCode';

        service.submitLoginInfo(jiraId, requestToken, verificationCode).then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/submit-login-info');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ atlasId: jiraId, verificationCode, requestToken });
        req.flush(dummyResponse);
    });

    it('should log out successfully', () => {
        const dummyResponse = { isSuccess: true };

        service.logOut('jiraId123').then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/logout?jiraId=jiraId123');
        expect(req.request.method).toBe('POST');
        req.flush(dummyResponse);
    });

    it('should handle log out failure', () => {
        const dummyResponse = { isSuccess: false };

        service.logOut('jiraId123').then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/logout?jiraId=jiraId123');
        expect(req.request.method).toBe('POST');
        req.flush(dummyResponse);
    });

    it('should search users as an Observable', () => {
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
            },
            {
                accountId: '67890',
                displayName: 'Jane Smith',
                emailAddress: 'jane.smith@example.com',
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

        service.searchUsers('http://example.com', 'John').then(data => {
            expect(data).toEqual(dummyUsers);
        });

        const req = httpMock.expectOne('/api/user/search?jiraUrl=http://example.com&username=John&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyUsers);
    });

    it('should search users with empty username as an Observable', () => {
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
            },
            {
                accountId: '67890',
                displayName: 'Jane Smith',
                emailAddress: 'jane.smith@example.com',
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

        service.searchUsers('http://example.com').then(data => {
            expect(data).toEqual(dummyUsers);
        });

        const req = httpMock.expectOne('/api/user/search?jiraUrl=http://example.com&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyUsers);
    });

    it('should fetch autocomplete data as an Observable', () => {
        const dummyAutocompleteData: JiraFieldAutocomplete[] = [
            {
                value: 'test1',
                displayName: 'Test 1'
            },
            {
                value: 'test2',
                displayName: 'Test 2'
            }
        ];

        service.getAutocompleteData('http://example.com', 'fieldName').then(data => {
            expect(data).toEqual(dummyAutocompleteData);
        });

        const req = httpMock.expectOne('/api/issue/autocompletedata?jiraUrl=http://example.com&fieldName=fieldName&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyAutocompleteData);
    });

    it('should fetch autocomplete data with empty field name as an Observable', () => {
        const dummyAutocompleteData: JiraFieldAutocomplete[] = [
            {
                value: 'test1',
                displayName: 'Test 1'
            },
            {
                value: 'test2',
                displayName: 'Test 2'
            }
        ];

        service.getAutocompleteData('http://example.com').then(data => {
            expect(data).toEqual(dummyAutocompleteData);
        });

        const req = httpMock.expectOne('/api/issue/autocompletedata?jiraUrl=http://example.com&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyAutocompleteData);
    });

    it('should fetch sprints as an Observable', () => {
        const dummySprints: JiraIssueSprint[] = [
            {
                id: '1',
                name: 'Sprint 1',
                state: 'active',
                self: '',
            },
            {
                id: '2',
                name: 'Sprint 2',
                state: 'closed',
                self: ''
            }
        ];

        service.getSprints('http://example.com', 'TEST').then(data => {
            expect(data).toEqual(dummySprints);
        });

        const req = httpMock.expectOne('/api/issue/sprint?jiraUrl=http://example.com&projectKeyOrId=TEST&');
        expect(req.request.method).toBe('GET');
        req.flush(dummySprints);
    });

    it('should fetch epics as an Observable', () => {
        const dummyEpics: JiraIssueEpic[] = [
            {
                id: '1',
                key: 'EPIC-1',
                name: 'Epic 1',
                summary: 'This is the first epic',
                color: {
                    key: 'color-1'
                },
                self: '',
                done: ''
            },
            {
                id: '2',
                key: 'EPIC-2',
                name: 'Epic 2',
                summary: 'This is the second epic',
                done: '',
                color: {
                    key: 'color-2'
                },
                self: ''
            }
        ];

        service.getEpics('http://example.com', 'TEST').then(data => {
            expect(data).toEqual(dummyEpics);
        });

        const req = httpMock.expectOne('/api/issue/epic?jiraUrl=http://example.com&projectKeyOrId=TEST&');
        expect(req.request.method).toBe('GET');
        req.flush(dummyEpics);
    });

    it('should save Jira server ID as an Observable', () => {
        const dummyResponse = { isSuccess: true, message: 'Jira server ID saved successfully' };

        service.saveJiraServerId('dummy-server-id').then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/save-jira-server-id?jiraServerId=dummy-server-id');
        expect(req.request.method).toBe('POST');
        req.flush(dummyResponse);
    });

    it('should validate connection as an Observable', () => {
        const dummyResponse = { isSuccess: true };

        service.validateConnection('dummy-server-id').then(data => {
            expect(data).toEqual(dummyResponse);
        });

        const req = httpMock.expectOne('/api/validate-connection?jiraServerId=dummy-server-id');
        expect(req.request.method).toBe('GET');
        req.flush(dummyResponse);
    });
});



