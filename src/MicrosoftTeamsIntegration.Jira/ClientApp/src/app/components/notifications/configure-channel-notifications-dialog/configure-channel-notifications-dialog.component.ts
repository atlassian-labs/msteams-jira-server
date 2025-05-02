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
    public selectedStatusOption: DropDownOption<string>[] = [];

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

    public commentIsSelectOptions: SelectOption[] = [
        { id: 'commentCreated', label: 'Created', value: 'CommentCreated' },
        { id: 'commentUpdated', label: 'Updated', value: 'CommentUpdated' },
    ];


    public jiraId: string | any;
    public microsoftUserId: string | any;
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
        private utilService: UtilService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.appInsightsService.logNavigation('ConfigureChannelNotifications', this.route);

        const { jiraId, microsoftUserId, conversationReferenceId, conversationId }
            = this.route.snapshot.params;

        this.jiraId = jiraId;
        this.microsoftUserId = microsoftUserId;
        this.conversationReferenceId = conversationReferenceId;
        this.conversationId = conversationId;

        this.notifications = await this.apiService.getAllNotificationsByConversationId(this.jiraId as string, this.conversationId);

        if (this.notifications && this.notifications.length > 0) {
            this.displayNotificationsListGroup();
        } else {
            this.displayInitialContainer();
        }
    }

    private async createForm(notification?: NotificationSubscription): Promise<void> {
        this.loading = true;

        this.projects = (await this.getProjects(this.jiraId as string)) as any;

        this.checkAddonUpdated();

        if (!this.projects || this.projects.length === 0) {
            const message = 'You don\'t have permission to perform this action';
            await this.router.navigate(['/error'], {queryParams: {message}});
            return;
        }

        this.availableProjectsOptions = this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
        this.projectFilteredOptions = this.availableProjectsOptions;

        let defaultProject: string = this.availableProjectsOptions[0]?.value || '';
        let defaultIssueType: string = this.availableIssueTypesOptions[0]?.value || '';
        let defaultStatus: string = this.statusesOptions[0]?.value || '';
        let defaultPriority: string = this.prioritiesOptions[0]?.value || '';
        this.issueIsSelectOptions = [
            { id: 'issueCreated', label: 'Created', value: 'IssueCreated' },
            { id: 'issueUpdated', label: 'Updated', value: 'IssueUpdated' },
        ];
        this.commentIsSelectOptions = [
            { id: 'commentCreated', label: 'Created', value: 'CommentCreated' },
            { id: 'commentUpdated', label: 'Updated', value: 'CommentUpdated' },
        ];

        this.addRemovePriorityFromForm();

        if (notification) {
            defaultProject = this.availableProjectsOptions.find(project => project.id === notification.projectId)?.value || defaultProject;
            await this.onProjectSelected(defaultProject);
            await this.setStatusesOptions();

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
                    defaultPriority = this.prioritiesOptions.find(opt => opt.id === priorityValue)?.value || defaultPriority;
                }

                const statusMatch = jqlParts.find(part => part.trim().startsWith('status in'));
                if (statusMatch) {
                    const statusValue = statusMatch.match(/"([^"]+)"/)?.[1];
                    defaultStatus = this.statusesOptions.find(opt => opt.label === statusValue)?.value || defaultStatus;
                }
            }

            this.issueIsSelectOptions = this.issueIsSelectOptions.filter(opt => notification.eventTypes.includes(opt.value as string));
            this.commentIsSelectOptions = this.commentIsSelectOptions.filter(opt => notification.eventTypes.includes(opt.value as string));
        } else {
            await this.onProjectSelected(defaultProject);
            await this.setStatusesOptions();
            this.addRemovePriorityFromForm();
        }

        // Create form with default values
        this.issueForm = new UntypedFormGroup({
            subscriptionId: new UntypedFormControl(notification?.subscriptionId),
            jiraId: new UntypedFormControl(this.jiraId, [Validators.required]),
            project: new UntypedFormControl(defaultProject),
            issuetype: new UntypedFormControl(defaultIssueType),
            status: new UntypedFormControl(defaultStatus),
            priority: new UntypedFormControl(defaultPriority),
            issueIs: new UntypedFormControl(this.issueIsSelectOptions),
            commentIs: new UntypedFormControl(this.commentIsSelectOptions)
        });

        this.loading = false;
    }

    public async onProjectSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const projectId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;

        this.selectedProject = this.projects?.find((proj: { id: string | null }) => proj.id === projectId);
        const projectKey = this.selectedProject?.key as string;
        const [issueTypesResult] = await Promise.all([
            this.apiService.getCreateMetaIssueTypes(this.jiraId as string, projectKey).catch(error => {
                console.error('Error fetching issue types:', error);
                return null;
            }),
        ]);

        if (issueTypesResult) {
            this.issueTypes = issueTypesResult;
            this.availableIssueTypesOptions = this.getIssueTypesOptions();
        } else {
            this.availableIssueTypesOptions = [this.DEFAULT_UNAVAILABLE_OPTION];
            const errorMessage = 'Failed to fetch issue types. Please try again later.';
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

            let jqlQuery = `project = ${selectedProject?.key}`;

            if (formValue.issuetype) {
                jqlQuery += ` AND type in ("${selectedIssueType?.name}")`;
            }

            if (formValue.priority) {
                jqlQuery += ` AND priority in ("${formValue.priority}")`;
            }

            if (formValue.status) {
                jqlQuery += ` AND status in ("${this.statusesOptions.find(opt => opt.id === formValue.status)?.label}")`;
            }

            const issueIsOptions = formValue.issueIs.map((option: string | SelectOption) =>
                typeof option === 'string'
                    ? this.notificationOptions.find(n => n.id === option)?.value
                    : this.notificationOptions.find(n => n.id === option.id)?.value);
            const commentIsOptions = formValue.commentIs.map((option: string | SelectOption) =>
                typeof option === 'string'
                    ? this.notificationOptions.find(n => n.id === option)?.value
                    : this.notificationOptions.find(n => n.id === option.id)?.value);

            const notificationSubscription: NotificationSubscription = {
                jiraId: this.jiraId,
                subscriptionType: SubscriptionType.Channel,
                projectId: selectedProject?.id,
                projectName: selectedProject?.name,
                filter: jqlQuery,
                conversationId: this.conversationId,
                conversationReferenceId: this.conversationReferenceId,
                eventTypes: [...issueIsOptions, ...commentIsOptions],
                microsoftUserId: this.microsoftUserId,
                isActive: true
            };

            const notifyMessage = this.issueForm.value.subscriptionId
                ? 'Notification updated successfully.'
                : 'Notification saved successfully.';

            if (this.issueForm.value.subscriptionId) {
                notificationSubscription.subscriptionId = this.issueForm.value.subscriptionId;
                await this.apiService.updateNotification(this.jiraId, notificationSubscription);
            } else {
                await this.apiService.addNotification(this.jiraId, notificationSubscription);
            }

            this.notificationService.notifySuccess(notifyMessage);

            this.displayNotificationsListGroup();
        }
    }

    public async deleteNotification(notification: NotificationSubscription): Promise<void> {
        await this.apiService.deleteNotification(this.jiraId as string, notification.subscriptionId as string);
        this.notificationService.notifySuccess('Notification deleted successfully.');
        this.displayNotificationsListGroup();
    }

    public async toggleNotification(notification: NotificationSubscription): Promise<void> {
        notification.isActive = !notification.isActive;
        await this.apiService.updateNotification(this.jiraId, notification);
        this.notificationService.notifySuccess('Notification successfully ' + (notification.isActive ? 'disabled' : 'enabled'));
        this.displayNotificationsListGroup();
    }

    public onCancel(): void {
        microsoftTeams.dialog.url.submit();
    }

    private async getProjects(jiraUrl: string): Promise<Project[] | null> {
        return await this.apiService.getProjects(jiraUrl, true);
    }

    private async findProjects(jiraUrl: string, filterName?: string): Promise<Project[]> {
        return await this.apiService.findProjects(jiraUrl, filterName, true);
    }

    private async setStatusesOptions(): Promise<void> {
        const jiraTransitionsArray = await this.transitionService
            .getTransitionsByProjectKeyOrId(this.jiraId as string, this.selectedProject?.id as string);
        const allTransitions = jiraTransitionsArray.flatMap(response => response.transitions);

        // Filter unique transitions based on their `id`
        const uniqueTransitions = allTransitions.filter(
            (transition, index, self) =>
                index === self.findIndex(t => t.id === transition.id)
        );

        this.statusesOptions = uniqueTransitions.map(this.dropdownUtilService.mapTransitionToDropdownOptionString);
        this.statusesOptions.unshift(this.ALL_OPTION);
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

    private async checkAddonUpdated(): Promise<void> {
        const addonVersion
                = await this.apiService.getAddonStatus(this.jiraId);

        const isAddonUpdated
            = this.utilService.isAddonUpdatedToVersion(addonVersion.addonVersion, this.utilService.getMinAddonVersionForNotifications());
        if(!isAddonUpdated) {
            this.notificationService.notifyError(this.utilService.getUpgradeAddonMessageForNotifications(), 20000, false);
        }
    }
}
