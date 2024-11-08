import * as microsoftTeams from '@microsoft/teams-js';

import {AbstractControl, UntypedFormControl, UntypedFormGroup, Validators} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {ApiService, AppInsightsService, ErrorService} from '@core/services';
import {Component, OnInit, ViewChild} from '@angular/core';
import {CurrentJiraUser} from '@core/models/Jira/jira-user.model';
import {Issue} from '@core/models';
import {MatDialog, MatDialogConfig} from '@angular/material/dialog';

import {AssigneeService} from '@core/services/entities/assignee.service';
import {DropDownComponent} from '@shared/components/dropdown/dropdown.component';
import {DropDownOption} from '@shared/models/dropdown-option.model';
import {DropdownUtilService} from '@shared/services/dropdown.util.service';
import {FieldItem} from '../fields/field-item';
import {FieldsService} from '@shared/services/fields.service';
import {IssueType} from '@core/models/Jira/issues.model';
import {PermissionService} from '@core/services/entities/permission.service';
import {Project} from '@core/models/Jira/project.model';
import {StringValidators} from '@core/validators/string.validators';
import {UtilService} from '@core/services/util.service';
import {NotificationService} from '@shared/services/notificationService';

@Component({
    selector: 'app-create-issue-dialog',
    templateUrl: './create-issue-dialog.component.html',
    styleUrls: ['./create-issue-dialog.component.scss']
})
export class CreateIssueDialogComponent implements OnInit {
    public loading = false;
    public uploading = false;
    public isFetchingProjects = false;
    public fetching = false;
    public canCreateIssue = true;

    public issueForm: UntypedFormGroup | any;

    public projects: Project[] | any;
    public issueTypes: IssueType[] | any;
    public fields: any;

    public availableProjectsOptions: DropDownOption<string>[] | any;
    public projectFilteredOptions: DropDownOption<string>[] | any;

    public availableIssueTypesOptions: DropDownOption<string>[] | any;
    public prioritiesOptions: DropDownOption<string>[] | any;

    public dynamicFieldsData: FieldItem[] | any;

    public assigneesOptions: DropDownOption<string>[] | any;
    public assigneesFilteredOptions: DropDownOption<string>[] | any;
    public assigneesLoading = false;

    public currentUserAccountId: string | any;
    public jiraUrl: string | any;
    public defaultDescription: string | any;
    public metadataRef: string | any;
    public currentUser: CurrentJiraUser | any;
    public returnIssueOnSubmit: boolean | any;
    public replyToActivityId: string | any;
    public defaultSummary: string | any;
    public defaultPriority: string | any;
    public defaultIssueType: string | any;
    public defaultAssignee: string | any;
    public isAddonUpdated: boolean | any;

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

    private selectedProject: Project | undefined;
    private selectedIssueType: IssueType | undefined;

    private readonly DEFAULT_UNAVAILABLE_OPTION: DropDownOption<string> = {
        id: null,
        value: null,
        label: 'Unavailable'
    };

    @ViewChild('assigneesDropdown', { static: false }) assigneesDropdown: DropDownComponent<string> | any;
    @ViewChild('projectsDropdown', { static: false }) projectsDropdown: DropDownComponent<string> | any;
    @ViewChild('issueTypeDropdown', { static: false }) issueTypeDropdown: DropDownComponent<string> | any;

