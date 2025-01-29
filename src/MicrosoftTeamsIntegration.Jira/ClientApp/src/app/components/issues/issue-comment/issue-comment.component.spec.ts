import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IssueCommentComponent } from './issue-comment.component';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { DomSanitizer } from '@angular/platform-browser';
import { AnalyticsService } from '@core/services/analytics.service';
import { JiraComment } from '@core/models';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { HttpErrorResponse } from '@angular/common/http';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { JiraUser } from '@core/models/Jira/jira-user.model';

describe('IssueCommentComponent', () => {
    let component: IssueCommentComponent;
    let fixture: ComponentFixture<IssueCommentComponent>;
    let commentService: jasmine.SpyObj<IssueCommentService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;

    beforeEach(async () => {
        const commentServiceSpy = jasmine.createSpyObj('IssueCommentService', ['updateComment']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendUiEvent']);

        await TestBed.configureTestingModule({
            declarations: [IssueCommentComponent],
            providers: [
                { provide: IssueCommentService, useValue: commentServiceSpy },
                { provide: DomSanitizer, useValue: { bypassSecurityTrustUrl: (url: string) => url } },
                { provide: AnalyticsService, useValue: analyticsServiceSpy }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(IssueCommentComponent);
        component = fixture.componentInstance;
        commentService = TestBed.inject(IssueCommentService) as jasmine.SpyObj<IssueCommentService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should determine if comment is editable', () => {
        component.permissions = { EDIT_ALL_COMMENTS: { havePermission: true } } as JiraPermissions;
        expect(component.isEditable).toBeTrue();

        component.permissions = {
            EDIT_OWN_COMMENTS: { havePermission: true }, EDIT_ALL_COMMENTS: { havePermission: false } } as JiraPermissions;
        component.comment = { author: { accountId: 'testUser' } } as JiraComment;
        component.user = { accountId: 'testUser' } as JiraUser;
        expect(component.isEditable).toBeTrue();

        component.permissions = {
            EDIT_OWN_COMMENTS: { havePermission: true }, EDIT_ALL_COMMENTS: { havePermission: false } } as JiraPermissions;
        component.comment = { author: { accountId: 'testUser2' } } as JiraComment;
        component.user = { accountId: 'testUser' } as JiraUser;
        expect(component.isEditable).toBeFalse();
    });

    it('should determine if comment is in editing state', () => {
        component.commentUpdateState = ValueChangeState.Editing;
        expect(component.isEditingState).toBeTrue();

        component.commentUpdateState = ValueChangeState.None;
        expect(component.isEditingState).toBeFalse();
    });

    it('should determine if comment is in pending state', () => {
        component.commentUpdateState = ValueChangeState.Pending;
        expect(component.isPendingState).toBeTrue();

        component.commentUpdateState = ValueChangeState.None;
        expect(component.isPendingState).toBeFalse();
    });

    it('should edit comment successfully', async () => {
        component.comment = { id: 'testCommentId', body: 'old comment', author: { accountId: 'testUser' } } as JiraComment;
        component.user = { accountId: 'testUser' } as JiraUser;
        component.jiraUrl = 'http://example.com';
        component.issueId = 'testIssueId';
        const updatedComment = { id: 'testCommentId', body: 'new comment', author: { accountId: 'testUser' } } as JiraComment;
        commentService.updateComment.and.returnValue(Promise.resolve(updatedComment));

        await component.editComment('new comment');

        expect(commentService.updateComment).toHaveBeenCalled();
        expect(component.comment.body).toBe('new comment');
        expect(component.commentUpdateState).toBe(ValueChangeState.Success);
    });

    it('should handle edit comment failure', async () => {
        component.comment = { id: 'testCommentId', body: 'old comment', author: { accountId: 'testUser' } } as JiraComment;
        component.user = { accountId: 'testUser' } as JiraUser;
        component.jiraUrl = 'http://example.com';
        component.issueId = 'testIssueId';
        commentService.updateComment.and.returnValue(Promise.reject(new HttpErrorResponse({ error: 'error' })));

        await component.editComment('new comment');

        expect(commentService.updateComment).toHaveBeenCalled();
        expect(component.commentUpdateState).toBe(ValueChangeState.ServerError);
    });

    it('should emit change event on successful edit', async () => {
        component.comment = { id: 'testCommentId', body: 'old comment', author: { accountId: 'testUser' } } as JiraComment;
        component.user = { accountId: 'testUser' } as JiraUser;
        component.jiraUrl = 'http://example.com';
        component.issueId = 'testIssueId';
        const updatedComment = { id: 'testCommentId', body: 'new comment', author: { accountId: 'testUser' } } as JiraComment;
        commentService.updateComment.and.returnValue(Promise.resolve(updatedComment));
        spyOn(component.change, 'emit');

        await component.editComment('new comment');

        expect(component.change.emit).toHaveBeenCalledWith({
            oldValue:
            { id: 'testCommentId', body: 'old comment', author: { accountId: 'testUser' } } as JiraComment,
            newValue: updatedComment });
    });
});
