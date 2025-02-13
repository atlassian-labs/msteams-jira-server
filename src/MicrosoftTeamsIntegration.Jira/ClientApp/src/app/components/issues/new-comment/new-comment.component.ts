import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { JiraComment } from '@core/models';
import { DomSanitizer } from '@angular/platform-browser';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';

@Component({
    selector: 'app-new-comment',
    templateUrl: './new-comment.component.html',
    styleUrls: ['./new-comment.component.scss'],
    standalone: false
})
export class NewCommentComponent {
    public error: Error | any;
    public serverErrorMessage = '';

    @Input() public jiraUrl: string | any;
    @Input() public issueId: string | any;
    @Input() public user: JiraUser | any;

    @Output() public created = new EventEmitter<JiraComment>();

    public commentSendState: ValueChangeState = ValueChangeState.None;
    public ValueChangeState = ValueChangeState;

    constructor(
        private commentService: IssueCommentService,
        public domSanitizer: DomSanitizer,
        private analyticsService: AnalyticsService
    ) { }

    public async sendComment(text: string, inputElRef: HTMLTextAreaElement | any): Promise<void> {
        if (!text || !text.replace(/ /g, '').length) {
            this.commentSendState = ValueChangeState.InvalidEmpty;
            return;
        }

        this.analyticsService.sendUiEvent('newCommentComponent',
            EventAction.clicked,
            UiEventSubject.link,
            'addComment',
            {source: 'editIssueModal'});

        try {
            this.commentSendState = ValueChangeState.Pending;

            const newComment = await this.commentService.addComment({
                jiraUrl: this.jiraUrl,
                issueIdOrKey: this.issueId,
                comment: text,
                metadataRef: null as any
            });

            newComment.author = this.user;

            this.created.emit(newComment);

            this.commentSendState = ValueChangeState.Success;
            inputElRef.value = '';
        } catch (error) {
            this.error = error;
            this.serverErrorMessage = (error as any).errorMessage ||
                'An error occured. Please check your permissions to perform this action.';

            this.commentSendState = ValueChangeState.ServerError;
        } finally {
            if (this.commentSendState !== ValueChangeState.ServerError) {
                setTimeout(() => this.commentSendState = ValueChangeState.None, 2500);
            }
        }
    }
}
