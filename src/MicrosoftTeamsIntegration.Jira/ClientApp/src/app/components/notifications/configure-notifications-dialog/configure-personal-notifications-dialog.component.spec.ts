import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfigurePersonalNotificationsDialogComponent } from './configure-personal-notifications-dialog.component';
import { ActivatedRoute } from '@angular/router';
import {ApiService, UtilService} from '@core/services';
import { NotificationService } from '@shared/services/notificationService';
import { of } from 'rxjs';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import * as microsoftTeams from '@microsoft/teams-js';

describe('ConfigurePersonalNotificationsDialogComponent', () => {
    let component: ConfigurePersonalNotificationsDialogComponent;
    let fixture: ComponentFixture<ConfigurePersonalNotificationsDialogComponent>;
    let mockApiService: jasmine.SpyObj<ApiService>;
    let mockNotificationService: jasmine.SpyObj<NotificationService>;
    let mockUtilsService: jasmine.SpyObj<UtilService>;
    let mockActivatedRoute: any;

    const mockNotificationSubscription: NotificationSubscription = {
        jiraId: 'test-jira-id',
        subscriptionType: SubscriptionType.Personal,
        eventTypes: ['activityAssignee', 'commentsAssignee'],
        isActive: true,
        filter: '',
        microsoftUserId: 'test-user-id',
        conversationId: 'test-conversation-id',
        conversationReferenceId: 'test-reference-id',
        projectId: '',
        projectName: ''
    };

    beforeEach(async () => {
        spyOn(microsoftTeams.dialog.url, 'submit').and.callFake(() => {});

        mockApiService = jasmine.createSpyObj('ApiService', [
            'getNotificationSettings',
            'updateNotification',
            'addNotification',
            'getAddonStatus',
        ]);

        mockNotificationService = jasmine.createSpyObj('NotificationService', [
            'notifySuccess',
            'notifyError'
        ]);

        mockUtilsService = jasmine.createSpyObj('UtilService', [
            'isAddonUpdatedToVersion',
            'getMinAddonVersionForNotifications'
        ]);

        const mockSnackBarRef = jasmine.createSpyObj('MatSnackBarRef', ['afterDismissed']);
        mockSnackBarRef.afterDismissed.and.returnValue(of({ dismissedByAction: false }));

        mockNotificationService.notifySuccess.and.returnValue(mockSnackBarRef);
        mockNotificationService.notifyError.and.returnValue(mockSnackBarRef);

        mockUtilsService.isAddonUpdatedToVersion.and.returnValue(true);
        mockApiService.getAddonStatus.and.returnValue(Promise.resolve({ addonStatus: 1, addonVersion: '1.0.0' }));

        mockActivatedRoute = {
            snapshot: {
                params: {
                    jiraId: 'test-jira-id',
                    microsoftUserId: 'test-user-id',
                    conversationId: 'test-conversation-id',
                    conversationReferenceId: 'test-reference-id'
                }
            }
        };

        await TestBed.configureTestingModule({
            declarations: [ConfigurePersonalNotificationsDialogComponent],
            providers: [
                {provide: ApiService, useValue: mockApiService},
                {provide: NotificationService, useValue: mockNotificationService},
                {provide: ActivatedRoute, useValue: mockActivatedRoute},
                {provide: UtilService, useValue: mockUtilsService},
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ConfigurePersonalNotificationsDialogComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('ngOnInit', () => {
        it('should initialize component with route parameters', async () => {
            await component.ngOnInit();

            expect(component.jiraId).toBe('test-jira-id');
            expect(component.microsoftUserId).toBe('test-user-id');
            expect(component.conversationId).toBe('test-conversation-id');
            expect(component.conversationReferenceId).toBe('test-reference-id');
            expect(component.notificationsForm).toBeDefined();
        });
    });

    describe('getNotificationSettings', () => {
        it('should load and set notification settings successfully', async () => {
            mockApiService.getNotificationSettings.and.returnValue(Promise.resolve({
                eventTypes: ['ActivityIssueAssignee', 'CommentIssueAssignee'],
                jiraId: 'test-jira-id',
                subscriptionType: SubscriptionType.Personal,
                isActive: true,
                filter: '',
                microsoftUserId: 'test-user-id',
                conversationId: 'test-conversation-id',
                conversationReferenceId: 'test-reference-id',
                projectId: '',
                projectName: ''
            }));

            await component.ngOnInit();

            expect(mockApiService.getNotificationSettings).toHaveBeenCalledWith('test-jira-id');
            expect(component.notificationsForm?.get('ActivityIssueAssignee')?.value).toBeTrue();
            expect(component.notificationsForm?.get('CommentIssueAssignee')?.value).toBeTrue();
        });

        it('should handle error when loading notification settings', async () => {
            mockApiService.getNotificationSettings.and.returnValue(Promise.reject('Error'));

            await component.ngOnInit();
            await component.getNotificationSettings();

            expect(mockNotificationService.notifyError).toHaveBeenCalledWith('Failed to load notification settings. Please try again.');
        });
    });

    describe('saveNotification', () => {
        beforeEach(async () => {
            await component.ngOnInit();
        });

        it('should not save if form is invalid', async () => {
            component.notificationsForm?.setErrors({invalid: true});
            await component.saveNotification();
            expect(mockApiService.updateNotification).not.toHaveBeenCalled();
            expect(mockApiService.addNotification).not.toHaveBeenCalled();
        });

        it('should update existing notification', async () => {
            component.savedNotificationSubscription = mockNotificationSubscription;
            mockApiService.updateNotification.and.returnValue(Promise.resolve());

            await component.saveNotification();

            expect(mockApiService.updateNotification).toHaveBeenCalled();
            expect(mockNotificationService.notifySuccess).toHaveBeenCalledWith('Notification updated successfully.');
        });

        it('should create new notification', async () => {
            mockApiService.addNotification.and.returnValue(Promise.resolve());

            await component.saveNotification();

            expect(mockApiService.addNotification).toHaveBeenCalled();
            expect(mockNotificationService.notifySuccess).toHaveBeenCalledWith('Notification saved successfully.');
        });

        it('should handle error when saving notification', async () => {
            mockApiService.addNotification.and.returnValue(Promise.reject('Error'));

            await component.saveNotification();

            expect(mockNotificationService.notifyError).toHaveBeenCalledWith('Failed to save notification. Please try again.');
        });
    });

    describe('getSelectedEventTypes', () => {
        it('should return selected event types', async () => {
            await component.ngOnInit();
            component.notificationsForm?.patchValue({
                ActivityIssueAssignee: true,
                CommentIssueAssignee: true,
                ActivityIssueCreator: false
            });

            const eventTypes = component['getSelectedEventTypes']();
            expect(eventTypes).toContain('ActivityIssueAssignee');
            expect(eventTypes).toContain('CommentIssueAssignee');
            expect(eventTypes).not.toContain('ActivityIssueCreator');
        });
    });
});
