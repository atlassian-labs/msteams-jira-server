import { AbstractControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { ApiService, AppInsightsService } from '@core/services';
import { Component, OnInit } from '@angular/core';
import { CurrentJiraUser, JiraUser, UserGroup } from '@core/models/Jira/jira-user.model';
import { Issue, IssueFields, IssueType, Priority, ProjectType } from '@core/models';
import { IssueStatus, JiraComment } from '@core/models';
import { JiraPermissionName, JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { MatDialog } from '@angular/material/dialog';

import { ActivatedRoute, Router } from '@angular/router';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { DomSanitizer } from '@angular/platform-browser';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { EditIssueMetadata } from '@core/models/Jira/jira-issue-edit-meta.model';
import { HttpErrorResponse } from '@angular/common/http';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { JiraTransition } from '@core/models/Jira/jira-transition.model';
import { PermissionService } from '@core/services/entities/permission.service';
import { SearchAssignableOptions } from '@core/models/Jira/search-assignable-options';
import { StringValidators } from '@core/validators/string.validators';
import { UtilService } from '@core/services/util.service';
import { NotificationService } from '@shared/services/notificationService';
import { FieldsService } from '@shared/services/fields.service';
import { FieldItem } from '@app/components/issues/fields/field-item';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';

import {TeamsService} from '@core/services/teams.service';
interface EditIssueModel {
    key: string;
    id: string;
    issueTypeIconUrl: string;
    summary: string;
    description: string;
    status: IssueStatus;
    created: string;
    updated: string;
    assignee: JiraUser | null;
    reporter: JiraUser;
    priorityId: string;
    comment: {
        comments: JiraComment[];
        maxResults: number;
        total: number;
        startAt: number;
    };
    projectKey: string;
    projectTypeKey: string;
}

const mapIssueToEditIssueDialogModel = (issue: Issue): EditIssueModel => ({
    key: issue.key,
    id: issue.id,
    issueTypeIconUrl: issue.fields.issuetype.iconUrl,
    projectKey: issue.fields.project.key,
    priorityId: (issue.fields.priority && issue.fields.priority.id) || null,
    priority: issue.fields.priority,
    summary: issue.fields.summary || '',
    description: issue.fields.description || '',
    created: issue.fields.created,
    updated: issue.fields.updated,
    reporter: issue.fields.reporter,
    assignee: issue.fields.assignee,
    comment: issue.fields.comment,
    status: issue.fields.status,
    projectTypeKey: issue.fields.project.projectTypeKey
} as EditIssueModel);

@Component({
    selector: 'app-edit-issue-dialog',
    templateUrl: './edit-issue-dialog.component.html',
    styleUrls: ['./edit-issue-dialog.component.scss'],
    standalone: false
})
export class EditIssueDialogComponent implements OnInit {
    public loading = false;
    public uploading = false;

    public error: HttpErrorResponse | Error | any;
    public issue: EditIssueModel | any;
    public issueRaw: Issue | any;
    public currentUser: CurrentJiraUser | any;
    public permissions: JiraPermissions | any;

    public jiraUrl: string | any;
    public issueId: string | any;
    public issueKey: string | any;
    public issueForm: UntypedFormGroup | any;
    public updatedFormFields: string[] = [];
    public replyToActivityId: string | any;

    public initialIssueForm: any;
    public errorMessage: string | any;

    // priority
    public prioritiesOptions: DropDownOption<string>[] = [];
    public selectedPriorityOption: DropDownOption<string> | any;

    // assignee
    public assigneesOptions: DropDownOption<string>[] | any;
    public assigneesFilteredOptions: DropDownOption<string>[] | any;
    public selectedAssigneeOption: DropDownOption<string> | any;
    public assigneesLoading = false;

    // status
    public statusesOptions: DropDownOption<JiraTransition>[] | any;
    public selectedStatusOption: DropDownOption<JiraTransition> | any;

    public currentUserAccountId: string | any;

    private editIssueMetadata: EditIssueMetadata | any;
    private notAssignableAssignee: JiraUser | any;

    private summaryFieldName = 'summary';
    private descriptionFieldName = 'description';
    private priorityFieldName = 'priorityId';
    private assigneeFieldName = 'assigneeAccountId';
    private statusFieldName = 'status';
    private selectedIssueType: IssueType | undefined;
    public fields: any;
    public defaultPriority: string | any;
    public dynamicFieldsData: FieldItem[] | any;

    public application: string | any;

    private readonly DEFAULT_UNAVAILABLE_OPTION: DropDownOption<string> = {
        id: null,
        value: null,
        label: 'Unavailable'
    };

    protected readonly Object = Object;

    constructor(public dialog: MatDialog,
        public domSanitizer: DomSanitizer,
        private apiService: ApiService,
        private permissionService: PermissionService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService,
        private route: ActivatedRoute,
        private dropdownUtilService: DropdownUtilService,
        private assigneeService: AssigneeService,
        private transitionService: IssueTransitionService,
        private router: Router,
        private notificationService: NotificationService,
        private fieldsService: FieldsService,
        private analyticsService: AnalyticsService,
        private teamsService: TeamsService
    ) { }

    public async ngOnInit(): Promise<void> {

        this.appInsightsService.logNavigation('EditIssueComponent', this.route);

        this.loading = true;

        try {
            const { jiraUrl, issueId, issueKey, replyToActivityId, application } = this.route.snapshot.params;
            this.jiraUrl = jiraUrl;
            this.issueId = issueId;
            this.issueKey = issueKey;
            this.replyToActivityId = replyToActivityId;
            this.application = application;

            this.analyticsService.sendScreenEvent(
                'editIssueModal',
                EventAction.viewed,
                UiEventSubject.taskModule,
                'editIssueModal', { application });

            const issueRelatedPermissions: JiraPermissionName[] = [
                'EDIT_ISSUES', 'ASSIGN_ISSUES', 'ASSIGNABLE_USER', 'TRANSITION_ISSUES', 'ADD_COMMENTS',
                'EDIT_OWN_COMMENTS', 'EDIT_ALL_COMMENTS', 'BROWSE'
            ];

            // if there is no jiraUrl  - it means we are in bot. That's why we need get the jiraUrl for personal scope
            if (!this.jiraUrl) {
                const response = await this.apiService.getJiraUrlForPersonalScope();
                this.jiraUrl = response.jiraUrl;
            }
            const { permissions } = await this.permissionService
                .getMyPermissions(this.jiraUrl, issueRelatedPermissions, this.issueId);
            this.permissions = permissions;

            if (!this.canEditIssue && !this.canViewIssue) {
                const message = 'You don\'t have permissions to perform this action';
                await this.router.navigate(['/error'], { queryParams: { message } });
                return;
            }

            const issuePromise = this.apiService.getIssueByIdOrKey(this.jiraUrl, this.issueId as string);
            const editIssueMetadataPromise = this.apiService.getEditIssueMetadata(this.jiraUrl, this.issueId as string);
            const currentUserPromise = this.apiService.getCurrentUserData(this.jiraUrl);

            const [issue, editIssueMetadata, currentUser] = await Promise.all([
                issuePromise,
                editIssueMetadataPromise,
                currentUserPromise
            ]);

            this.issueRaw = issue;
            this.editIssueMetadata = editIssueMetadata;
            this.issue = mapIssueToEditIssueDialogModel(issue);
            this.currentUser = currentUser;
            this.currentUserAccountId = this.currentUser.name;

            const optionPromises = [];

            if (this.allowEditPriority) {
                optionPromises.push(this.setPrioritiesOptions());
            }

            if (this.allowEditAssignee) {
                optionPromises.push(this.setAssigneeOptions());
            }

            if (this.allowEditStatus) {
                optionPromises.push(this.setStatusesOptions());
            }

            await Promise.all(optionPromises);

            await this.createForm();

            this.teamsService.notifySuccess();
        } catch (error) {
            this.error = error as any;
            this.appInsightsService.trackException(
                new Error(error as any),
                'EditIssueDialogComponent::ngOnInit',
                this.issue
            );
        }

        this.loading = false;
    }

    public get summary(): AbstractControl | any {
        return this.issueForm?.get(this.summaryFieldName);
    }

    public get assigneeAccountId(): AbstractControl | any {
        return this.issueForm?.get(this.assigneeFieldName);
    }

    public get priorityId(): AbstractControl | any {
        return this.issueForm?.get(this.priorityFieldName);
    }

    public get keyLink(): string {
        const jiraServerInstanceUrl = this.currentUser?.jiraServerInstanceUrl || this.jiraUrl;
        return encodeURI(`${jiraServerInstanceUrl}/browse/${this.issue?.key}`);
    }

    public get canEditIssue(): boolean | undefined {
        return this.permissions?.EDIT_ISSUES.havePermission;
    }

    public get canViewIssue(): boolean | undefined {
        return this.permissions?.BROWSE.havePermission;
    }

    public get allowEditSummary(): boolean | undefined {
        return this.permissions?.EDIT_ISSUES.havePermission && this.canEditField('summary');
    }

    public get allowEditDescription(): boolean | undefined {
        return this.permissions?.EDIT_ISSUES.havePermission && this.canEditField('description');
    }

    public get allowEditPriority(): boolean | undefined {
        return this.permissions?.EDIT_ISSUES.havePermission && this.canEditField('priority');
    }

    public get allowEditAssignee(): boolean | undefined {
        // for JSM projects user should be a member of 'jira-servicedesk-users' group in order to get assignees,
        // even with ASSIGN_ISSUES project permission
        if (this.issue?.projectTypeKey === ProjectType.ServiceDesk) {
            return this.currentUser?.groups?.items.some((x: { name: UserGroup }) =>
                x.name === UserGroup.JiraServicedeskUsers) &&
                this.permissions?.ASSIGN_ISSUES.havePermission;
        }
        return this.permissions.ASSIGN_ISSUES.havePermission && this.issue.assignee !== undefined;
    }

    public get allowEditStatus(): boolean | undefined {
        return this.permissions?.TRANSITION_ISSUES.havePermission;
    }

    /**
     * @returns {true} if the field is in the edit issue metadata
     */
    public canEditField = (fieldName: string): boolean | undefined => !!(this.editIssueMetadata?.fields[fieldName]);

    public assignToMe(): void {
        this.assigneeAccountId.setValue(this.currentUserAccountId);
    }

    public async onAssigneeSearchChanged(username: string): Promise<void> {
        this.assigneesFilteredOptions = await this.getAssigneesOptions(username);
    }

    public onNewCommentCreated(comment: JiraComment): void {
        this.issue?.comment.comments.push(comment);
    }

    public async onSubmit(): Promise<void> {
        if (this.issueForm?.invalid) {
            return;
        }

        this.analyticsService.sendUiEvent('editIssueModal',
            EventAction.clicked,
            UiEventSubject.button,
            'editIssueInJira',
            {source: 'editIssueModal', application: this.application});

        const formValue = this.issueForm?.value;
        const editIssueModel = {
            priority: { } as Partial<IssueStatus>,
            status: { } as Partial<Priority>,
            fields: { } as Partial<any>,
            editIssueMetadata: { } as Partial<any>
        } as Partial<IssueFields>;

        if (this.updatedFormFields.indexOf(this.statusFieldName) !== -1) {
            (editIssueModel['status'] as IssueStatus).id = formValue.status.id;
        }

        if (this.updatedFormFields.indexOf(this.priorityFieldName) !== -1) {
            (editIssueModel['priority'] as Priority).id = formValue.priorityId;
        }

        // if user has permissions to assign the issue - pass even null as a value for accountId (it means Unassigned)
        if (this.updatedFormFields.indexOf(this.assigneeFieldName) !== -1) {
            editIssueModel['assignee'] = {
                name: formValue.assigneeAccountId
            } as JiraUser;
        }

        Object.entries(this.fieldsService.getAllowedTransformedFields(this.fields, formValue)).forEach(([key, value]) => {
            if (this.updatedFormFields.includes(key)) {
                editIssueModel.fields[key] = value;
            }
        });

        editIssueModel.editIssueMetadata = this.editIssueMetadata.fields;

        try {
            this.uploading = true;

            const response = await this.apiService.updateIssue(
                encodeURIComponent(this.jiraUrl as string),
                this.issue?.id as string,
                editIssueModel);

            if (response.isSuccess) {
                this.showConfirmationNotification();
                return;
            } else {
                this.notificationService.notifyError(response.errorMessage ||
                    'Something went wrong. Please check your permission to perform this type of action.');
                this.uploading = false;
            }
        } catch (error) {
            this.notificationService.notifyError((error as any).errorMessage ||
                'Something went wrong. Please try again or contact support.');
            this.uploading = false;
        }
    }

    public onCancel(): void {
        this.teamsService.submitDialog();
    }

    public onConfirmCancel(): void {
        // TODO: add confirmation popup
        this.onCancel();
    }

    public removeUnassignableUser(): void {
        if (this.notAssignableAssignee) {
            const tempAssigneeAccountId = this.notAssignableAssignee.accountId
                ? this.notAssignableAssignee.accountId
                : this.notAssignableAssignee.name;

            this.assigneesOptions = this.assigneesOptions?.filter((x: { value: string }) =>
                x.value !== tempAssigneeAccountId);
        }
    }

    public setUpdatedFormFields(): void {
        const currentForm = this.issueForm;
        const initialForm = this.initialIssueForm;

        for (const key of Object.keys(currentForm?.value)) {
            const currentValue = currentForm?.value[key];
            const initialValue = initialForm.value[key];

            if (currentValue !== initialValue) {
                if (this.updatedFormFields.indexOf(key) === -1) {
                    this.updatedFormFields.push(key);
                }
            } else {
                this.updatedFormFields = this.updatedFormFields.filter(x => x !== key);
            }
        }
    }

    public sanitazeUrl(url: any) {
        return this.domSanitizer.bypassSecurityTrustUrl(url);
    }

    private async createForm(): Promise<void> {
        this.issueForm = new UntypedFormGroup({});

        if (this.allowEditSummary) {
            this.issueForm.addControl(
                this.summaryFieldName,
                new UntypedFormControl(
                    this.issue?.summary,
                    [Validators.required, StringValidators.isNotEmptyString]
                ),
                { emitEvent: false }
            );
        }

        if (this.allowEditDescription) {
            this.issueForm.addControl(
                this.descriptionFieldName,
                new UntypedFormControl(
                    this.issue?.description
                ),
                { emitEvent: false }
            );
        }

        if (this.allowEditPriority) {
            this.issueForm.addControl(
                this.priorityFieldName,
                new UntypedFormControl(
                    this.selectedPriorityOption?.value
                ),
                { emitEvent: false }
            );
        }

        if (this.allowEditAssignee) {
            this.issueForm.addControl(
                this.assigneeFieldName,
                new UntypedFormControl(
                    this.selectedAssigneeOption?.value
                ),
                { emitEvent: false }
            );
        }

        if (this.allowEditStatus) {
            this.issueForm.addControl(
                this.statusFieldName,
                new UntypedFormControl(
                    this.selectedStatusOption?.value
                ),
                { emitEvent: false }
            );
        }

        const issueTypes = await this.apiService.getCreateMetaIssueTypes(this.jiraUrl as string, this.issue?.projectKey);

        await this.addControlsByIssueType(this.issueRaw.fields['issuetype'].id, issueTypes);

        this.initialIssueForm = { ...this.issueForm };

        this.issueForm.valueChanges.subscribe(() => {
            if(!this.issueForm.dirty) {
                this.initialIssueForm = { ...this.issueForm };
            } else {
                this.setUpdatedFormFields();
            }
        });
    }

    public async addControlsByIssueType(issueTypeId: string, issueTypes: IssueType[] | any): Promise<void> {
        if (issueTypeId) {
            this.selectedIssueType = issueTypes?.find((issueType: { id: string }) => issueType.id === issueTypeId);

            this.fields = await this.apiService.getCreateMetaFields(
                this.jiraUrl as string,
                this.issue.projectKey as string,
                this.selectedIssueType?.id as string,
                this.selectedIssueType?.name as string);
        }
        // re-init fields
        const allowedFields = this.fieldsService.getAllowedFields(this.fields);

        if (this.issueForm) {
            // remove all controls for previously selected issue type if they are not configured for selected issue type
            Object.keys(this.issueForm.controls).forEach(fieldName => {
                if (!allowedFields.find(f => f.key === fieldName)) {
                    this.addRemoveControlFromForm(fieldName);
                }
            });
        }
        this.addRemovePriorityFromForm();

        // add form controls for all allowed fields for selected issue type
        allowedFields.forEach(customField => {
            this.addRemoveControlFromForm(customField.key);
        });

        this.dynamicFieldsData = this.fieldsService.getCustomFieldTemplates(this.fields, this.jiraUrl as string, this.issueRaw);
    }

    private addRemovePriorityFromForm(): void {
        if (!this.issueForm || !this.fields) {
            return;
        }

        const priorities = this.fields.priority;

        const priorityControlName = 'priority';

        if (priorities) {
            this.prioritiesOptions = priorities.allowedValues.map(this.dropdownUtilService.mapPriorityToDropdownOption);
            let defaultPriorityVal = null;
            const defaultPriority = this.defaultPriority ?
                priorities.allowedValues.find((p: { name: string }) => p.name.toLowerCase() === this.defaultPriority?.toLowerCase()) : null;
            if (defaultPriority) {
                defaultPriorityVal = this.dropdownUtilService.mapPriorityToDropdownOption(defaultPriority).value;
            } else if (priorities.hasDefaultValue && priorities.defaultValue) {
                defaultPriorityVal = this.dropdownUtilService.mapPriorityToDropdownOption(priorities.defaultValue).value;
            } else if (this.prioritiesOptions?.length && this.prioritiesOptions.length > 0) {
                defaultPriorityVal = this.prioritiesOptions[0].value;
            }

            this.issueForm.addControl(
                priorityControlName,
                new UntypedFormControl(
                    defaultPriorityVal
                ),
                { emitEvent: false }
            );
        } else if (this.issueForm.contains(priorityControlName)) {
            this.issueForm.removeControl(priorityControlName, { emitEvent: false });
            this.prioritiesOptions = [];
        }
    }

    private addRemoveControlFromForm(controlName: string): void {
        if (!this.issueForm || !this.fields || this.issueForm.contains(controlName)) {
            return;
        }

        if (this.fields[controlName]) {
            let control = new UntypedFormControl();

            if (this.isFieldRequired(controlName) && !this.isFieldReadonly(controlName) && controlName !== 'project') {
                control = new UntypedFormControl(null, [Validators.required]);
            }

            this.issueForm.addControl(
                controlName,
                control,
                { emitEvent: false }
            );
        } else if (this.issueForm.contains(controlName)) {
            this.issueForm.removeControl(controlName, { emitEvent: false });
        }
    }

    public isFieldRequired(fieldName: string): boolean {
        return this.fields && this.fields[fieldName] && this.fields[fieldName].required;
    }

    public isFieldReadonly(fieldName: string): boolean {
        return this.fields
            && this.fields[fieldName]
            && Array.isArray(this.fields[fieldName].operations) &&
            this.fields[fieldName].operations.length === 0;
    }

    private async getAssigneesOptions(userDisplayNameOrEmail: string = ''): Promise<DropDownOption<string>[]> {
        const options: SearchAssignableOptions = {
            jiraUrl: this.jiraUrl as string,
            issueKey: this.issue?.key as string,
            projectKey: this.issue?.projectKey as string,
            query: userDisplayNameOrEmail || ''
        };
        const assignees = await this.assigneeService.searchAssignable(options);

        return this.assigneeService.assigneesToDropdownOptions(assignees, userDisplayNameOrEmail);
    }

    private async setPrioritiesOptions(): Promise<void> {
        const priorityFieldName = 'priority';
        const priorities = this.editIssueMetadata?.fields[priorityFieldName];
        if (priorities) {
            this.prioritiesOptions = priorities.allowedValues.map(this.dropdownUtilService.mapPriorityToDropdownOption);
            this.selectedPriorityOption = this.prioritiesOptions.find(prt => prt.id === this.issue?.priorityId);
        }
    }

    private async setAssigneeOptions(): Promise<void> {
        this.assigneesOptions = await this.getAssigneesOptions();
        this.assigneesFilteredOptions = this.assigneesOptions;

        const assignee = this.issue.assignee;
        if (assignee === null || assignee === undefined) {
            this.selectedAssigneeOption = this.assigneeService.unassignedOption;
        }

        if (assignee) {
            const value = assignee.accountId ? assignee.accountId : assignee.name;
            this.selectedAssigneeOption = this.assigneesOptions.find((opt: { value: string }) => opt.value === value);

            /* if the issue is assigned to the user and now this user is not assignable - add this user to dropdown options
                to remove him/her when some option was selected */
            if (this.selectedAssigneeOption === undefined) {
                this.notAssignableAssignee = assignee;

                this.selectedAssigneeOption = {
                    id: value,
                    value,
                    label: assignee.displayName,
                    icon: assignee.avatarUrls['24x24']
                };

                this.assigneesOptions.push(this.selectedAssigneeOption);
            }
        }
    }

    private async setStatusesOptions() {
        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl as string, this.issue?.key as string);

        const initOption = {
            id: this.issue?.status.id,
            value: {
                id: this.issue?.status.id
            },
            label: this.issue?.status.name
        } as DropDownOption<JiraTransition>;

        this.statusesOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdownOption);

        const initOptionInTransitions = this.statusesOptions
            .find((option: { value: { to: { id: string | undefined } } }) =>
                option?.value?.to.id === this.issue?.status?.id);
        if (initOptionInTransitions) {
            this.selectedStatusOption = initOptionInTransitions;
        } else {
            this.statusesOptions.unshift(initOption);
            this.selectedStatusOption = initOption;
        }
    }

    private showConfirmationNotification(): void {
        const issueUrl =
            `<a href="${this.keyLink}" target="_blank" rel="noreferrer noopener">
            ${this.issue?.key}
            </a>`;
        const message = `The issue ${issueUrl} has been updated`;

        this.notificationService.notifySuccess(message).afterDismissed().subscribe(() => {
            this.teamsService.submitDialog();
        });
    }
}
