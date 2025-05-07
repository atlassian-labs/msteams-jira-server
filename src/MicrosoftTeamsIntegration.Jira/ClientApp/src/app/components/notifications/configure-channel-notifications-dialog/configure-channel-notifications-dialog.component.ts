import { Component, OnInit, ViewChild } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { IssueType, Project } from '@core/models';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { ApiService, AppInsightsService, UtilService } from '@core/services';
import { ActivatedRoute, Router } from '@angular/router';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { NotificationService } from '@shared/services/notificationService';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { SelectOption } from '@shared/models/select-option.model';
import { NotificationSubscription, SubscriptionType } from '@core/models/NotificationSubscription';
import * as microsoftTeams from '@microsoft/teams-js';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';
import { NotificationSubscriptionEvent, NotificationSubscriptionAction } from '@core/models/NotificationSubscriptionEvent';

@Component({
    selector: 'app-configure-channel-notifications-dialog',
    templateUrl: './configure-channel-notifications-dialog.component.html',
    styleUrls: ['./configure-channel-notifications-dialog.component.scss'],
    standalone: false
})
export class ConfigureChannelNotificationsDialogComponent implements OnInit {
    // Loading and view state
    public loading = false;
    public showInitialContainer = false;
    public showCreateNotificationsGroup = false;
    public showNotificationsListGroup = false;

    public issueForm: UntypedFormGroup | any;

    // Project related
    public projects: Project[] | any;
    public availableProjectsOptions: DropDownOption<string>[] = [];
    public projectFilteredOptions: DropDownOption<string>[] = [];
    private selectedProject: Project | undefined;
    @ViewChild('projectsDropdown', { static: false }) projectsDropdown: DropDownComponent<string> | any;

    public fields: any;
    public issueTypes: IssueType[] | any;
    public availableIssueTypesOptions: DropDownOption<string>[] = [];
    private selectedIssueType: IssueType | undefined;
    public prioritiesOptions: DropDownOption<string>[] = [];
    public statusesOptions: DropDownOption<string>[] = [];

    // Notification options
    private notificationOptions: SelectOption[] = [
        { id: 'issueCreated', label: 'Issue Created', value: 'IssueCreated' },
        { id: 'issueUpdated', label: 'Issue Updated', value: 'IssueUpdated' },
        { id: 'commentCreated', label: 'Comment Created', value: 'CommentCreated' },
        { id: 'commentUpdated', label: 'Comment Updated', value: 'CommentUpdated' },
    ];

    public issueIsSelectOptions: SelectOption[] = [
        { id: 'issueCreated', label: 'Created', value: 'IssueCreated' },
        { id: 'issueUpdated', label: 'Updated', value: 'IssueUpdated' },
    ];
    public issueIsSelectOptionsSelected: SelectOption[] = [];

    public commentIsSelectOptions: SelectOption[] = [
        { id: 'commentCreated', label: 'Created', value: 'CommentCreated' },
        { id: 'commentUpdated', label: 'Updated', value: 'CommentUpdated' },
    ];

    public commentIsSelectOptionsSelected: SelectOption[] = [];


    public jiraId: string | any;
    public notifications: NotificationSubscription[] | any;
    public conversationReferenceId: string | any;
    public conversationId: string | any;

    private readonly DEFAULT_UNAVAILABLE_OPTION: DropDownOption<string> = {
        id: null,
        value: null,
        label: 'Unavailable'
    };

    private readonly ALL_OPTION: DropDownOption<string> = {
        id: 'all',
        value: 'all',
        label: 'All'
    };

    constructor(
        private apiService: ApiService,
        private transitionService: IssueTransitionService,
        private route: ActivatedRoute,
        private router: Router,
        private dropdownUtilService: DropdownUtilService,
        private notificationService: NotificationService,
        private appInsightsService: AppInsightsService,
        private utilService: UtilService,
        private analyticsService: AnalyticsService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.appInsightsService.logNavigation('ConfigureChannelNotifications', this.route);
        this.analyticsService.sendScreenEvent(
            'configureChannelNotificationsModal',
            EventAction.viewed,
            UiEventSubject.taskModule,
            'configureChannelNotificationsModal', {});

        const { jiraId, conversationReferenceId, conversationId }
            = this.route.snapshot.params;

        this.jiraId = jiraId;
        this.conversationReferenceId = conversationReferenceId;
        this.conversationId = conversationId;

        this.notifications = await this.apiService.getAllNotificationsByConversationId(this.jiraId as string, this.conversationId);

        if (this.notifications && this.notifications.length > 0) {
            await this.displayNotificationsListGroup();
        } else {
            this.displayInitialContainer();
        }
    }

