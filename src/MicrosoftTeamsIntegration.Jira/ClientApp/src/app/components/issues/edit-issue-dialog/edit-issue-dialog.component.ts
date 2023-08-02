import * as microsoftTeams from '@microsoft/teams-js';

import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { ApiService, AppInsightsService } from '@core/services';
import { Component, OnInit } from '@angular/core';
import { ConfirmationDialogData, DialogType } from '@core/models/dialogs/issue-dialog.model';
import {CurrentJiraUser, JiraUser, UserGroup} from '@core/models/Jira/jira-user.model';
import {Issue, IssueFields, Priority, ProjectType} from '@core/models';
import { IssueStatus, JiraComment } from '@core/models';
import { JiraPermissionName, JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';

import { ActivatedRoute, Router } from '@angular/router';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { ConfirmationDialogComponent } from '@app/components/issues/confirmation-dialog/confirmation-dialog.component';
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

const mapIssueToEditIssueDialogModel = (issue: Issue): EditIssueModel => {
    return {
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
    } as EditIssueModel;
};

@Component({
    selector: 'app-edit-issue-dialog',
    templateUrl: './edit-issue-dialog.component.html',
    styleUrls: ['./edit-issue-dialog.component.scss']
})
export class EditIssueDialogComponent implements OnInit {
    public loading = false;
    public uploading = false;

    public error: HttpErrorResponse | Error;
    public issue: EditIssueModel;
    public currentUser: CurrentJiraUser;
    public permissions: JiraPermissions;

    public jiraUrl: string;
    public issueId: string;
    public issueKey: string;
    public issueForm: FormGroup;
    public updatedFormFields: string[] = [];
    public replyToActivityId: string;

    public initialIssueForm: any;
    public errorMessage: string;

    // priority
    public prioritiesOptions: DropDownOption<string>[] = [];
    public selectedPriorityOption: DropDownOption<string>;
    private priorities: Priority[];

    // assignee
    public assigneesOptions: DropDownOption<string>[];
    public assigneesFilteredOptions: DropDownOption<string>[];
    public selectedAssigneeOption: DropDownOption<string>;
    public assigneesLoading = false;

    // status
    public statusesOptions: DropDownOption<JiraTransition>[];
    public selectedStatusOption: DropDownOption<JiraTransition>;

    public currentUserAccountId: string;

    private editIssueMetadata: EditIssueMetadata;
    private notAssignableAssignee: JiraUser;

    private dialogDefaultSettings: MatDialogConfig = {
        width: '350px',
        height: '200px',
        minWidth: '200px',
        minHeight: '150px',
        ariaLabel: 'Confirmation dialog',
        closeOnNavigation: true,
        autoFocus: false,
        role: 'dialog'
    };

    private summaryFieldName = 'summary';
    private descriptionFieldName = 'description';
    private priorityFieldName = 'priorityId';
    private assigneeFieldName = 'assigneeAccountId';
    private statusFieldName = 'status';

    constructor(
        public dialog: MatDialog,
        public domSanitizer: DomSanitizer,
        private apiService: ApiService,
        private permissionService: PermissionService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService,
        private route: ActivatedRoute,
        private dropdownUtilService: DropdownUtilService,
        private assigneeService: AssigneeService,
        private transitionService: IssueTransitionService,
        private router: Router
    ) { }

    public async ngOnInit(): Promise<void> {

        this.appInsightsService.logNavigation('EditIssueComponent', this.route);

        this.loading = true;

        try {
            const { jiraUrl, issueId, issueKey, replyToActivityId } = this.route.snapshot.params;
            this.jiraUrl = jiraUrl;
            this.issueId = issueId;
            this.issueKey = issueKey;
            this.replyToActivityId = replyToActivityId;

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
                const message = "You don't have permissions to perform this action";
                await this.router.navigate(['/error'], { queryParams: { message } });
                return;
            }

            const issue = await this.apiService.getIssueByIdOrKey(this.jiraUrl, this.issueId);
            this.issue = mapIssueToEditIssueDialogModel(issue);

            this.editIssueMetadata = await this.apiService.getEditIssueMetadata(this.jiraUrl, this.issueId);

            this.currentUser = await this.apiService.getCurrentUserData(this.jiraUrl);
            this.currentUserAccountId = this.currentUser.name;

            // priority
            if (this.allowEditPriority) {
                await this.setPrioritiesOptions();
            }
            
            // assignee
            if (this.allowEditAssignee) {
                await this.setAssigneeOptions();
            }

            // status
            if (this.allowEditStatus) {
                await this.setStatusesOptions();
            }

            await this.createForm();

        } catch (error) {
            this.error = error;
            this.appInsightsService.trackException(
                new Error(error),
                'EditIssueMaterialDialogComponent::ngOnInit',
                this.issue
            );
        }

        this.loading = false;
    }

    public get summary(): AbstractControl {
        return this.issueForm.get(this.summaryFieldName);
    }

    public get assigneeAccountId(): AbstractControl {
        return this.issueForm.get(this.assigneeFieldName);
    }

    public get priorityId(): AbstractControl {
        return this.issueForm.get(this.priorityFieldName);
    }

    public get keyLink(): string {
        const jiraServerInstanceUrl = this.currentUser.jiraServerInstanceUrl || this.jiraUrl;
        return encodeURI(`${jiraServerInstanceUrl}/browse/${this.issue.key}`);
    }
    
    public get canEditIssue(): boolean {
        return this.permissions.EDIT_ISSUES.havePermission;
    }

    public get canViewIssue(): boolean {
        return this.permissions.BROWSE.havePermission;
    }

    public get allowEditSummary(): boolean {
        return this.permissions.EDIT_ISSUES.havePermission && this.canEditField('summary');
    }

    public get allowEditDescription(): boolean {
        return this.permissions.EDIT_ISSUES.havePermission && this.canEditField('description');
    }

    public get allowEditPriority(): boolean {
        return this.permissions.EDIT_ISSUES.havePermission && this.canEditField('priority');
    }

    public get allowEditAssignee(): boolean {
        // for JSM projects user should be a member of 'jira-servicedesk-users' group in order to get assignees,
        // even with ASSIGN_ISSUES project permission 
        if (this.issue.projectTypeKey == ProjectType.ServiceDesk) {
            return this.currentUser.groups.items.some(x => x.name == UserGroup.JiraServicedeskUsers) && 
                this.permissions.ASSIGN_ISSUES.havePermission;
        }
        return this.permissions.ASSIGN_ISSUES.havePermission;
    }

    public get allowEditStatus(): boolean {
        return this.permissions.TRANSITION_ISSUES.havePermission;
    }

    /**
     * @returns {true} if the field is in the edit issue metadata
     */
    public canEditField = (fieldName: string): boolean => !!this.editIssueMetadata.fields[fieldName];

    public assignToMe(): void {
        this.assigneeAccountId.setValue(this.currentUserAccountId);
        this.setUpdatedFormFields();
    }

    public async onAssigneeSearchChanged(username: string): Promise<void> {
        this.assigneesFilteredOptions = await this.getAssigneesOptions(username);
    }

    public onNewCommentCreated(comment: JiraComment): void {
        this.issue.comment.comments.push(comment);
    }

    public async onSubmit(): Promise<void> {
        if (this.issueForm.invalid) {
            return;
        }

        const formValue = this.issueForm.value;
        const editIssueModel = {
            priority: {},
            status: {}
        } as Partial<IssueFields>;

        if (this.updatedFormFields.indexOf(this.summaryFieldName) !== -1) {
            editIssueModel.summary = formValue.summary;
        }

        if (this.updatedFormFields.indexOf(this.descriptionFieldName) !== -1) {
            editIssueModel.description = formValue.description;
        }

        if (this.updatedFormFields.indexOf(this.statusFieldName) !== -1) {
            editIssueModel.status.id = formValue.status.id;
        }

        if (this.updatedFormFields.indexOf(this.priorityFieldName) !== -1) {
            editIssueModel.priority.id = formValue.priorityId;
        }

        // if user has permissions to assign the issue - pass even null as a value for accountId (it means Unassigned)
        if (this.updatedFormFields.indexOf(this.assigneeFieldName) !== -1) {
            editIssueModel.assignee = {
                name: formValue.assigneeAccountId
            } as JiraUser;
        }

        try {
            this.uploading = true;

            const response = await this.apiService.updateIssue(encodeURIComponent(this.jiraUrl), this.issue.id, editIssueModel);

            if (response.isSuccess) {
                this.openConfirmationDialog();
                return;
            }

            this.errorMessage = response.errorMessage ||
                'Something went wrong. Please check your permission to perform this type of action.';
        } catch (error) {

        } finally {
            this.uploading = false;
        }
    }

    public onCancel(): void {
        microsoftTeams.tasks.submitTask();
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

            this.assigneesOptions = this.assigneesOptions.filter(x => x.value !== tempAssigneeAccountId);
        }
    }

    public setUpdatedFormFields(): void {
        const currentForm = this.issueForm;
        const initialForm = this.initialIssueForm;

        for (let i = 0; i < Object.keys(currentForm.value).length; i++) {
            const key = Object.keys(currentForm.value)[i];
            const currentValue = currentForm.value[key];
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

    private async createForm(): Promise<void> {
        this.issueForm = new FormGroup({});

        if (this.allowEditSummary) {
            this.issueForm.addControl(
                this.summaryFieldName,
                new FormControl(
                    this.issue.summary,
                    [Validators.required, StringValidators.isNotEmptyString]
                ),
            );
        }

        if (this.allowEditDescription) {
            this.issueForm.addControl(
                this.descriptionFieldName,
                new FormControl(
                    this.issue.description
                )
            );
        }

        if (this.allowEditPriority) {
            this.issueForm.addControl(
                this.priorityFieldName,
                new FormControl(
                    this.selectedPriorityOption.value
                ),
            );
        }

        if (this.allowEditAssignee) {
            this.issueForm.addControl(
                this.assigneeFieldName,
                new FormControl(
                    this.selectedAssigneeOption.value
                ),
            );
        }

        if (this.allowEditStatus) {
            this.issueForm.addControl(
                this.statusFieldName,
                new FormControl(
                    this.selectedStatusOption.value
                )
            );
        }

        this.initialIssueForm = { ...this.issueForm };
    }

    private async getAssigneesOptions(userDisplayNameOrEmail: string = ''): Promise<DropDownOption<string>[]> {
        const options: SearchAssignableOptions = {
            jiraUrl: this.jiraUrl,
            issueKey: this.issue.key,
            projectKey: this.issue.projectKey,
            query: userDisplayNameOrEmail || ''
        };
        const assignees = await this.assigneeService.searchAssignable(options);

        return this.assigneeService.assigneesToDropdownOptions(assignees, userDisplayNameOrEmail);
    }

    private async setPrioritiesOptions(): Promise<void> {
        this.priorities = await this.apiService.getPriorities(this.jiraUrl);
        if (this.priorities) {
            this.prioritiesOptions = this.priorities.map(this.dropdownUtilService.mapPriorityToDropdownOption);
            this.selectedPriorityOption = this.prioritiesOptions.find(prt => prt.id === this.issue.priorityId);
        }
    }

    private async setAssigneeOptions(): Promise<void> {
        this.assigneesOptions = await this.getAssigneesOptions();
        this.assigneesFilteredOptions = this.assigneesOptions;

        const assignee = this.issue.assignee;
        if (assignee === null) {
            this.selectedAssigneeOption = this.assigneeService.unassignedOption;
        }

        if (assignee) {
            const value = assignee.accountId ? assignee.accountId : assignee.name;
            this.selectedAssigneeOption = this.assigneesOptions.find(opt => opt.value === value);

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
        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl, this.issue.key);

        const initOption = {
            id: this.issue.status.id,
            value: {
                id: this.issue.status.id
            },
            label: this.issue.status.name
        } as DropDownOption<JiraTransition>;

        this.statusesOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdonwOption);

        const initOptionInTransitions = this.statusesOptions.find(option => option.value.to.id === this.issue.status.id);
        if (initOptionInTransitions) {
            this.selectedStatusOption = initOptionInTransitions;
        } else {
            this.statusesOptions.unshift(initOption);
            this.selectedStatusOption = initOption;
        }
    }

    private openConfirmationDialog(): void {
        const dialogConfig = {
            ...this.dialogDefaultSettings,
            ...{
                data: {
                    title: 'Success',
                    subtitle: `Issue <a href="${this.keyLink}" target="_blank" rel="noreferrer noopener">${this.issue.key}</a> has been successfully updated.`,
                    buttonText: 'Dismiss',
                    dialogType: DialogType.SuccessLarge
                } as ConfirmationDialogData
            }
        };

        this.dialog.open(ConfirmationDialogComponent, dialogConfig)
            .afterClosed().subscribe(() => {
                microsoftTeams.tasks.submitTask({ commandName: 'showIssueCard', issueId: this.issue.id, issueKey: this.issueKey, replyToActivityId: this.replyToActivityId });
                microsoftTeams.tasks.submitTask();
            });
    }
}