    constructor(
        private apiService: ApiService,
        private assigneeService: AssigneeService,
        private dropdownUtilService: DropdownUtilService,
        private appInsightsService: AppInsightsService,
        private utilService: UtilService,
        private route: ActivatedRoute,
        private router: Router,
        public dialog: MatDialog,
        private errorService: ErrorService,
        private permissionService: PermissionService,
        private fieldsService: FieldsService,
        private notificationService: NotificationService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.appInsightsService.logNavigation('CreateIssueComponent', this.route);
        const { jiraUrl, description, metadataRef, returnIssueOnSubmit, replyToActivityId, summary, issueType, priority, assignee }
            = this.route.snapshot.params;
        this.jiraUrl = jiraUrl;
        this.defaultDescription = description;
        this.metadataRef = metadataRef;
        this.returnIssueOnSubmit = returnIssueOnSubmit === 'true';
        this.replyToActivityId = replyToActivityId;

        this.defaultSummary = summary;
        this.defaultIssueType = issueType;
        this.defaultAssignee = assignee;
        this.defaultPriority = priority;

        this.loading = true;

        try {
            this.loading = true;
            await this.createForm();
            this.loading = false;
            const getAddonStatusPromise = this.apiService.getAddonStatus(jiraUrl);
            const getCurrentUserDataPromise = this.apiService.getCurrentUserData(this.jiraUrl as string);

            const [{ addonVersion }, currentUser] = await Promise.all([
                getAddonStatusPromise,
                getCurrentUserDataPromise
            ]);

            this.isAddonUpdated = this.utilService.isAddonUpdated(addonVersion);
            this.currentUser = currentUser;
            this.currentUserAccountId = this.currentUser.name;

            microsoftTeams.app.notifySuccess();
        } catch (error) {
            this.appInsightsService.trackException(
                new Error(error as any),
                'CreateIssueDialogData::ngOnInit'
            );

            microsoftTeams.dialog.url.submit(error as any);
        } finally {
            this.loading = false;
        }
    }

    public async onSubmit(): Promise<void> {
        if (this.issueForm?.invalid) {
            return;
        }

        const formValue = this.issueForm?.value;

        const createIssueFields = {
        } as Partial<any>;

        this.fieldsService.getAllowedFields(this.fields).forEach(field => {
            if (formValue[field.key]) {
                if (field.allowedValues && field.schema.type !== 'option-with-child') {
                    if (Array.isArray(formValue[field.key])) {
                        createIssueFields[field.key] = formValue[field.key].map((x: any) => ({ id: x }));
                    } else {
                        createIssueFields[field.key] = {
                            id: formValue[field.key]
                        };
                    }

                } else {
                    if (field.schema.type === 'user') {
                        createIssueFields[field.key] = {
                            name: formValue[field.key]
                        };
                    } else {
                        createIssueFields[field.key] = formValue[field.key];
                    }
                }
            }
        });

        const createIssueModel = {
            fields: createIssueFields,
            metadataRef: this.metadataRef
        } as any;

        try {
            this.uploading = true;

            const response = await this.apiService.createIssue(this.jiraUrl as string, createIssueModel);

            if (response.isSuccess && response.content) {
                this.showConfirmationNotification(response.content);
                return;
            }
        } catch (error) {
            const errorMessage = this.errorService.getHttpErrorMessage(error as any);
            this.notificationService.notifyError(errorMessage ||
                'Something went wrong. Please check your permission to perform this type of action.');
            this.uploading = false;
        }
    }

    public async onSearchChanged(filterName: string): Promise<void> {
        filterName = filterName.trim().toLowerCase();
        this.isFetchingProjects = true;
        try {
            this.projects =
                await this.findProjects(this.jiraUrl as string, filterName);
            this.projectsDropdown.filteredOptions =
                this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
        } catch (error) {
            this.appInsightsService.trackException(
                new Error('Error while searching projects'),
                'Create Issue Dialog',
                { originalErrorMessage: (error as any).message }
            );
        } finally {
            this.isFetchingProjects = false;
        }
    }

    public get isAssignableUser(): boolean | any {
        return this.assigneesOptions &&
            this.assigneesOptions
                .find((x: { value: string | undefined }) =>
                    x.value === this.currentUserAccountId) !== undefined;
    }

    public getControlByName(controlName: string): AbstractControl | any {
        if (this.issueForm?.contains(controlName)) {
            return this.issueForm.get(controlName);
        }
    }

    public async onProjectSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const projectId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;
        this.fetching = true;

        this.selectedProject = this.projects?.find((proj: { id: string | null }) => proj.id === projectId);
        const projectKey = this.selectedProject?.key as string;
        const [canCreateIssue, issueTypesResult] = await Promise.all([
            this.canCreateIssueForProject(projectKey),
            this.apiService.getCreateMetaIssueTypes(this.jiraUrl as string, projectKey).catch(error => {
                console.error('Error fetching issue types:', error);
                return null;
            }),
        ]);

