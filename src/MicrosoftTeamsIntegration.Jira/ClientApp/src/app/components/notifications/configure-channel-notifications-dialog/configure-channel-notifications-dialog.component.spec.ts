import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfigureChannelNotificationsDialogComponent } from './configure-channel-notifications-dialog.component';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, AppInsightsService, UtilService } from '@core/services';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';
import { NotificationService } from '@shared/services/notificationService';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { of } from 'rxjs';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import { NotificationSubscriptionEvent, NotificationSubscriptionAction } from '@core/models/NotificationSubscriptionEvent';
import { Project, ProjectTypeKey } from '@core/models';
import { SelectOption } from '@shared/models/select-option.model';
import { TeamsService } from '@core/services/teams.service';
describe('ConfigureChannelNotificationsDialogComponent', () => {
    let teamsService: jasmine.SpyObj<TeamsService>;
    let component: ConfigureChannelNotificationsDialogComponent;
    let fixture: ComponentFixture<ConfigureChannelNotificationsDialogComponent>;
    let mockApiService: jasmine.SpyObj<ApiService>;
    let mockNotificationService: jasmine.SpyObj<NotificationService>;
    let mockUtilsService: jasmine.SpyObj<UtilService>;
    let mockAppInsightsService: jasmine.SpyObj<AppInsightsService>;
    let mockTransitionService: jasmine.SpyObj<IssueTransitionService>;
    let mockDropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    let mockRouter: jasmine.SpyObj<Router>;
    let mockActivatedRoute: any;
    let mockAnalyticsService: jasmine.SpyObj<AnalyticsService>;

    const mockNotificationSubscription: NotificationSubscription = {
        jiraId: 'test-jira-id',
        subscriptionType: SubscriptionType.Channel,
        eventTypes: ['IssueCreated', 'CommentCreated'],
        isActive: true,
        filter: 'project = TEST AND type in ("Bug")',
        microsoftUserId: '',
        conversationId: 'test-conversation-id',
        conversationReferenceId: 'test-reference-id',
        projectId: 'TEST',
        projectName: 'Test Project'
    };

    const mockProject: Project = {
        id: 'TEST',
        key: 'TEST',
        name: 'Test Project',
        projectTypeKey: 'software',
        issueTypes: [],
        avatarUrls: {
            '16x16': '',
            '24x24': '',
            '32x32': '',
            '48x48': ''
        },
        simplified: false
    };

    const mockIssueType = {
        id: '1',
        name: 'Bug',
        fields: [],
        description: '',
        iconUrl: ''
    };

    const mockStatuses = [
        {
            id: '1',
            name: 'To Do',
            description: 'Task is not started',
            iconUrl: 'https://example.com/todo.png'
        },
        {
            id: '2',
            name: 'In Progress',
            description: 'Task is being worked on',
            iconUrl: 'https://example.com/inprogress.png'
        },
        {
            id: '3',
            name: 'Done',
            description: 'Task is completed',
            iconUrl: 'https://example.com/done.png'
        }
    ];

    beforeEach(async () => {
        mockApiService = jasmine.createSpyObj('ApiService', [
            'getAllNotificationsByConversationId',
            'getProjects',
            'getCreateMetaIssueTypes',
            'getCreateMetaFields',
            'updateNotification',
            'addNotification',
            'deleteNotification',
            'sendNotificationSubscriptionEvent',
            'getAddonStatus',
            'getStatusesByProject',
            'findProjects'
        ]);

        mockNotificationService = jasmine.createSpyObj('NotificationService', [
            'notifySuccess',
            'notifyError'
        ]);

        mockUtilsService = jasmine.createSpyObj('UtilService', [
            'isAddonUpdatedToVersion',
            'getMinAddonVersionForNotifications',
            'getUpgradeAddonMessageForNotifications',
            'getMsTeamsContext'
        ]);

        mockAppInsightsService = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        mockTransitionService = jasmine.createSpyObj('IssueTransitionService', ['getTransitionsByProjectKeyOrId']);
        mockDropdownUtilService = jasmine.createSpyObj('DropdownUtilService', [
            'mapProjectToDropdownOption',
            'mapIssueTypeToDropdownOption',
            'mapPriorityToDropdownOption',
            'mapTransitionToDropdownOptionString',
            'mapStatusToDropdownOption'
        ]);

        mockAnalyticsService = jasmine.createSpyObj('AnalyticsService', [
            'sendScreenEvent',
            'sendUiEvent'
        ]);

        mockRouter = jasmine.createSpyObj('Router', ['navigate']);

        const mockSnackBarRef = jasmine.createSpyObj('MatSnackBarRef', ['afterDismissed']);
        mockSnackBarRef.afterDismissed.and.returnValue(of({ dismissedByAction: false }));

        mockNotificationService.notifySuccess.and.returnValue(mockSnackBarRef);
        mockNotificationService.notifyError.and.returnValue(mockSnackBarRef);

        mockUtilsService.isAddonUpdatedToVersion.and.returnValue(true);
        mockUtilsService.getMsTeamsContext.and.returnValue({
            tid: 'test-tenant-id',
            loginHint: 'test-login-hint',
            userObjectId: 'test-user-id',
            locale: 'en-US'
        });
        mockApiService.getAddonStatus.and.returnValue(Promise.resolve({ addonStatus: 1, addonVersion: '1.0.0' }));

        mockActivatedRoute = {
            snapshot: {
                params: {
                    jiraId: 'test-jira-id',
                    microsoftUserId: '',
                    conversationId: 'test-conversation-id',
                    conversationReferenceId: 'test-reference-id'
                }
            }
        };

        // Setup mock implementations for dropdown mapping methods
        mockDropdownUtilService.mapStatusToDropdownOption.and.callFake((status: any) => ({
            id: status.id,
            value: status.id,
            label: status.name
        }));

        mockDropdownUtilService.mapProjectToDropdownOption.and.callFake((project: any) => ({
            id: project.id,
            value: project.id,
            label: project.name
        }));

        mockDropdownUtilService.mapIssueTypeToDropdownOption.and.callFake((issueType: any) => ({
            id: issueType.id,
            value: issueType.id,
            label: issueType.name
        }));

        await TestBed.configureTestingModule({
            declarations: [ConfigureChannelNotificationsDialogComponent],
            providers: [
                { provide: ApiService, useValue: mockApiService },
                { provide: NotificationService, useValue: mockNotificationService },
                { provide: ActivatedRoute, useValue: mockActivatedRoute },
                { provide: UtilService, useValue: mockUtilsService },
                { provide: AppInsightsService, useValue: mockAppInsightsService },
                { provide: IssueTransitionService, useValue: mockTransitionService },
                { provide: DropdownUtilService, useValue: mockDropdownUtilService },
                { provide: Router, useValue: mockRouter },
                { provide: AnalyticsService, useValue: mockAnalyticsService },
                {
                    provide: TeamsService,
                    useValue: {
                        initialize: jasmine.createSpy('initialize').and.returnValue(Promise.resolve()),
                        notifySuccess: jasmine.createSpy('notifySuccess'),
                        submitDialog: jasmine.createSpy('submitDialog')
                    }
                }]
        }).compileComponents();

        fixture = TestBed.createComponent(ConfigureChannelNotificationsDialogComponent);
        component = fixture.componentInstance;
        teamsService = TestBed.inject(TeamsService) as jasmine.SpyObj<TeamsService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('ngOnInit', () => {
        it('should initialize component with route parameters and display notifications list', async () => {
            mockApiService.getAllNotificationsByConversationId.and.returnValue(Promise.resolve([mockNotificationSubscription]));

            await component.ngOnInit();

            expect(component.jiraId).toBe('test-jira-id');
            expect(component.conversationId).toBe('test-conversation-id');
            expect(component.conversationReferenceId).toBe('test-reference-id');
            expect(component.showNotificationsListGroup).toBeTrue();
            expect(mockAppInsightsService.logNavigation).toHaveBeenCalled();
            expect(mockAnalyticsService.sendScreenEvent).toHaveBeenCalled();
        });

        it('should display initial container when no notifications exist', async () => {
            mockApiService.getAllNotificationsByConversationId.and.returnValue(Promise.resolve([]));

            await component.ngOnInit();

            expect(component.showInitialContainer).toBeTrue();
            expect(component.showNotificationsListGroup).toBeFalse();
        });
    });

    describe('createForm', () => {
        it('should create form with default values', async () => {
            mockApiService.getProjects.and.returnValue(Promise.resolve([mockProject]));
            mockApiService.getCreateMetaIssueTypes.and.returnValue(Promise.resolve([mockIssueType]));
            mockApiService.getStatusesByProject.and.returnValue(Promise.resolve(mockStatuses));
            mockTransitionService.getTransitionsByProjectKeyOrId.and.returnValue(Promise.resolve([{
                expand: 'transitions',
                transitions: []
            }]));

            await component.displayCreateNotificationsGroup();

            expect(component.issueForm).toBeDefined();
            expect(component.issueForm.get('project')).toBeDefined();
            expect(component.issueForm.get('issuetype')).toBeDefined();
        });

        it('should handle project selection and update issue types', async () => {
            mockApiService.getProjects.and.returnValue(Promise.resolve([mockProject]));
            mockApiService.getCreateMetaIssueTypes.and.returnValue(Promise.resolve([mockIssueType]));
            mockApiService.getStatusesByProject.and.returnValue(Promise.resolve(mockStatuses));

            await component.onProjectSelected('TEST');

            expect(mockDropdownUtilService.mapStatusToDropdownOption).toHaveBeenCalled();
            expect(component.availableIssueTypesOptions.length).toBeGreaterThan(0);
            expect(component.statusesOptions.length).toBeGreaterThan(0);
        });

        it('should handle error when fetching statuses', async () => {
            mockApiService.getProjects.and.returnValue(Promise.resolve([mockProject]));
            mockApiService.getCreateMetaIssueTypes.and.returnValue(Promise.resolve([mockIssueType]));
            mockApiService.getStatusesByProject.and.returnValue(Promise.reject('Error'));

            await component.onProjectSelected('TEST');

            expect(mockNotificationService.notifyError).toHaveBeenCalledWith('Failed to fetch statuses. Please try again later.');
        });
    });

    describe('deleteNotification', () => {
        it('should delete notification successfully', async () => {
            mockApiService.deleteNotification.and.returnValue(Promise.resolve());
            mockApiService.sendNotificationSubscriptionEvent.and.returnValue(Promise.resolve());

            await component.deleteNotification(mockNotificationSubscription);

            expect(mockNotificationService.notifySuccess).toHaveBeenCalledWith('Notification deleted successfully.');
            expect(mockAnalyticsService.sendUiEvent).toHaveBeenCalled();
        });
    });

    describe('toggleNotification', () => {
        it('should toggle notification status successfully', async () => {
            mockApiService.updateNotification.and.returnValue(Promise.resolve());

            await component.toggleNotification(mockNotificationSubscription);

            expect(mockApiService.updateNotification).toHaveBeenCalled();
            expect(mockNotificationService.notifySuccess).toHaveBeenCalled();
            expect(mockAnalyticsService.sendUiEvent).toHaveBeenCalled();
        });
    });

    describe('checkAddonUpdated', () => {
        it('should show error when addon is not updated', async () => {
            mockUtilsService.isAddonUpdatedToVersion.and.returnValue(false);
            mockUtilsService.getUpgradeAddonMessageForNotifications.and.returnValue('Please upgrade addon');

            await component['checkAddonUpdated']();

            expect(mockNotificationService.notifyError).toHaveBeenCalled();
        });
    });

    describe('onSearchChanged', () => {
        const mockFoundProjects: Project[] = [
            {
                id: 'NEW1',
                key: 'NEW1',
                name: 'New Project 1',
                projectTypeKey: 'software' as ProjectTypeKey,
                issueTypes: [],
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                simplified: false
            },
            {
                id: 'NEW2',
                key: 'NEW2',
                name: 'New Project 2',
                projectTypeKey: 'software' as ProjectTypeKey,
                issueTypes: [],
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                simplified: false
            }
        ];

        beforeEach(() => {
            component.projects = [mockProject];
            component.projectsDropdown = {
                filteredOptions: []
            };
        });

        it('should update filtered projects when search is successful', async () => {
            mockApiService.findProjects.and.returnValue(Promise.resolve(mockFoundProjects));
            component.jiraId = 'test-jira-id';

            await component.onSearchChanged('new');

            expect(component.isFetchingProjects).toBeFalse();
            expect(component.projectsDropdown.filteredOptions.length).toBe(2);
            expect(component.projects.length).toBe(3); // Original + 2 new projects
            expect(mockApiService.findProjects).toHaveBeenCalledWith('test-jira-id', 'new', true);
        });

        it('should handle empty search string', async () => {
            mockApiService.findProjects.and.returnValue(Promise.resolve([]));
            component.jiraId = 'test-jira-id';
            await component.onSearchChanged('   ');

            expect(component.isFetchingProjects).toBeFalse();
            expect(mockApiService.findProjects).toHaveBeenCalledWith('test-jira-id', '', true);
        });

        it('should handle API error gracefully', async () => {
            const error = new Error('API Error');
            mockApiService.findProjects.and.returnValue(Promise.reject(error));

            await component.onSearchChanged('error');

            expect(component.isFetchingProjects).toBeFalse();
            expect(mockAppInsightsService.trackException).toHaveBeenCalledWith(
                new Error('Error while searching projects'),
                'Configure Channel Notifications Dialog',
                { originalErrorMessage: 'API Error' }
            );
        });

        it('should not add duplicate projects', async () => {
            const duplicateProject = { ...mockProject };
            mockApiService.findProjects.and.returnValue(Promise.resolve([duplicateProject]));

            await component.onSearchChanged('test');

            expect(component.projects.length).toBe(1); // Should not add duplicate
            expect(component.projectsDropdown.filteredOptions.length).toBe(1);
        });
    });

    describe('compareSelectOptions', () => {
        const option1: SelectOption = { id: '1', label: 'Option 1', value: 'value1' };
        const option2: SelectOption = { id: '2', label: 'Option 2', value: 'value2' };
        const option1Copy: SelectOption = { id: '1', label: 'Option 1', value: 'value1' };

        it('should return true for identical options', () => {
            expect(component.compareSelectOptions(option1, option1)).toBeTrue();
        });

        it('should return true for options with same id', () => {
            expect(component.compareSelectOptions(option1, option1Copy)).toBeTrue();
        });

        it('should return false for different options', () => {
            expect(component.compareSelectOptions(option1, option2)).toBeFalse();
        });

        it('should handle null/undefined options', () => {
            expect(component.compareSelectOptions(null as any, option1)).toBeFalse();
            expect(component.compareSelectOptions(option1, null as any)).toBeFalse();
            expect(component.compareSelectOptions(undefined as any, option1)).toBeFalse();
            expect(component.compareSelectOptions(option1, undefined as any)).toBeFalse();
        });

        it('should handle mixed type comparisons', () => {
            const stringOption = '1';
            expect(component.compareSelectOptions(stringOption as any, option1)).toBeFalse();
            expect(component.compareSelectOptions(option1, stringOption as any)).toBeFalse();
        });
    });

    describe('saveNotification', () => {
        const mockFormValue = {
            subscriptionId: '',
            jiraId: 'test-jira-id',
            project: 'TEST',
            issuetype: '1',
            status: '1',
            priority: '1',
            issueIs: [
                { id: 'issueCreated', label: 'Created', value: 'IssueCreated' },
                { id: 'issueUpdated', label: 'Updated', value: 'IssueUpdated' }
            ],
            commentIs: [
                { id: 'commentCreated', label: 'Created', value: 'CommentCreated' },
                { id: 'commentUpdated', label: 'Updated', value: 'CommentUpdated' }
            ]
        };

        const mockNewNotification: NotificationSubscription = {
            jiraId: 'test-jira-id',
            subscriptionType: SubscriptionType.Channel,
            projectId: 'TEST',
            projectName: 'Test Project',
            filter: 'project = "TEST" AND type in ("Bug") AND priority in ("High") AND status in ("To Do")',
            microsoftUserId: '',
            conversationId: 'test-conversation-id',
            conversationReferenceId: 'test-reference-id',
            eventTypes: ['IssueCreated', 'IssueUpdated', 'CommentCreated', 'CommentUpdated'],
            isActive: true
        };

        beforeEach(() => {
            component.jiraId = 'test-jira-id';
            component.conversationId = 'test-conversation-id';
            component.conversationReferenceId = 'test-reference-id';
            component.projects = [mockProject];
            component.issueTypes = [mockIssueType];
            component.statusesOptions = mockStatuses.map(s => ({ id: s.id, value: s.id, label: s.name }));
            component.prioritiesOptions = [{ id: '1', value: '1', label: 'High' }];
            component.issueForm = {
                valid: true,
                value: mockFormValue
            } as any;

            mockApiService.addNotification.and.returnValue(Promise.resolve());
            mockApiService.updateNotification.and.returnValue(Promise.resolve());
            mockApiService.sendNotificationSubscriptionEvent.and.returnValue(Promise.resolve());
        });

        it('should not proceed if form is invalid', async () => {
            component.issueForm.valid = false;

            await component.saveNotification();

            expect(mockApiService.addNotification).not.toHaveBeenCalled();
            expect(mockApiService.updateNotification).not.toHaveBeenCalled();
            expect(component.submitting).toBeFalse();
        });

        it('should update existing notification successfully', async () => {
            const existingNotification = { ...mockNewNotification, subscriptionId: '123' };
            component.issueForm.value.subscriptionId = '123';
            component.notifications = [existingNotification];

            await component.saveNotification();

            expect(mockApiService.updateNotification).toHaveBeenCalledWith('test-jira-id', {
                ...mockNewNotification,
                subscriptionId: '123'
            });
            expect(mockNotificationService.notifySuccess).toHaveBeenCalledWith('Notification updated successfully.');
            expect(mockAnalyticsService.sendUiEvent).toHaveBeenCalledWith(
                'configureChannelNotificationsModal',
                EventAction.clicked,
                UiEventSubject.button,
                'updateChannelNotification',
                { source: 'configureChannelNotificationsModal' }
            );
            expect(component.submitting).toBeFalse();
        });

        it('should handle API errors during creation', async () => {
            component.issueForm.value.subscriptionId = '';
            mockApiService.addNotification.and.returnValue(Promise.reject('API Error'));

            await component.saveNotification();

            expect(mockNotificationService.notifyError).toHaveBeenCalled();
            expect(component.submitting).toBeFalse();
        });

        it('should send notification subscription event after successful creation', async () => {
            component.issueForm.value.subscriptionId = '';

            await component.saveNotification();

            expect(mockApiService.sendNotificationSubscriptionEvent).toHaveBeenCalledWith({
                subscription: mockNewNotification,
                action: NotificationSubscriptionAction.Create
            });
        });
    });
});
