import { Component, OnInit } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import { ApiService } from '@core/services';
import { NotificationService } from '@shared/services/notificationService';
import * as microsoftTeams from '@microsoft/teams-js';

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

    constructor(
        private route: ActivatedRoute,
        private apiService: ApiService,
        private notificationService: NotificationService
    ) { }

    public async ngOnInit() {
        const { jiraId, microsoftUserId, conversationId, conversationReferenceId } = this.route.snapshot.params;

        this.jiraId = jiraId;
        this.microsoftUserId = microsoftUserId;
        this.conversationId = conversationId;
        this.conversationReferenceId = conversationReferenceId;

        await this.createForm();

        await this.getNotificationSettings();
    }

    public async getNotificationSettings(): Promise<void> {
        try {
            const response = await this.apiService.getNotificationSettings(this.jiraId, this.microsoftUserId);

            if (response) {
                this.notificationsForm?.patchValue({
                    ActivityIssueAssignee: response.eventTypes.includes('ActivityIssueAssignee'),
                    CommentIssueAssignee: response.eventTypes.includes('CommentIssueAssignee'),
                    ActivityIssueCreator: response.eventTypes.includes('ActivityIssueCreator'),
                    CommentIssueCreator: response.eventTypes.includes('CommentIssueCreator'),
                    IssueViewer: response.eventTypes.includes('IssueViewer'),
                    CommentViewer: response.eventTypes.includes('CommentViewer'),
                    MentionedOnIssue: response.eventTypes.includes('MentionedOnIssue')
                });

                this.savedNotificationSubscription = response;
            }
        } catch (error) {
            this.notificationService.notifyError('Failed to load notification settings. Please try again.')
                .afterDismissed().subscribe(() => {
                    microsoftTeams.dialog.url.submit();
                });
        }
    }

    public async saveNotification(): Promise<void> {
        if (!this.notificationsForm?.valid) {
            return;
        }

        if (this.savedNotificationSubscription) {
            this.savedNotificationSubscription.eventTypes = this.getSelectedEventTypes();
            this.savedNotificationSubscription.microsoftUserId = this.microsoftUserId;
            this.savedNotificationSubscription.jiraId = this.jiraId;
            this.savedNotificationSubscription.conversationId = this.conversationId;
            this.savedNotificationSubscription.conversationReferenceId = this.conversationReferenceId;

            await this.apiService.updateNotification(this.jiraId, this.savedNotificationSubscription);

            this.notificationService.notifySuccess('Notification updated successfully.').afterDismissed().subscribe(() => {
                microsoftTeams.dialog.url.submit();
            });

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
            projectId: ''
        };

        try {
            await this.apiService.addNotification(this.jiraId, notification);
            this.notificationService.notifySuccess('Notification saved successfully.').afterDismissed().subscribe(() => {
                microsoftTeams.dialog.url.submit();
            });
        } catch (error) {
            this.notificationService.notifyError('Failed to save notification. Please try again.').afterDismissed().subscribe(() => {
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