    private async createForm(notification?: NotificationSubscription): Promise<void> {
        this.loading = true;

        this.projects = (await this.getProjects(this.jiraId as string)) as any;

        await this.checkAddonUpdated();

        if (!this.projects || this.projects.length === 0) {
            const message = 'You don\'t have permission to perform this action';
            await this.router.navigate(['/error'], {queryParams: {message}});
            return;
        }

        this.availableProjectsOptions = this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
        this.projectFilteredOptions = this.availableProjectsOptions;

        let defaultProjectOption: string = this.availableProjectsOptions[0]?.value || '';
        let defaultIssueTypeOption: string = this.availableIssueTypesOptions[0]?.value || '';
        let defaultStatusOption: string = this.statusesOptions[0]?.value || '';
        let defaultPriorityOption: string = this.prioritiesOptions[0]?.value || '';
        this.issueIsSelectOptionsSelected = [
            { id: 'issueCreated', label: 'Created', value: 'IssueCreated' },
            { id: 'issueUpdated', label: 'Updated', value: 'IssueUpdated' },
        ];
        this.commentIsSelectOptionsSelected = [
            { id: 'commentCreated', label: 'Created', value: 'CommentCreated' },
            { id: 'commentUpdated', label: 'Updated', value: 'CommentUpdated' },
        ];


        this.addRemovePriorityFromForm();

        if (notification) {
            const { defaultProject, defaultIssueType, defaultPriority, defaultStatus }
                = await this.initializeDefaultsForNotification(notification);
            defaultProjectOption = defaultProject;
            defaultIssueTypeOption = defaultIssueType;
            defaultStatusOption = defaultStatus;
            defaultPriorityOption = defaultPriority;

            this.issueIsSelectOptionsSelected
                = this.issueIsSelectOptions.filter(opt => notification.eventTypes.includes(opt.value as string));
            this.commentIsSelectOptionsSelected
                = this.commentIsSelectOptions.filter(opt => notification.eventTypes.includes(opt.value as string));
        } else {
            await this.onProjectSelected(defaultProjectOption);
            this.addRemovePriorityFromForm();
        }

        // Create form with default values
        this.issueForm = new UntypedFormGroup({
            subscriptionId: new UntypedFormControl(notification?.subscriptionId),
            jiraId: new UntypedFormControl(this.jiraId, [Validators.required]),
            project: new UntypedFormControl(defaultProjectOption),
            issuetype: new UntypedFormControl(defaultIssueTypeOption),
            status: new UntypedFormControl(defaultStatusOption),
            priority: new UntypedFormControl(defaultPriorityOption),
            issueIs: new UntypedFormControl(this.issueIsSelectOptions),
            commentIs: new UntypedFormControl(this.commentIsSelectOptions)
        });

        this.loading = false;
    }

    public async onProjectSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const projectId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;

        this.selectedProject = this.projects?.find((proj: { id: string | null }) => proj.id === projectId);
        const projectKey = this.selectedProject?.key as string;
        const [issueTypesResult, statusesResult] = await Promise.all([
            this.apiService.getCreateMetaIssueTypes(this.jiraId as string, projectKey).catch(error => {
                console.error('Error fetching issue types:', error);
                return null;
            }),
            await this.apiService.getStatusesByProject(this.jiraId as string, projectId as string).catch(error => {
                console.error('Error fetching statuses:', error);
                return null;
            })
        ]);

        if (issueTypesResult) {
            this.issueTypes = issueTypesResult;
            this.availableIssueTypesOptions = this.getIssueTypesOptions();
        } else {
            this.availableIssueTypesOptions = [this.DEFAULT_UNAVAILABLE_OPTION];
            const errorMessage = 'Failed to fetch issue types. Please try again later.';
            this.notificationService.notifyError(errorMessage);
        }

