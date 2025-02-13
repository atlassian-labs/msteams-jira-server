import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NewCommentComponent } from './new-comment.component';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { DomSanitizer } from '@angular/platform-browser';
import { AnalyticsService } from '@core/services/analytics.service';
import { JiraComment } from '@core/models';
import { ValueChangeState } from '@core/enums/value-change-status.enum';
import { HttpErrorResponse } from '@angular/common/http';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('NewCommentComponent', () => {
    let component: NewCommentComponent;
    let fixture: ComponentFixture<NewCommentComponent>;
    let commentService: jasmine.SpyObj<IssueCommentService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;

    beforeEach(async () => {
        const commentServiceSpy = jasmine.createSpyObj('IssueCommentService', ['addComment']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendUiEvent']);

        await TestBed.configureTestingModule({
            declarations: [NewCommentComponent],
            providers: [
                { provide: IssueCommentService, useValue: commentServiceSpy },
                { provide: DomSanitizer, useValue: { bypassSecurityTrustUrl: (url: string) => url } },
                { provide: AnalyticsService, useValue: analyticsServiceSpy }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(NewCommentComponent);
        component = fixture.componentInstance;
        commentService = TestBed.inject(IssueCommentService) as jasmine.SpyObj<IssueCommentService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should send comment successfully', async () => {
        const newComment: JiraComment = {
            id: '1',
            author: { displayName: 'Test User', accountId: 'testAccountId' },
            body: 'test comment',
            created: new Date().toISOString(),
            updated: new Date().toISOString()
        } as JiraComment;
        commentService.addComment.and.returnValue(Promise.resolve(newComment));
        spyOn(component.created, 'emit');
        const inputElRef = { value: 'test comment' } as HTMLTextAreaElement;

        await component.sendComment('test comment', inputElRef);

        expect(commentService.addComment).toHaveBeenCalled();
        expect(component.created.emit).toHaveBeenCalledWith(newComment);
        expect(component.commentSendState).toBe(ValueChangeState.Success);
        expect(inputElRef.value).toBe('');
    });

    it('should handle empty comment', async () => {
        const inputElRef = { value: '' } as HTMLTextAreaElement;

        await component.sendComment('', inputElRef);

        expect(component.commentSendState).toBe(ValueChangeState.InvalidEmpty);
    });

    it('should handle comment submission failure', async () => {
        commentService.addComment.and.returnValue(Promise.reject(new HttpErrorResponse({ error: 'error' })));
        const inputElRef = { value: 'test comment' } as HTMLTextAreaElement;

        await component.sendComment('test comment', inputElRef);

        expect(commentService.addComment).toHaveBeenCalled();
        expect(component.commentSendState).toBe(ValueChangeState.ServerError);
    });
});
