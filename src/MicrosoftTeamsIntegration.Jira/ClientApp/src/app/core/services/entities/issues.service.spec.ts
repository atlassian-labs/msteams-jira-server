import { TestBed } from '@angular/core/testing';

import { IssuesService } from '@core/services/entities/issues.service';
import { } from 'jasmine'; // Fix warnings with decribe, beforeEach and other jasmine methods
import { Issue, NormalizedIssue, IssueCustomFields, NormalizedSlaField } from '@core/models';
import { JqlOptions } from '@core/models/Jira/jql-settings.model';
import { BreachTime, CompletedCycle, JiraSlaField, OngoingCycle } from '@core/models/Jira/jira-sla-field.model';
import { JiraUser } from '@core/models/Jira/jira-user.model';

describe('IssuesService', () => {
    let service: IssuesService;
    const testIssue: Issue = {
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

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [IssuesService]
        });

        service = TestBed.inject(IssuesService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should normalize issues', () => {
        const issueToNormalize: Issue = {...testIssue};
        const issues: Issue[] = [
            issueToNormalize
        ];

        const normalizedIssues = service.normalizeIssues(issues);
        expect(normalizedIssues.length).toBe(1);
        expect(normalizedIssues[0].issuekey).toBe('TEST-1');
        expect(normalizedIssues[0].assignee).toBe('John Doe');
    });

    it('should normalize issues if issues list is empty', () => {
        const issues: Issue[] = [];

        const normalizedIssues = service.normalizeIssues(issues);
        expect(normalizedIssues.length).toBe(0);
        expect(normalizedIssues).toEqual([]);
    });

    it('should normalize issues and return assignee name for unknown assignee', () => {
        const originalAssignee = testIssue.fields.assignee;
        const issueToNormalize: Issue = {...testIssue};;
        issueToNormalize.fields.assignee = undefined as unknown as JiraUser;
        const issues: Issue[] = [
            issueToNormalize
        ];

        const normalizedIssues = service.normalizeIssues(issues);
        expect(normalizedIssues.length).toBe(1);
        expect(normalizedIssues[0].assignee).toEqual('Unassigned');
        testIssue.fields.assignee = originalAssignee;
    });

    it('should normalize issues and return reporter name for unknown reporter', () => {
        const originalReporter = testIssue.fields.reporter;
        const issueToNormalize: Issue = {...testIssue};;
        issueToNormalize.fields.reporter = undefined as unknown as JiraUser;
        const issues: Issue[] = [
            issueToNormalize
        ];

        const normalizedIssues = service.normalizeIssues(issues);
        expect(normalizedIssues.length).toBe(1);
        expect(normalizedIssues[0].reporter).toEqual('Unassigned');
        testIssue.fields.reporter = originalReporter;
    });


    it('should normalize issues and return resolution when it is undefined', () => {
        const issueToNormalize: Issue = {...testIssue};;
        issueToNormalize.fields['resolution'] = undefined;
        const issues: Issue[] = [
            issueToNormalize
        ];

        const normalizedIssues = service.normalizeIssues(issues);
        expect(normalizedIssues.length).toBe(1);
        expect(normalizedIssues[0].resolution).toEqual('Unresolved');
    });

    it('should get displayed columns ordered', () => {
        const unmappedFieldsInOrder = ['customfield_10021', 'issuetype'];
        const fieldNamesMap = { customfield_10021: 'Time to resolution', issuetype: 'Issue type' };

        const displayedColumns = service.getDisplayedColumnsOrdered(unmappedFieldsInOrder, fieldNamesMap);
        expect(displayedColumns).toEqual(['timeToResolution', 'issuetype']);
    });

    it('should get displayed columns ordered for empty list', () => {
        const fieldNamesMap = { customfield_10021: 'Time to resolution', issuetype: 'Issue type' };

        const displayedColumns = service.getDisplayedColumnsOrdered([], fieldNamesMap);
        expect(displayedColumns).toEqual([]);
    });

    it('should map custom fields', () => {
        const issue: Issue = { ...testIssue };
        issue.fields['customfield_10021'] = {
            id: '1',
            name: 'Time to resolution',
            type: 'com.atlassian.jira.plugin.system.customfieldtypes:float',
        };

        const issues: Issue[] = [
            issue
        ];
        const fieldNamesMap = { customfield_10021: 'Time to resolution' };

        const mappedIssues = service.mapCustomFields(issues, fieldNamesMap);
        expect(mappedIssues[0].fields.timeToResolution).toBeDefined();
    });

    it('should map custom fields and return empty list if issue list is empty', () => {
        const issues: Issue[] = [
        ];
        const fieldNamesMap = { customfield_10021: 'Time to resolution' };

        const mappedIssues = service.mapCustomFields(issues, fieldNamesMap);
        expect(mappedIssues).toEqual([]);
    });

    it('should get normalized SLA field', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: {
                remainingTime: { millis: 60000, friendly: '1m' },
                breached: false,
                paused: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [],
            id: '',
            name: ''
        };

        const normalizedField = service.getNormalizedSlaField(jiraField);
        expect(normalizedField.remainingTimeMillis).toBe(60000);
        expect(normalizedField.remainingTimeFriendly).toBe('0:01');
    });

    it('should resolve icons in SLA fields if ongoingCycle defined', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: {
                remainingTime: { millis: 60000, friendly: '1m' },
                breached: false,
                paused: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [],
            id: '',
            name: ''
        };

        const iconClass = service.resolveIconsInSlaFields(jiraField);
        expect(iconClass).toContain('aui-icon aui-icon-small sla-status-icon');
    });

    it('should resolve icons in SLA fields if ongoingCycle undefined', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: undefined as unknown as OngoingCycle,
            completedCycles: [{
                stopTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                goalDuration: {
                    millis: 0,
                    friendly: ''
                },
                elapsedTime: {
                    millis: 0,
                    friendly: ''
                },
                remainingTime: {
                    millis: 0,
                    friendly: ''
                },
                breached: true,
                withinCalendarHours: true,
            } as unknown as CompletedCycle],
            id: '',
            name: ''
        };

        const iconClass = service.resolveIconsInSlaFields(jiraField);
        expect(iconClass).toContain('aui-icon aui-icon-small sla-status-icon aui-iconfont-close-dialog sla-breached');
    });

    it('should not resolve icons in SLA fields if field is undefined ', () => {
        const iconClass = service.resolveIconsInSlaFields(undefined as unknown as JiraSlaField);
        expect(iconClass).toBeUndefined();
    });

    it('should get friendly time of time to field when ongoingCycle defined', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: {
                remainingTime: { millis: 60000, friendly: '1m' },
                breached: false,
                paused: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [],
            id: '',
            name: ''
        };

        const friendlyTime = service.getFriendlyTimeOfTimeToField(jiraField);
        expect(friendlyTime).toBe('0:01');
    });

    it('should get friendly time of time to field when ongoingCycle undefined', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: undefined as unknown as OngoingCycle,
            completedCycles: [{
                stopTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                goalDuration: {
                    millis: 0,
                    friendly: ''
                },
                elapsedTime: {
                    millis: 0,
                    friendly: ''
                },
                remainingTime: {
                    millis: 0,
                    friendly: '1h 10m'
                },
                breached: true,
                withinCalendarHours: true,
            } as unknown as CompletedCycle],
            id: '',
            name: ''
        };

        const friendlyTime = service.getFriendlyTimeOfTimeToField(jiraField);
        expect(friendlyTime).toBe('1:10');
    });


    it('should get friendly time of time to field when completedCycles and ongoingCycle are not defined', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: undefined as unknown as OngoingCycle,
            completedCycles: [],
            id: '',
            name: ''
        };

        const friendlyTime = service.getFriendlyTimeOfTimeToField(jiraField);
        expect(friendlyTime).toEqual(null);
    });

    it('should handle getting friendly time for undefined field', () => {
        const jiraField: JiraSlaField = undefined as unknown as JiraSlaField;

        const friendlyTime = service.getFriendlyTimeOfTimeToField(jiraField);
        expect(friendlyTime).toBeUndefined();
    });

    it('should get milliseconds of time to field when ongoingCycle defined', () => {
        const jiraField: JiraSlaField = {
            id: '1',
            name: 'Time to resolution',
            ongoingCycle: {
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                } as BreachTime,
                paused: false,
                remainingTime: {
                    millis: 60000,
                    friendly: '1m'
                },
                breached: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [] as unknown as CompletedCycle[]
        };

        const millis = service.getMillisecondsOfTimeToField(jiraField);
        expect(millis).toBe(60000);
    });

    it('should get milliseconds of time to field when ongoingCycle undefined', () => {
        const jiraField: JiraSlaField = {
            id: '1',
            name: 'Time to resolution',
            ongoingCycle: undefined as unknown as OngoingCycle,
            completedCycles: [{
                stopTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                goalDuration: {
                    millis: 0,
                    friendly: ''
                },
                elapsedTime: {
                    millis: 0,
                    friendly: ''
                },
                remainingTime: {
                    millis: 10000,
                    friendly: ''
                },
                breached: false,
                withinCalendarHours: true
            }] as unknown as CompletedCycle[]
        };

        const millis = service.getMillisecondsOfTimeToField(jiraField);
        expect(millis).toBe(10000);
    });

    it('should check if time to field is within calendar hours', () => {
        const jiraField: JiraSlaField = {
            id: '1',
            name: 'Time to resolution',
            ongoingCycle: {
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                } as BreachTime,
                paused: false,
                remainingTime: {
                    millis: 60000,
                    friendly: '1m'
                },
                breached: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [{
                stopTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                breachTime: {
                    iso8601: new Date(),
                    jira: new Date(),
                    friendly: '',
                    epochMillis: 0
                },
                goalDuration: {
                    millis: 0,
                    friendly: ''
                },
                elapsedTime: {
                    millis: 0,
                    friendly: ''
                },
                remainingTime: {
                    millis: 0,
                    friendly: ''
                },
                breached: false,
                withinCalendarHours: true
            }] as unknown as CompletedCycle[]
        };

        const isWithinCalendarHours = service.isCalendarHoursOfTimeToField(jiraField);
        expect(isWithinCalendarHours).toBeTrue();
    });

    it('should check if time to field is breached', () => {
        const jiraField: JiraSlaField = {
            ongoingCycle: {
                remainingTime: { millis: 60000, friendly: '1m' },
                breached: true,
                paused: false,
                withinCalendarHours: true
            } as OngoingCycle,
            completedCycles: [],
            id: '',
            name: ''
        };

        const isBreached = service.isBreachedOfTimeToField(jiraField);
        expect(isBreached).toBeTrue();
    });

    it('should get filters from query', () => {
        const query = 'type in ("Task", "Sub-task") AND project = TEST';
        const filters = service.getFiltersFromQuery(query);
        expect(filters).toBe('Task,  Sub-task, ');
    });

    it('should create JQL query', () => {
        const options: JqlOptions = {
            accountId: '123',
            page: 'IssuesAssigned',
            projectKey: 'TEST',
            jql: 'status = "To Do"',
            jqlSuffix: 'order by priority DESC'
        };

        const jqlQuery = service.createJqlQuery(options);
        expect(jqlQuery).toBe('status = "To Do" AND project="TEST" AND (assignee=currentUser()) order by priority DESC');
    });

    it('should create JQL query when project is in JQL', () => {
        const options: JqlOptions = {
            accountId: '123',
            page: 'IssuesAssigned',
            projectKey: 'TEST',
            jql: 'status = "To Do" AND project="TEST"',
            jqlSuffix: 'order by priority DESC'
        };

        const jqlQuery = service.createJqlQuery(options);
        expect(jqlQuery).toBe('status = "To Do" AND project="TEST" AND (assignee=currentUser()) order by priority DESC');
    });

    it('should create JQL query when JQL in in map', () => {
        const options: JqlOptions = {
            accountId: '',
            page: '',
            projectKey: '',
            jql: 'all-issues',
            jqlSuffix: ''
        };

        const jqlQuery = service.createJqlQuery(options);
        expect(jqlQuery).toBe('order by created DESC');
    });

    it('should create full filter JQL', () => {
        const baseJqlQuery = 'project = TEST';
        const filterJql = 'status = "To Do" order by priority DESC';

        const fullJql = service.createFullFilterJql(baseJqlQuery, filterJql);
        expect(fullJql).toBe('project = TEST AND (status = "To Do") order by priority DESC');
    });

    it('should adjust order by query string with issue property', () => {
        const orderByQuery = 'order by created DESC';
        const adjustedQuery = service.adjustOrderByQueryStringWithIssueProperty(orderByQuery, 'created', 'updated');
        expect(adjustedQuery).toBe('order by updated DESC');
    });
});
