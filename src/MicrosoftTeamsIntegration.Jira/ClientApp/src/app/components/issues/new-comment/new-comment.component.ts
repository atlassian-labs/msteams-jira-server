import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { JiraComment } from '@core/models';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
    selector: 'app-new-comment',
    templateUrl: './new-comment.component.html',
    styleUrls: ['./new-comment.component.scss']
})
export class NewCommentComponent {
    public error: Error;
    public serverErrorMessage = '';

    @Input() public jiraUrl: string;
    @Input() public issueId: string;
    @Input() public user: JiraUser;

    @Output() public created = new EventEmitter<JiraComment>();

    public commentSendState: ValueChangeState = ValueChangeState.None;
    public ValueChangeState = ValueChangeState;

    constructor(
        private commentService: IssueCommentService,
        public domSanitizer: DomSanitizer
    ) { }

    public async sendComment(text: string, inputElRef: HTMLTextAreaElement): Promise<void> {
        if (!text || !text.replace(/ /g, '').length) {
            this.commentSendState = ValueChangeState.InvalidEmpty;
            return;
        }

        try {
            this.commentSendState = ValueChangeState.Pending;

            const newComment = await this.commentService.addComment({
                jiraUrl: this.jiraUrl,
                issueIdOrKey: this.issueId,
                comment: text,
                metadataRef: null
            });

            newComment.author = this.user;

            this.created.emit(newComment);

            this.commentSendState = ValueChangeState.Success;
            inputElRef.value = '';
        } catch (error) {
            this.error = error;
            this.serverErrorMessage = error.errorMessage || 'An error occured. Please check your permissions to perform this action.';

            this.commentSendState = ValueChangeState.ServerError;
        } finally {
            if (this.commentSendState !== ValueChangeState.ServerError) {
                setTimeout(() => this.commentSendState = ValueChangeState.None, 2500);
            }
        }
    }
}