        this.canCreateIssue = canCreateIssue;

        if (!this.canCreateIssue) {
            this.availableIssueTypesOptions = [this.DEFAULT_UNAVAILABLE_OPTION];
            const errorMessage = 'You can\'t create issue for this project. Contact project admin to check your permissions.';
            this.notificationService.notifyError(errorMessage);
        }
        if (issueTypesResult) {
            this.issueTypes = issueTypesResult;
            this.availableIssueTypesOptions = this.getIssueTypesOptions();
        } else {
            this.availableIssueTypesOptions = [this.DEFAULT_UNAVAILABLE_OPTION];
            const errorMessage = 'Failed to fetch issue types. Please try again later.';
            this.notificationService.notifyError(errorMessage);
        }

        await this.onIssueTypeSelected(this.availableIssueTypesOptions[0]);
        this.fetching = false;
    }

    public async onIssueTypeSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const issueTypeId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;
        this.fetching = true;

        if (issueTypeId) {
            this.selectedIssueType = this.issueTypes?.find((issueType: { id: string }) => issueType.id === issueTypeId);

            this.fields = await this.apiService.getCreateMetaFields(
                this.jiraUrl as string,
                this.selectedProject?.key as string,
                this.selectedIssueType?.id as string,
                this.selectedIssueType?.name as string);
        }
        this.assigneesOptions = await this.getAssigneesOptions(this.selectedProject?.key as string);
        this.assigneesFilteredOptions = this.assigneesOptions;

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

        // get data for dynamic fields if user has permissions to create an issue
        if (this.canCreateIssue) {
            this.dynamicFieldsData = this.fieldsService.getCustomFieldTemplates(this.fields, this.jiraUrl as string);
        } else {
            this.dynamicFieldsData = [];
        }

        this.fetching = false;
    }

    public async onAssigneeSearchChanged(username: string): Promise<void> {
        this.assigneesDropdown.filteredOptions = await this.getAssigneesOptions(this.selectedProject?.key as string, username);
    }

    public isFieldRequired(fieldName: string): boolean {
        return this.fields && this.fields[fieldName] && this.fields[fieldName].required;
    }

    public assignToMe(): void {
        this.getControlByName('assignee').setValue(this.currentUserAccountId);
    }

    public handleProjectClick(): void {
        if (!this.isAddonUpdated) {
            this.openSnackBar();
        }
    }

    private openSnackBar(): void {
        this.notificationService.notifyError(this.utilService.getUpgradeAddonMessage());
    }

    private showConfirmationNotification(issue: Issue): void {
        const issueBaseUrl = encodeURI(`${this.currentUser?.jiraServerInstanceUrl || this.jiraUrl as string}/browse/${issue.key}`);
        const issueUrl =
            `<a href="${issueBaseUrl}" target="_blank" rel="noreferrer noopener">
            ${issue.key}
            </a>`;
        const message = `Issue ${issueUrl} has been created`;
        this.notificationService.notifySuccess(message).afterDismissed().subscribe(() => {
            if (this.returnIssueOnSubmit) {
                microsoftTeams.dialog.url.submit(issue.key);
            } else {
                microsoftTeams.dialog.url.submit({
                    commandName: 'showIssueCard',
                    issueId: issue.id,
                    issueKey: issue.key,
                    replyToActivityId: this.replyToActivityId});
            }
            microsoftTeams.dialog.url.submit();
        });
    }

    private async createForm(): Promise<void> {
        this.projects = (await this.getProjects(this.jiraUrl as string)) as any;

        // if there are no projects to create an issue for - the user does not permission to create an issue
        if (!this.projects || this.projects.length === 0) {
            const message = 'You don\'t have permission to perform this action';
            await this.router.navigate(['/error'], {queryParams: {message}});
            return;
        }

        this.availableProjectsOptions = this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
        this.projectFilteredOptions = this.availableProjectsOptions;

        await this.onProjectSelected(this.availableProjectsOptions[0].value);

        const defaultAssignee = this.defaultAssignee && this.assigneesOptions ?
            this.assigneesOptions.find((x: { label: string }) =>
                x.label.toLowerCase() === this.defaultAssignee?.toLowerCase()) :
            this.assigneesOptions && this.assigneesOptions.length > 0 ?
                this.assigneesOptions[0].value :
                null;

        const defaultIssueType = this.defaultIssueType && this.availableIssueTypesOptions ?
            this.availableIssueTypesOptions.find((x: { label: string }) =>
                x.label.toLowerCase() === this.defaultIssueType?.toLowerCase()) :
            this.availableIssueTypesOptions && this.availableIssueTypesOptions.length > 0 ?
                this.availableIssueTypesOptions[0].value :
                null;

        this.issueForm = new UntypedFormGroup({
            project: new UntypedFormControl(
                this.availableProjectsOptions && this.availableProjectsOptions.length > 0 ?
                    this.availableProjectsOptions[0].value :
                    null
            ),
            issuetype: new UntypedFormControl(
                defaultIssueType
            ),
            summary: new UntypedFormControl(
                this.defaultSummary ? this.defaultSummary : '',
                [Validators.required, StringValidators.isNotEmptyString]
            ),
            description: new UntypedFormControl(this.defaultDescription),
            assignee: new UntypedFormControl(
                defaultAssignee
            )
        });

        this.addRemovePriorityFromForm();

        // create form controls for all allowed system and custom fields
        this.fieldsService.getAllowedFields(this.fields).forEach(dynamicField => {
            this.addRemoveControlFromForm(dynamicField.key);
        });
    }

    private addRemoveControlFromForm(controlName: string): void {
        if (!this.issueForm || !this.fields) {
            return;
        }

        if (this.fields[controlName]) {
            this.issueForm.addControl(
                controlName,
                this.isFieldRequired(controlName) ? new UntypedFormControl(null, [Validators.required]) : new UntypedFormControl()
            );
        } else if (this.issueForm.contains(controlName)) {
            this.issueForm.removeControl(controlName);
        }
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
                )
            );
        } else if (this.issueForm.contains(priorityControlName)) {
            this.issueForm.removeControl(priorityControlName);
            this.prioritiesOptions = [];
        }
    }

    private getIssueTypesOptions(): DropDownOption<string>[] {
        if (this.issueTypes && this.issueTypes.length > 0) {
            return this.issueTypes
                // TODO: in future make option to create subtask. For now get rid of this option!
                .filter((type: { subtask: any }) => !type.subtask)
                .map(this.dropdownUtilService.mapIssueTypeToDropdownOption);
        }
        return [this.DEFAULT_UNAVAILABLE_OPTION];
    }

    private async getAssigneesOptions(projectKey: string, username: string = ''): Promise<DropDownOption<string>[]> {
        this.assigneesLoading = true;

        const assignees = await this.assigneeService.searchAssignableMultiProject(this.jiraUrl as string, projectKey, username);

        this.assigneesLoading = false;

        return this.assigneeService.assigneesToDropdownOptions(assignees, username);
    }

    private async findProjects(jiraUrl: string, filterName?: string): Promise<Project[]> {
        return await this.apiService.findProjects(jiraUrl, filterName, true);
    }

    private async getProjects(jiraUrl: string): Promise<Project[] | null> {
        this.canCreateIssue = await this.hasCreateIssuePermission();
        if (this.canCreateIssue) {
            return await this.apiService.getProjects(jiraUrl, true);
        }

        return null;
    }

    private async canCreateIssueForProject(projectIdOrKey: string): Promise<boolean> {
        const result =
            await this.permissionService.getMyPermissions(this.jiraUrl as string, 'CREATE_ISSUES', undefined, projectIdOrKey);
        return result.permissions.CREATE_ISSUES.havePermission;
    }

    private async hasCreateIssuePermission(): Promise<boolean> {
        const result =
            await this.permissionService.getMyPermissions(this.jiraUrl as string, 'CREATE_ISSUES', undefined as any, undefined as any);
        return result.permissions.CREATE_ISSUES.havePermission;
    }
}
