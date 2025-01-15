import { Component, Input, Output, EventEmitter } from '@angular/core';
import { JiraComment } from '@core/models';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { HttpErrorResponse } from '@angular/common/http';
import { JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { IssueUpdateCommentOptions } from '@core/models/Jira/issue-comment-options.model';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'app-issue-comment',
    templateUrl: './issue-comment.component.html',
    styleUrls: ['./issue-comment.component.scss'],
    standalone: false
})
export class IssueCommentComponent {
    public error: Error | undefined;

    public showEditIcon = false;
    public showEditField = false;

    public ValueChangeState = ValueChangeState;
    public commentUpdateState = ValueChangeState.None;

    @Input() public comment: JiraComment | any;
    @Input() public jiraUrl: string | any;
    @Input() public issueId: string | any;
    @Input() public user: JiraUser | any;
    @Input() public permissions: JiraPermissions | any;

    @Output() public change = new EventEmitter<{ oldValue: JiraComment; newValue: JiraComment }>();

    constructor(
        private commentService: IssueCommentService,
        public domSanitizer: DomSanitizer
    ) { }

    public get isEditable(): boolean {
        if (!this.permissions) {
            return false;
        }

        if (this.permissions.EDIT_ALL_COMMENTS.havePermission) {
            return true;
        }

        if (this.permissions.EDIT_OWN_COMMENTS.havePermission &&
            this.comment.author.accountId === this.user.accountId) {
            return true;
        }

        return false;
    }

    public get isEditingState(): boolean {
        return this.commentUpdateState === ValueChangeState.Editing;
    }

    public get isPendingState(): boolean {
        return this.commentUpdateState === ValueChangeState.Pending;
    }

    public async editComment(newCommentText: string): Promise<void> {
        const oldCommentText = this.comment.body;

        if (oldCommentText === newCommentText) {
            return;
        }

        if (!newCommentText || !newCommentText.replace(/ /g, '').length) {
            this.commentUpdateState = ValueChangeState.InvalidEmpty;
            return;
        }

        this.commentUpdateState = ValueChangeState.Pending;

        try {
            const options: IssueUpdateCommentOptions = {
                jiraUrl: this.jiraUrl,
                issueIdOrKey: this.issueId,
                commentId: this.comment.id,
                comment: newCommentText
            };

            const updatedComment = await this.commentService.updateComment(options);
            const oldComment = this.comment;

            updatedComment.author = this.comment.author;
            updatedComment.updateAuthor = this.user;

            this.comment = { ...this.comment, ...updatedComment };

            this.change.emit({ oldValue: oldComment, newValue: this.comment });

            this.commentUpdateState = ValueChangeState.Success;
            this.showEditField = false;
        } catch (error) {
            this.error = error as any;

            if (error instanceof HttpErrorResponse) {
                this.commentUpdateState = ValueChangeState.ServerError;
            } else {
                this.commentUpdateState = ValueChangeState.Failed;
            }
        } finally {
            setTimeout(() => this.commentUpdateState = ValueChangeState.None, 2500);
        }
    }
}
