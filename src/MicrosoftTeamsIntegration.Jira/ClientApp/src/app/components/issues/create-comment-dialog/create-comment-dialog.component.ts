import { Component, OnInit, Input } from '@angular/core';
import { FormGroup, FormControl, Validators, AbstractControl } from '@angular/forms';
import { MatDialogConfig, MatDialog } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';
import { ListKeyManager, ListKeyManagerOption } from '@angular/cdk/a11y';
import { UP_ARROW, DOWN_ARROW, ENTER, TAB } from '@angular/cdk/keycodes';

import { IssueCommentService } from '@core/services/entities/comment.service';
import { IssueAddCommentOptions } from '@core/models/Jira/issue-comment-options.model';
import { ApiService, ErrorService, AppInsightsService } from '@core/services';
import { Issue } from '@core/models';
import { UtilService } from '@core/services/util.service';
import * as microsoftTeams from '@microsoft/teams-js';
import { StringValidators } from '@core/validators/string.validators';
import { NotificationService } from '@shared/services/notificationService';

@Component({
    selector: 'app-create-comment-dialog',
    templateUrl: './create-comment-dialog.component.html',
    styleUrls: ['./create-comment-dialog.component.scss']
})
export class CreateCommentDialogComponent implements OnInit {

    public issues: Issue[] = [];
    public selectedIssue: Issue;
    public activeIssue: Issue;
    public errorMessage: string;
    public searchTerm: string;
    public loading = false;
    public commentForm: FormGroup;
    private dialogDefaultSettings: MatDialogConfig = {
        width: '300px',
        height: '170px',
        minWidth: '300px',
        minHeight: '170px',
        ariaLabel: 'Confirmation dialog',
        closeOnNavigation: true,
        autoFocus: false,
        role: 'dialog'
    };
    private keyboardEventsManager: ListKeyManager<any>;
    private metadataRef: string;

    @Input() public defaultComment: string;
    @Input() public jiraUrl: string;
    @Input() public jiraId: string;
    @Input() public defaultSearchTerm: string;

    constructor(
        private apiService: ApiService,
        private commentService: IssueCommentService,
        private route: ActivatedRoute,
        public dialog: MatDialog,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService,
        private errorService: ErrorService,
        private notificationService: NotificationService
    ) { }

    public async ngOnInit() {
        this.loading = true;
        try {
            const { metadataRef, jiraUrl, jiraId, comment, issueUrl } = this.route.snapshot.params;
            this.metadataRef = metadataRef;
            this.jiraUrl = jiraUrl;
            this.jiraId = jiraId;
            this.defaultComment = comment;
            this.defaultSearchTerm = this.getIssueKey(issueUrl);

            await this.createForm();

            if (this.defaultSearchTerm) {
                await this.search(this.defaultSearchTerm);
            }
        } catch (error) {
            this.appInsightsService.trackException(
                new Error(error),
                'CreateCommentDialogData::ngOnInit'
            );
        }

        this.loading = false;
    }

    public async onSubmit(): Promise<void> {
        if (this.commentForm.invalid) {
            return;
        }

        this.errorMessage = '';

        const options: IssueAddCommentOptions = {
            jiraUrl: this.jiraId,
            issueIdOrKey: this.selectedIssue.id,
            comment: this.commentForm.value.comment,
            metadataRef: this.metadataRef
        };

        try {
            const response = await this.commentService.addComment(options);

            if (response && response.body) {
                this.showConfirmationNotification(this.selectedIssue);
                return;
            }
        } catch (error) {
            const errorMessage = this.errorService.getHttpErrorMessage(error);
            this.errorMessage = errorMessage ||
                'Comment cannot be added. Issue does not exist or you do not have permission to see it.';
        }
    }

    public onSelectIssue(issue: Issue): void {
        this.selectedIssue = issue;
    }

    public get comment(): AbstractControl {
        return this.commentForm.get('comment');
    }

    private async search(searchTerm: string) {
        this.selectedIssue = null;
        const jqlQuery = this.getSearchJql(searchTerm);
        await this.getIssues(jqlQuery);
    }

    private async getIssues(jqlQuery?: string) {
        try {
            const { issues } = await this.apiService.getIssues(this.jiraId, jqlQuery ? jqlQuery : 'order by updated DESC');

            if (!issues.length) {
                this.issues = [];
            } else {
                this.issues = issues;
                this.keyboardEventsManager = new ListKeyManager(this.issues as ListKeyManagerOption[]);
                // preselect issue if we have just one result
                if (this.issues.length === 1) {
                    this.selectedIssue = issues[0];
                }
            }

        } catch (error) {
            this.errorMessage = 'Cannot retrieve issue. Please try again later.';
        }
    }

    private getSearchJql(searchTerm: string): string {
        searchTerm = searchTerm.trim();

        if (searchTerm.includes('-')) {
            const parts = searchTerm.split('-');
            if(parts.length === 2 && parts.every(e => e !== null && e !== '')){
                const projectKey = parts[0].normalize();
                const issueNumber = parts[1].normalize();

                if(!isNaN(+issueNumber)) {
                    return `issuekey='${projectKey}-${issueNumber}'`;
                }
            }
        }

        return searchTerm ? `summary~'${searchTerm}*' order by updated DESC` : '';
    }

    private showConfirmationNotification(issue: Issue): void {
        const issueUrl =
            `<a href="${this.jiraUrl}/browse/${issue.key}" target="_blank" rel="noreferrer noopener">
            ${issue.key}
            </a>`;
        const message = `Comment was added to ${issueUrl}`;

        this.notificationService.notifySuccess(message).afterDismissed().subscribe(() => {
            microsoftTeams.tasks.submitTask();
        });
    }

    private getIssueKey(issueURL: string): string {
        if (!issueURL) {
            return null;
        }

        try {
            const issueLinkRegex = /(?:https|https?):\/\/(?<hostname>[^\/]*)(\/[^\/]*)*\/browse\/(?<idOrKey>[a-zA-Z\d]+\-[\d]+)\/?/;
            const issueRegexPattern = new RegExp(issueLinkRegex, 'i');

            const parts = issueRegexPattern.exec(issueURL);
            if (parts && (parts as any).groups) {
                const groups = (parts as any).groups;
                return groups['idOrKey'];
            }
            return null;
        } catch (err) {
            console.error('Cannot get issue id from URL', err);
        }
    }

    private async createForm(): Promise<void> {
        await this.getIssues();

        this.commentForm = new FormGroup({
            comment: new FormControl(
                this.defaultComment,
                [Validators.required, StringValidators.isNotEmptyString]
            )
        });
    }

    private handleListKeyUp(event: KeyboardEvent) {
        event.stopImmediatePropagation();
        if (this.keyboardEventsManager && this.issues.length > 0) {
            if(event.keyCode === TAB) {
                const activeItem = this.issues[0];
                this.keyboardEventsManager.setActiveItem(activeItem);
                this.activeIssue = activeItem;
                return false;
            }
            if (event.keyCode === DOWN_ARROW || event.keyCode === UP_ARROW) {
                this.keyboardEventsManager.onKeydown(event);
                this.activeIssue = this.keyboardEventsManager.activeItem;
                return false;
            } else if (event.keyCode === ENTER) {
                this.selectedIssue = this.keyboardEventsManager.activeItem;
                return false;
            }
        }
    }

    private handleListFocusOut() {
        this.disableActiveListItem();
    }

    private handleListMouseover() {
        this.disableActiveListItem();
    }

    private disableActiveListItem() {
        this.activeIssue = null;
    }
}
