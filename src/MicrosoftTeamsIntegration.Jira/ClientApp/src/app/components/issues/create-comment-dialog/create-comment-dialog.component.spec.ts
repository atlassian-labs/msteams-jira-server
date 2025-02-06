import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UntypedFormGroup, UntypedFormControl, ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { CreateCommentDialogComponent } from './create-comment-dialog.component';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { ApiService, ErrorService, AppInsightsService } from '@core/services';
import { UtilService } from '@core/services/util.service';
import { NotificationService } from '@shared/services/notificationService';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute } from '@angular/router';
import { Issue, JiraComment, JiraIssuesSearch } from '@core/models';
import { ListKeyManager, ListKeyManagerOption } from '@angular/cdk/a11y';
import { UP_ARROW, DOWN_ARROW, ENTER, TAB } from '@angular/cdk/keycodes';

describe('CreateCommentDialogComponent', () => {
    let component: CreateCommentDialogComponent;
    let fixture: ComponentFixture<CreateCommentDialogComponent>;
    let mockCommentService: jasmine.SpyObj<IssueCommentService>;
    let mockApiService: jasmine.SpyObj<ApiService>;
    let mockErrorService: jasmine.SpyObj<ErrorService>;
    let mockNotificationService: jasmine.SpyObj<NotificationService>;
    let mockAppInsightsService: jasmine.SpyObj<AppInsightsService>;
    let mockAnalyticsService: jasmine.SpyObj<AnalyticsService>;
    let mockActivatedRoute: any;

    beforeEach(async () => {
        mockCommentService = jasmine.createSpyObj('IssueCommentService', ['addComment']);
        mockApiService = jasmine.createSpyObj('ApiService', ['getIssues']);
        mockErrorService = jasmine.createSpyObj('ErrorService', ['getHttpErrorMessage']);
        mockNotificationService = jasmine.createSpyObj('NotificationService', ['notifyError', 'notifySuccess']);
        mockAppInsightsService = jasmine.createSpyObj('AppInsightsService', ['trackException']);
        mockAnalyticsService = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent']);
        mockActivatedRoute = {
            snapshot: {
                params: {
                    metadataRef: 'testMetadataRef',
                    jiraUrl: 'testJiraUrl',
                    jiraId: 'testJiraId',
                    comment: 'testComment',
                    issueUrl: 'https://localhost.com/browse/SCRUM-14',
                    application: 'testApplication'
                }
            }
        };

        await TestBed.configureTestingModule({
            declarations: [CreateCommentDialogComponent],
            imports: [ReactiveFormsModule],
            providers: [
                { provide: IssueCommentService, useValue: mockCommentService },
                { provide: ApiService, useValue: mockApiService },
                { provide: ErrorService, useValue: mockErrorService },
                { provide: NotificationService, useValue: mockNotificationService },
                { provide: AppInsightsService, useValue: mockAppInsightsService },
                { provide: AnalyticsService, useValue: mockAnalyticsService },
                { provide: ActivatedRoute, useValue: mockActivatedRoute },
                UtilService
            ]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(CreateCommentDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', async () => {
        await component.ngOnInit();
        expect(component.loading).toBeFalse();
        expect(component.defaultComment).toBe('testComment');
        expect(component.jiraUrl).toBe('testJiraUrl');
        expect(component.jiraId).toBe('testJiraId');
        expect(component.defaultSearchTerm).toBe('SCRUM-14');
    });

    it('should handle form submission successfully', async () => {
        component.commentForm = new UntypedFormGroup({
            comment: new UntypedFormControl('test comment')
        });
        component.selectedIssue = { id: 'testIssueId' } as Issue;
        mockCommentService.addComment.and.returnValue(Promise.resolve({
            id: '1',
            author: { displayName: 'Test User', accountId: 'testAccountId' },
            body: 'test comment',
            created: new Date().toISOString(),
            updated: new Date().toISOString()
        } as JiraComment));

        await component.onSubmit();

        expect(mockCommentService.addComment).toHaveBeenCalled();
        expect(mockNotificationService.notifySuccess).toHaveBeenCalled();
    });

    it('should handle form submission failure', async () => {
        component.commentForm = new UntypedFormGroup({
            comment: new UntypedFormControl('test comment')
        });
        component.selectedIssue = { id: 'testIssueId' } as Issue;
        mockCommentService.addComment.and.returnValue(Promise.reject(new Error('error')));
        mockErrorService.getHttpErrorMessage.and.returnValue('error message');

        await component.onSubmit();

        expect(mockCommentService.addComment).toHaveBeenCalled();
        expect(mockNotificationService.notifyError).toHaveBeenCalledWith('error message');
    });

    it('should select an issue', () => {
        const issue = { id: 'testIssueId' } as Issue;
        component.onSelectIssue(issue);
        expect(component.selectedIssue).toBe(issue);
    });

    it('should search for issues', async () => {
        const issues = [{ id: 'testIssueId' }] as Issue[];
        mockApiService.getIssues.and.returnValue(Promise.resolve({ issues } as JiraIssuesSearch));

        await component.search('test search term');

        expect(mockApiService.getIssues).toHaveBeenCalled();
        expect(component.issues).toEqual(issues);
    });

    it('should handle focus out event', () => {
        component.activeIssue = { id: 'testIssueId' } as Issue;
        component.handleListFocusOut();
        expect(component.activeIssue).toBeUndefined();
    });

    it('should handle list key up events', () => {
        const issues = [{ id: 'testIssueId' }] as Issue[];
        component.issues = issues;
        component['keyboardEventsManager'] = new ListKeyManager(component['issues'] as ListKeyManagerOption[]);
        const event = new KeyboardEvent('keyup', { keyCode: DOWN_ARROW });
        spyOn(event, 'stopImmediatePropagation');
        component.handleListKeyUp(event);
        expect(event.stopImmediatePropagation).toHaveBeenCalled();
        expect(component.activeIssue).toBe(issues[0]);
    });

    it('should handle list key up events for TAB key', () => {
        const issues = [{ id: 'testIssueId' }] as Issue[];
        component.issues = issues;
        component['keyboardEventsManager'] = new ListKeyManager(component['issues'] as ListKeyManagerOption[]);
        const event = new KeyboardEvent('keyup', { keyCode: TAB });
        spyOn(event, 'stopImmediatePropagation');
        component.handleListKeyUp(event);
        expect(event.stopImmediatePropagation).toHaveBeenCalled();
        expect(component.activeIssue).toBe(issues[0]);
    });
});