        if(statusesResult) {
            this.statusesOptions = statusesResult.map(this.dropdownUtilService.mapStatusToDropdownOption);
            this.statusesOptions.unshift(this.ALL_OPTION);
        } else {
            this.statusesOptions = [this.DEFAULT_UNAVAILABLE_OPTION];
            const errorMessage = 'Failed to fetch statuses. Please try again later.';
            this.notificationService.notifyError(errorMessage);
        }

        await this.onIssueTypeSelected(this.availableIssueTypesOptions[0]);
    }

    public async onIssueTypeSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const issueTypeId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;

        if (issueTypeId) {
            this.selectedIssueType = this.issueTypes?.find((issueType: { id: string }) => issueType.id === issueTypeId);

            this.fields = await this.apiService.getCreateMetaFields(
                this.jiraId as string,
                this.selectedProject?.key as string,
                this.selectedIssueType?.id as string,
                this.selectedIssueType?.name as string);

            this.addRemovePriorityFromForm();
        }
    }

    public compareSelectOptions(option1: SelectOption, option2: SelectOption): boolean {
        return option1 && option2 ? option1.id === option2.id : option1 === option2;
    }

    public async saveNotification(): Promise<void> {
        if (this.issueForm.valid) {
            const formValue = this.issueForm.value;
            const selectedProject = this.projects.find((project: { id: string }) => project.id === formValue.project);
            const selectedIssueType = this.issueTypes.find((issueType: { id: string }) => issueType.id === formValue.issuetype);
            const selectedPriority = this.prioritiesOptions.find((priority) => priority.id === formValue?.priority)?.label;
            const selectedStatus = this.statusesOptions.find(opt => opt.id === formValue.status)?.label;

            const jqlQuery = this.buildJqlQuery(
                formValue,
                selectedProject?.key,
                selectedIssueType?.name,
                selectedPriority,
                selectedStatus);

            const issueIsOptions = this.getMappedNotificationTypeOptions(formValue.issueIs);
            const commentIsOptions = this.getMappedNotificationTypeOptions(formValue.commentIs);

            const notificationSubscription: NotificationSubscription = {
                jiraId: this.jiraId,
                subscriptionType: SubscriptionType.Channel,
                projectId: selectedProject?.id,
                projectName: selectedProject?.name,
                filter: jqlQuery,
                microsoftUserId: '',
                conversationId: this.conversationId,
                conversationReferenceId: this.conversationReferenceId,
                eventTypes: [...issueIsOptions, ...commentIsOptions],
                isActive: true
            };

            const isDuplicate = this.notifications
                .filter((notification: NotificationSubscription) =>
                    notification.subscriptionId !== this.issueForm.value.subscriptionId)
                .find((subscription: NotificationSubscription) => this.areNotificationsEqual(subscription, notificationSubscription));

            if (isDuplicate) {
                this.notificationService.notifyError(
                    'The subscription with the same configuration already exists. Please use a different configuration.');
                return;
            }

            const notifyMessage = this.issueForm.value.subscriptionId
                ? 'Notification updated successfully.'
                : 'Notification saved successfully.';

            let notificationSubscriptionEvent: NotificationSubscriptionEvent | undefined;

            if (this.issueForm.value.subscriptionId) {
                notificationSubscription.subscriptionId = this.issueForm.value.subscriptionId;
                await this.apiService.updateNotification(this.jiraId, notificationSubscription);

                notificationSubscriptionEvent = {
                    subscription: notificationSubscription,
                    action: NotificationSubscriptionAction.Update
                };
                this.analyticsService.sendUiEvent(
                    'configureChannelNotificationsModal',
                    EventAction.clicked,
                    UiEventSubject.button,
                    'updateChannelNotification',
                    {source: 'configureChannelNotificationsModal'});
            } else {
                await this.apiService.addNotification(this.jiraId, notificationSubscription);

                notificationSubscriptionEvent = {
                    subscription: notificationSubscription,
                    action: NotificationSubscriptionAction.Create
                };
                this.analyticsService.sendUiEvent(
                    'configureChannelNotificationsModal',
                    EventAction.clicked,
                    UiEventSubject.button,
                    'createChannelNotification',
                    {source: 'configureChannelNotificationsModal'});
            }

            this.notificationService.notifySuccess(notifyMessage);

            await this.displayNotificationsListGroup();

            await this.apiService.sendNotificationSubscriptionEvent(notificationSubscriptionEvent);
        }
    }

    public async deleteNotification(notification: NotificationSubscription): Promise<void> {
        this.analyticsService.sendUiEvent(
            'configureChannelNotificationsModal',
            EventAction.clicked,
            UiEventSubject.button,
            'deleteChannelNotification',
            {source: 'configureChannelNotificationsModal'});
        await this.apiService.deleteNotification(this.jiraId as string, notification.subscriptionId as string);
        this.notificationService.notifySuccess('Notification deleted successfully.');
        await this.displayNotificationsListGroup();

        const notificationSubscriptionEvent: NotificationSubscriptionEvent = {
            subscription: notification,
            action: NotificationSubscriptionAction.Delete
        };

        await this.apiService.sendNotificationSubscriptionEvent(notificationSubscriptionEvent);
    }

    public async toggleNotification(notification: NotificationSubscription): Promise<void> {
        this.analyticsService.sendUiEvent(
            'configureChannelNotificationsModal',
            EventAction.clicked,
            UiEventSubject.button,
            !notification.isActive ? 'muteChannelNotification' : 'unmuteChannelNotification',
            {source: 'configureChannelNotificationsModal'});
        notification.isActive = !notification.isActive;
        await this.apiService.updateNotification(this.jiraId, notification);
        this.notificationService.notifySuccess('Notification successfully ' + (notification.isActive ? 'enabled' : 'disabled'));
        await this.displayNotificationsListGroup();

        const notificationSubscriptionEvent: NotificationSubscriptionEvent = {
            subscription: notification,
            action: notification.isActive ? NotificationSubscriptionAction.Enabled : NotificationSubscriptionAction.Disabled
        };

        await this.apiService.sendNotificationSubscriptionEvent(notificationSubscriptionEvent);
    }

    public onCancel(): void {
        microsoftTeams.dialog.url.submit();
    }

    public displayInitialContainer(): void {
        this.showInitialContainer = true;
        this.showCreateNotificationsGroup = false;
        this.showNotificationsListGroup = false;
    }

    public async displayCreateNotificationsGroup(): Promise<void> {
        this.showInitialContainer = false;
        this.showCreateNotificationsGroup = true;
        this.showNotificationsListGroup = false;

        await this.createForm();
    }

    public async displayNotificationsListGroup(): Promise<void> {
        this.notifications = await this.apiService.getAllNotificationsByConversationId(this.jiraId as string, this.conversationId);

        if (this.notifications && this.notifications.length > 0) {
            this.showInitialContainer = false;
            this.showCreateNotificationsGroup = false;
            this.showNotificationsListGroup = true;
        } else {
            this.displayInitialContainer();
        }
    }

    public async displayCreateNotificationsGroupWithNotification(notification: NotificationSubscription): Promise<void> {
        this.showInitialContainer = false;
        this.showCreateNotificationsGroup = true;
        this.showNotificationsListGroup = false;

        await this.createForm(notification);
    }

    private async getProjects(jiraUrl: string): Promise<Project[] | null> {
        return await this.apiService.getProjects(jiraUrl, true);
    }

    private addRemovePriorityFromForm(): void {
        if (!this.fields || Object.keys(this.fields).length === 0) {
            this.prioritiesOptions = [this.ALL_OPTION];
            return;
        }

        const priorities = this.fields.priority;

        const priorityControlName = 'priority';

        if (priorities) {
            const prioritiesOptions = priorities.allowedValues.map(this.dropdownUtilService.mapPriorityToDropdownOption);
            prioritiesOptions.unshift(this.ALL_OPTION);
            this.prioritiesOptions = prioritiesOptions;
        } else if (this.issueForm.contains(priorityControlName)) {
            this.issueForm.removeControl(priorityControlName);
            this.prioritiesOptions = [];
        }
    }

    private getIssueTypesOptions(): DropDownOption<string>[] {
        if (this.issueTypes && this.issueTypes.length > 0) {
            const issueTypes = this.issueTypes
                .map(this.dropdownUtilService.mapIssueTypeToDropdownOption);
            issueTypes.unshift(this.ALL_OPTION);

            return issueTypes;
        }
        return [this.DEFAULT_UNAVAILABLE_OPTION];
    }

    private buildJqlQuery(
        formValue: any,
        projectKey: string | undefined,
        issueTypeName: string | undefined,
        priority: string | undefined,
        status: string | undefined
    ): string {
        let jqlQuery = `project = "${projectKey}"`;

        if (formValue.issuetype && formValue.issuetype !== this.ALL_OPTION.value) {
            jqlQuery += ` AND type in ("${issueTypeName}")`;
        }

        if (formValue.priority && formValue.priority !== this.ALL_OPTION.value) {
            jqlQuery += ` AND priority in ("${priority}")`;
        }

        if (formValue.status && formValue.status !== this.ALL_OPTION.value) {
            jqlQuery += ` AND status in ("${status}")`;
        }

        return jqlQuery;
    }

    private getMappedNotificationTypeOptions(options: (string | SelectOption)[]): any {
        return options.map((option: string | SelectOption) =>
            typeof option === 'string'
                ? this.notificationOptions.find(n => n.id === option)?.value
                : this.notificationOptions.find(n => n.id === option.id)?.value
        );
    }

    private async initializeDefaultsForNotification(notification: NotificationSubscription):
    Promise<{ defaultProject: string; defaultIssueType: string; defaultPriority: string; defaultStatus: string }> {
        const defaultProject = this.availableProjectsOptions.find(project => project.id === notification.projectId)?.value || '';
        await this.onProjectSelected(defaultProject);

        let defaultIssueType = this.availableIssueTypesOptions[0]?.value || '';
        let defaultPriority = this.prioritiesOptions[0]?.value || '';
        let defaultStatus = this.statusesOptions[0]?.value || '';

        if (notification.filter) {
            const jqlParts = notification.filter.split(' AND ');

            const typeMatch = jqlParts.find(part => part.trim().startsWith('type in'));
            if (typeMatch) {
                const typeValue = typeMatch.match(/"([^"]+)"/)?.[1];
                defaultIssueType = this.availableIssueTypesOptions.find(opt => opt.label === typeValue)?.value || defaultIssueType;
                await this.onIssueTypeSelected(defaultIssueType);
            }

            const priorityMatch = jqlParts.find(part => part.trim().startsWith('priority in'));
            if (priorityMatch) {
                const priorityValue = priorityMatch.match(/"([^"]+)"/)?.[1];
                defaultPriority = this.prioritiesOptions.find(opt => opt.label === priorityValue)?.value || defaultPriority;
            }

            const statusMatch = jqlParts.find(part => part.trim().startsWith('status in'));
            if (statusMatch) {
                const statusValue = statusMatch.match(/"([^"]+)"/)?.[1];
                defaultStatus = this.statusesOptions.find(opt => opt.label === statusValue)?.value || defaultStatus;
            }
        }

        return { defaultProject, defaultIssueType, defaultPriority, defaultStatus };
    }

    private async checkAddonUpdated(): Promise<void> {
        const addonVersion
                = await this.apiService.getAddonStatus(this.jiraId);

        const isAddonUpdated
            = this.utilService.isAddonUpdatedToVersion(addonVersion.addonVersion, this.utilService.getMinAddonVersionForNotifications());
        if(!isAddonUpdated) {
            this.notificationService.notifyError(this.utilService.getUpgradeAddonMessageForNotifications(), 5000, false);
        }
    }

    private areNotificationsEqual(
        notification1: NotificationSubscription,
        notification2: NotificationSubscription
    ): boolean {
        return notification1.jiraId === notification2.jiraId &&
            notification1.subscriptionType === notification2.subscriptionType &&
            notification1.conversationId === notification2.conversationId &&
            JSON.stringify(notification1.eventTypes) === JSON.stringify(notification2.eventTypes) &&
            notification1.projectId === notification2.projectId &&
            notification1.projectName === notification2.projectName &&
            notification1.filter === notification2.filter;
    }
}
