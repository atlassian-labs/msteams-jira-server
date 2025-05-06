import { Component, OnInit } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import {ApiService, UtilService} from '@core/services';
import { NotificationService } from '@shared/services/notificationService';
import * as microsoftTeams from '@microsoft/teams-js';
import {AnalyticsService, EventAction, UiEventSubject} from '@core/services/analytics.service';

@Component({
    selector: 'app-configure-personal-notifications-dialog',
    templateUrl: './configure-personal-notifications-dialog.component.html',
    styleUrls: ['./configure-personal-notifications-dialog.component.scss'],
    standalone: false
})
export class ConfigurePersonalNotificationsDialogComponent implements OnInit {
    public notificationsForm: UntypedFormGroup | undefined;
    public jiraId: string | any;
    public microsoftUserId: string | any;
    public conversationId: string | any;
    public conversationReferenceId: string | any;
    public savedNotificationSubscription: NotificationSubscription | any;
    public loading = false;
    public submitting = false;
    public isAddonUpdated = false;
    public replyToActivityId: string | any;

    constructor(
        private route: ActivatedRoute,
        private apiService: ApiService,
        private notificationService: NotificationService,
        private utilService: UtilService,
        private analyticsService: AnalyticsService
    ) { }

    public async ngOnInit() {
        const { jiraId, microsoftUserId, conversationId, conversationReferenceId, replyToActivityId } = this.route.snapshot.params;

        this.jiraId = jiraId;
        this.microsoftUserId = microsoftUserId;
        this.conversationId = conversationId;
        this.conversationReferenceId = conversationReferenceId;
        this.loading = true;
        this.replyToActivityId = replyToActivityId;

        this.analyticsService.sendScreenEvent(
            'configurePersonalNotificationsModal',
            EventAction.viewed,
            UiEventSubject.taskModule,
            'configurePersonalNotificationsModal', {});

        await this.createForm();

        await this.getNotificationSettings();
    }

    public async getNotificationSettings(): Promise<void> {
        try {
            const getAddonStatusPromise
                = this.apiService.getAddonStatus(this.jiraId);
            const getCurrentNotificationSettingsPromise
                = await this.apiService.getNotificationSettings(this.jiraId);
            const [{ addonVersion }, notificationSettings] = await Promise.all([
                getAddonStatusPromise,
                getCurrentNotificationSettingsPromise
            ]);
            this.isAddonUpdated
                = this.utilService.isAddonUpdatedToVersion(addonVersion, this.utilService.getMinAddonVersionForNotifications());
            if(!this.isAddonUpdated) {
                this.notificationService.notifyError(this.utilService.getUpgradeAddonMessageForNotifications(), 5000, false);
            }

            if (notificationSettings) {
                this.notificationsForm?.patchValue({
                    ActivityIssueAssignee: notificationSettings.eventTypes.includes('ActivityIssueAssignee'),
                    CommentIssueAssignee: notificationSettings.eventTypes.includes('CommentIssueAssignee'),
                    ActivityIssueCreator: notificationSettings.eventTypes.includes('ActivityIssueCreator'),
                    CommentIssueCreator: notificationSettings.eventTypes.includes('CommentIssueCreator'),
                    IssueViewer: notificationSettings.eventTypes.includes('IssueViewer'),
                    CommentViewer: notificationSettings.eventTypes.includes('CommentViewer'),
                    MentionedOnIssue: notificationSettings.eventTypes.includes('MentionedOnIssue')
                });

                this.savedNotificationSubscription = notificationSettings;
            }
        } catch (error) {
            this.notificationService.notifyError('Failed to load notification settings. Please try again.')
                .afterDismissed().subscribe(() => {
                    microsoftTeams.dialog.url.submit();
                });
        } finally {
            this.loading = false;
        }
    }

    public eventTypeSelected() {
        if (this.notificationsForm) {
            this.notificationsForm.markAsTouched();
        }
    }

    public async saveNotification(): Promise<void> {
        if (!this.notificationsForm?.valid) {
            return;
        }
        this.submitting = true;

        if (this.savedNotificationSubscription) {
            this.savedNotificationSubscription.eventTypes = this.getSelectedEventTypes();
            this.savedNotificationSubscription.microsoftUserId = this.microsoftUserId;
            this.savedNotificationSubscription.jiraId = this.jiraId;
            this.savedNotificationSubscription.conversationId = this.conversationId;
            this.savedNotificationSubscription.conversationReferenceId = this.conversationReferenceId;
            this.savedNotificationSubscription.isActive = true;

            await this.apiService.updateNotification(this.jiraId, this.savedNotificationSubscription);

            this.notificationService.notifySuccess('Notification updated successfully.').afterDismissed().subscribe(() => {
                microsoftTeams.dialog.url.submit({
                    commandName: 'showNotificationSettings',
                    replyToActivityId: this.replyToActivityId});
                this.submitting = false;
                microsoftTeams.dialog.url.submit();
            });
            this.analyticsService.sendUiEvent(
                'configurePersonalNotificationsModal',
                EventAction.clicked,
                UiEventSubject.button,
                'updatePersonalNotification',
                {source: 'configurePersonalNotificationsModal'});

            return;
        }

        const notification: NotificationSubscription = {
            jiraId: this.jiraId,
            subscriptionType: SubscriptionType.Personal,
            eventTypes: this.getSelectedEventTypes(),
            isActive: true,
            filter: '',
            microsoftUserId: this.microsoftUserId,
            conversationId: this.conversationId,
            conversationReferenceId: this.conversationReferenceId,
            projectId: '',
            projectName: ''
        };
        this.analyticsService.sendUiEvent(
            'configurePersonalNotificationsModal',
            EventAction.clicked,
            UiEventSubject.button,
            'createPersonalNotification',
            {source: 'configurePersonalNotificationsModal'});

        try {
            await this.apiService.addNotification(this.jiraId, notification);
            this.notificationService.notifySuccess('Notification saved successfully.').afterDismissed().subscribe(() => {
                microsoftTeams.dialog.url.submit({
                    commandName: 'showNotificationSettings',
                    replyToActivityId: this.replyToActivityId});
                this.submitting = false;
                microsoftTeams.dialog.url.submit();
            });
        } catch (error) {
            this.notificationService.notifyError('Failed to save notification. Please try again.').afterDismissed().subscribe(() => {
                this.submitting = false;
            });
        }
    }

    private getSelectedEventTypes(): string[] {
        const eventTypes: string[] = [];
        if (this.notificationsForm) {
            const formControls = this.notificationsForm.controls;

            for (const controlName in formControls) {
                if (formControls.hasOwnProperty(controlName)) {
                    const control = formControls[controlName];
                    if (control.value === true) {
                        eventTypes.push(controlName); // Use the control name as the event type
                    }
                }
            }
        }

        return eventTypes;
    }

    private async createForm(): Promise<void> {
        this.notificationsForm = new UntypedFormGroup({
            ActivityIssueAssignee: new UntypedFormControl(false),
            CommentIssueAssignee: new UntypedFormControl(false),
            ActivityIssueCreator: new UntypedFormControl(false),
            CommentIssueCreator: new UntypedFormControl(false),
            IssueViewer: new UntypedFormControl(false),
            CommentViewer: new UntypedFormControl(false),
            MentionedOnIssue: new UntypedFormControl(false)
        });
    }
}
