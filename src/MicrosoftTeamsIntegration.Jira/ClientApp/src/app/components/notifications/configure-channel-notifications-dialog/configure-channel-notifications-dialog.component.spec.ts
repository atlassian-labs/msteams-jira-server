import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfigureChannelNotificationsDialogComponent } from './configure-channel-notifications-dialog.component';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, AppInsightsService, UtilService } from '@core/services';
import { AnalyticsService } from '@core/services/analytics.service';
import { NotificationService } from '@shared/services/notificationService';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { of } from 'rxjs';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import { NotificationSubscriptionEvent, NotificationSubscriptionAction } from '@core/models/NotificationSubscriptionEvent';
import { Project } from '@core/models';
import * as microsoftTeams from '@microsoft/teams-js';

describe('ConfigureChannelNotificationsDialogComponent', () => {
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
        spyOn(microsoftTeams.dialog.url, 'submit').and.callFake(() => {});

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
            'getStatusesByProject'
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

        mockAppInsightsService = jasmine.createSpyObj('AppInsightsService', ['logNavigation']);
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
                { provide: AnalyticsService, useValue: mockAnalyticsService }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ConfigureChannelNotificationsDialogComponent);
        component = fixture.componentInstance;
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
});
