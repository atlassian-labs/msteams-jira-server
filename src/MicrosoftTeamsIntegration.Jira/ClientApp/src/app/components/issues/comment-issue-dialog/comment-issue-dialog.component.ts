import { Component, OnInit } from '@angular/core';
import { UntypedFormGroup, UntypedFormControl, Validators, AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { IssueAddCommentOptions } from '@core/models/Jira/issue-comment-options.model';
import { ApiService, ErrorService, AppInsightsService } from '@core/services';
import { Issue } from '@core/models';
import * as microsoftTeams from '@microsoft/teams-js';
import { StringValidators } from '@core/validators/string.validators';
import { DomSanitizer } from '@angular/platform-browser';
import { JiraPermissionName } from '@core/models/Jira/jira-permission.model';
import { PermissionService } from '@core/services/entities/permission.service';
import { NotificationService } from '@shared/services/notificationService';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';

@Component({
    selector: 'app-comment-issue-dialog',
    templateUrl: './comment-issue-dialog.component.html',
    styleUrls: ['./comment-issue-dialog.component.scss'],
    standalone: false
})
export class CommentIssueDialogComponent implements OnInit {
    public issue: Issue | undefined;
    public loading = false;
    public commentForm: UntypedFormGroup | undefined;
    public jiraId: string | undefined;
    public jiraUrl: string | undefined;
    public issueId: string | undefined;
    public issueKey: string | undefined;
    public formDisabled: boolean | undefined;

    constructor(
        public domSanitizer: DomSanitizer,
        private commentService: IssueCommentService,
        private route: ActivatedRoute,
        private apiService: ApiService,
        private appInsightsService: AppInsightsService,
        private permissionService: PermissionService,
        private router: Router,
        private errorService: ErrorService,
        private readonly notificationService: NotificationService,
        private analyticsService: AnalyticsService
    ) { }

    public async ngOnInit() {
        this.loading = true;
        try {
            const { jiraId, issueId, issueKey, application } = this.route.snapshot.params;
            this.jiraId = jiraId;
            this.issueId = issueId;
            this.issueKey = issueKey;
            const commentRelatedPermissions: JiraPermissionName[] = [
                'ADD_COMMENTS',
            ];

            this.analyticsService.sendScreenEvent(
                'createCommentModal',
                EventAction.viewed,
                UiEventSubject.taskModule,
                'createCommentModal', {application: application});

            if (!this.jiraId) {
                const response = await this.apiService.getJiraUrlForPersonalScope();
                this.jiraId = response.jiraUrl;
            }

            const { permissions } = await this.permissionService
                .getMyPermissions(this.jiraId, commentRelatedPermissions, this.issueId);

            if (!permissions.ADD_COMMENTS.havePermission) {
                const message = 'You don\'t have permissions to add comments';
                await this.router.navigate(['/error'], { queryParams: { message } });
                return;
            }

            const currentUserPromise = this.apiService.getCurrentUserData(this.jiraId);
            const issuePromise = this.apiService.getIssueByIdOrKey(this.jiraId, this.issueId as string);

            const [currentUser, issue] = await Promise.all([
                currentUserPromise,
                issuePromise
            ]);

            this.issue = issue;
            this.jiraUrl = currentUser.jiraServerInstanceUrl;
            await this.createForm();

            microsoftTeams.app.notifySuccess();
        } catch (error) {
            this.appInsightsService.trackException(
                new Error(error as any),
                'CommentIssueDialogData::ngOnInit'
            );
        }

        this.loading = false;
    }

    public async onSubmit(): Promise<void> {
        if (this.commentForm?.invalid) {
            return;
        }

        const options: IssueAddCommentOptions = {
            jiraUrl: this.jiraId as string,
            issueIdOrKey: this.issueId as string,
            comment: this.commentForm?.value.comment,
            metadataRef: null as any
        };

        try {
            const response = await this.commentService.addComment(options);

            if (response && response.body) {
                this.formDisabled = true;
                this.showConfirmationNotification();
                return;
            }
        } catch (error) {
            const errorMessage = this.errorService.getHttpErrorMessage(error as any);
            this.notificationService.notifyError(errorMessage ||
                'Comment cannot be added. Issue does not exist or you do not have permission to see it.');
            this.formDisabled = false;
        }
    }

    public get comment(): AbstractControl | any {
        return this.commentForm?.get('comment');
    }

    public get keyLink(): string {
        return encodeURI(`${this.jiraUrl}/browse/${this.issue?.key}`);
    }

    public get summary(): string | undefined {
        const maxLenght = 110;
        return (this.issue?.fields.summary.length && this.issue.fields.summary.length > maxLenght)
            ? this.issue?.fields.summary.slice(0, maxLenght-1) + '...'
            : this.issue?.fields.summary;
    }

    public sanitazeUrl(url: any) {
        return this.domSanitizer.bypassSecurityTrustUrl(url);
    }

    private showConfirmationNotification(): void {
        const issueUrl = `<a href="${this.jiraUrl}/browse/${this.issue?.key}" target="_blank" rel="noreferrer noopener">
             ${this.issue?.key}
             </a>`;

        this.notificationService.notifySuccess(`Comment was added to ${issueUrl}`)
            .afterDismissed().subscribe(() => {
                microsoftTeams.dialog.url.submit();
            });
    }

    private async createForm(): Promise<void> {
        this.commentForm = new UntypedFormGroup({
            comment: new UntypedFormControl('',[Validators.required, StringValidators.isNotEmptyString])
        });
    }
}
