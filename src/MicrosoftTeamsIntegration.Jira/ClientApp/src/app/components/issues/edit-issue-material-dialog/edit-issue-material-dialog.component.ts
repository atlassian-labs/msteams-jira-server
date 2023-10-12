import { Component, OnInit, Inject, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import {Issue, JiraComment, IssueStatus, ProjectType} from '@core/models';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { ApiService, UtilService, AppInsightsService } from '@core/services';

import { EditIssueDialogData } from '@core/models/dialogs/issue-dialog.model';
import {JiraUser, CurrentJiraUser, UserGroup} from '@core/models/Jira/jira-user.model';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { PermissionService } from '@core/services/entities/permission.service';
import { JiraPermissionName, JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { EditIssueMetadata } from '@core/models/Jira/jira-issue-edit-meta.model';
import { DomSanitizer } from '@angular/platform-browser';
import {ActivatedRoute, Router} from '@angular/router';


export interface EditDialogIssueModel {
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
    projectTypeKey: string;
}

// * TODO: Move this to utils or somewhere else with dat interface
export const mapIssueToEditIssueDialogModel = (issue: Issue): EditDialogIssueModel => ({
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
} as EditDialogIssueModel);

@Component({
    selector: 'app-edit-issue-material-dialog',
    templateUrl: './edit-issue-material-dialog.component.html',
    styleUrls: ['./edit-issue-material-dialog.component.scss']
})
export class EditIssueMaterialDialogComponent implements OnInit, OnDestroy {
    public loading = false;
    public error: HttpErrorResponse | Error;
    public summaryUpdateErrorMessage: string;
    public descriptionUpdateErrorMessage: string;

    public issue: EditDialogIssueModel;

    public currentUser: CurrentJiraUser;
    public showDescriptionControlButtons = false;
    public showSummaryControlButtons = false;

    public ValueChangeState = ValueChangeState;
    public descriptionUpdateStatus: ValueChangeState = ValueChangeState.None;
    public summaryUpdateStatus: ValueChangeState = ValueChangeState.None;

    public permissions: JiraPermissions;

    private oldSummary: string;
    private oldDescription: string;

    @ViewChild('description', {static: false}) public description: ElementRef;

    @ViewChild('summary', {static: false}) public summary: ElementRef;

    private subscription: Subscription = new Subscription();
    private initialIssue: EditDialogIssueModel;
    private editIssueMetadata: EditIssueMetadata;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: EditIssueDialogData,
        public dialogRef: MatDialogRef<EditIssueMaterialDialogComponent>,
        public domSanitizer: DomSanitizer,
        private apiService: ApiService,
        private route: ActivatedRoute,
        private permissionService: PermissionService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService,
        private router: Router
    ) { }

    public async ngOnInit(): Promise<void> {

        this.appInsightsService.logNavigation('EditIssueMaterialComponent', this.route);

        this.loading = true;

        try {
            const issueRelatedPermissions: JiraPermissionName[] = [
                'ADD_COMMENTS', 'EDIT_OWN_COMMENTS', 'EDIT_ALL_COMMENTS',
                'EDIT_ISSUES', 'ASSIGN_ISSUES', 'ASSIGNABLE_USER', 'BROWSE',
            ];

            const { permissions } = await this.permissionService
                .getMyPermissions(this.data.jiraUrl, issueRelatedPermissions, this.data.issueId);
            this.permissions = permissions;

            if (!this.canEditIssue && !this.canViewIssue) {
                const message = 'You don\'t have permissions to perform this action';
                await this.router.navigate(['/error'], { queryParams: { message } });
                return;
            }

            const issue = await this.apiService.getIssueByIdOrKey(this.data.jiraUrl, this.data.issueId);

            this.issue = mapIssueToEditIssueDialogModel(issue);
            this.oldSummary = this.issue.summary;
            this.oldDescription = this.issue.description;

            this.editIssueMetadata = await this.apiService.getEditIssueMetadata(this.data.jiraUrl, this.data.issueId);

            this.initialIssue = this.utilService.jsonCopy<EditDialogIssueModel>(this.issue);

            this.currentUser = await this.apiService.getCurrentUserData(this.data.jiraUrl);
        } catch (error) {
            this.error = error;
            this.appInsightsService.trackException(
                new Error(error),
                'EditIssueMaterialDialogComponent::ngOnInit',
                this.issue
            );

            if (error.status && error.status === 401) {
                this.dialogRef.close();
            }
        }

        // disable backdrop and escape button clicks to close the dialog
        this.dialogRef.disableClose = true;
        this.subscribeToCloseEvents();

        this.loading = false;
    }

    public get keyLink(): string {
        const jiraServerInstanceUrl = this.currentUser.jiraServerInstanceUrl || this.data.jiraUrl;
        return encodeURI(`${jiraServerInstanceUrl}/browse/${this.issue.key}`);
    }

    public async updateDescription(): Promise<void> {
        const newDescription = this.issue.description;

        try {
            this.descriptionUpdateStatus = ValueChangeState.Pending;

            const result = await this.apiService.updateIssueDescription(this.data.jiraUrl, this.issue.id, newDescription);

            if (result.isSuccess) {
                this.issue.description = newDescription;
                this.showDescriptionControlButtons = false;
                this.descriptionUpdateStatus = ValueChangeState.Success;

                setTimeout(() => this.descriptionUpdateStatus = ValueChangeState.None, 2500);
            } else {
                this.descriptionUpdateStatus = ValueChangeState.Failed;
                this.descriptionUpdateErrorMessage = result.errorMessage || 'Error while updating description';
            }
        } catch (error) {
            // show update error
            this.descriptionUpdateStatus = ValueChangeState.ServerError;
            this.descriptionUpdateErrorMessage = error.errorMessage || error.message || 'Error while updating description.';
        }
    }

    public async updateSummary(): Promise<void> {
        const newSummary = this.issue.summary;

        try {
            this.summaryUpdateStatus = ValueChangeState.Pending;
            const response = await this.apiService.updateIssueSummary(this.data.jiraUrl, this.issue.id, newSummary);

            if (response.isSuccess) {
                this.summaryUpdateStatus = ValueChangeState.Success;
                this.showSummaryControlButtons = false;

                this.issue.summary = newSummary;

                setTimeout(() => this.summaryUpdateStatus = ValueChangeState.None, 2500);
            } else {
                this.summaryUpdateStatus = ValueChangeState.Failed;
                this.summaryUpdateErrorMessage = response.errorMessage;
            }
        } catch (error) {
            this.summaryUpdateStatus = ValueChangeState.Failed;
        }
    }

    public discardDescriptionChanges(): void {
        this.issue.description = this.oldDescription;
        this.showDescriptionControlButtons = false;
        this.descriptionUpdateStatus = ValueChangeState.None;
    }

    public discardSummaryChanges(): void {
        this.issue.summary = this.oldSummary;
        this.showSummaryControlButtons = false;
        this.summaryUpdateStatus = ValueChangeState.None;
    }

    public onDescriptionChanged(): void {
        const newDescription = this.issue.description;
        const valueChanged = this.showDescriptionControlButtons = newDescription !== this.oldDescription;
        this.descriptionUpdateStatus = valueChanged ? ValueChangeState.Editing : ValueChangeState.None;
    }

    public onSummaryChanged(): void {
        const newSummary = this.issue.summary;
        if (!newSummary || newSummary.trim() === '') {
            this.summaryUpdateStatus = ValueChangeState.Failed;
            this.showSummaryControlButtons = true;
            this.summaryUpdateErrorMessage = 'You must specify a summary of the issue';
            return;
        }
        const valueChanged = this.showSummaryControlButtons = newSummary !== this.oldSummary;
        this.summaryUpdateStatus = valueChanged ? ValueChangeState.Editing : ValueChangeState.None;
    }

    public onStatusChanged(statusId: string): void {
        this.issue.status.id = statusId;
    }

    public onAssigneeChanged(assignee: JiraUser | null): void {
        this.issue.assignee = assignee;
    }

    public onPriorityChanged(priorityId: string): void {
        this.issue.priorityId = priorityId;
    }

    public onNewCommentCreated(comment: JiraComment): void {
        this.issue.comment.comments.push(comment);
    }

    /**
     * @returns {true} if the field is in the edit issue metadata
     */
    public canEditField = (fieldName: string): boolean => !!this.editIssueMetadata.fields[fieldName];

    public ngOnDestroy(): void {
        this.subscription.unsubscribe();
    }

    public get canEditIssue(): boolean {
        return this.permissions.EDIT_ISSUES.havePermission;
    }

    public get canViewIssue(): boolean {
        return this.permissions.BROWSE.havePermission;
    }

    public get allowEditAssignee(): boolean {
        // for JSM projects user should be a member of 'jira-servicedesk-users' group in order to get assignees,
        // even with ASSIGN_ISSUES project permission
        if (this.issue.projectTypeKey === ProjectType.ServiceDesk) {
            return this.currentUser.groups.items.some(x => x.name === UserGroup.JiraServicedeskUsers) &&
                this.permissions.ASSIGN_ISSUES.havePermission;
        }
        return this.permissions.ASSIGN_ISSUES.havePermission;
    }

    private subscribeToCloseEvents(): void {
        const backDropClickSubscription = this.dialogRef.backdropClick().subscribe(() => this.close());
        this.subscription.add(backDropClickSubscription);

        const keydownEventsSubscription = this.dialogRef.keydownEvents().subscribe((keyEvent: KeyboardEvent) => {
            // 27 - keyCode of 'Escape' button
            if (keyEvent.keyCode === 27) {
                this.close();
            }
        });
        this.subscription.add(keydownEventsSubscription);
    }

    private close(): void {
        if (this.summaryUpdateStatus === ValueChangeState.Editing || this.descriptionUpdateStatus === ValueChangeState.Editing) {
            if (!confirm('You have some unsaved changes. Are you sure you want to close the window?')) {
                return;
            }
        }

        const issueChanged = !this.utilService.jsonEqual(this.issue, this.initialIssue);

        this.dialogRef.close(issueChanged);
    }
}
