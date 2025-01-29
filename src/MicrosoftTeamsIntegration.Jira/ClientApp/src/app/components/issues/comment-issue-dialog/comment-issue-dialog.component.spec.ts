import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { CommentIssueDialogComponent } from './comment-issue-dialog.component';
import { IssueCommentService } from '@core/services/entities/comment.service';
import { ApiService, ErrorService, AppInsightsService } from '@core/services';
import { PermissionService } from '@core/services/entities/permission.service';
import { NotificationService } from '@shared/services/notificationService';
import { AnalyticsService } from '@core/services/analytics.service';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { JiraPermissionsResponse } from '@core/models/Jira/jira-permission.model';
import { Issue, JiraComment } from '@core/models';
import * as microsoftTeams from '@microsoft/teams-js';

describe('CommentIssueDialogComponent', () => {
    let component: CommentIssueDialogComponent;
    let fixture: ComponentFixture<CommentIssueDialogComponent>;
    let commentService: jasmine.SpyObj<IssueCommentService>;
    let apiService: jasmine.SpyObj<ApiService>;
    let permissionService: jasmine.SpyObj<PermissionService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let notificationService: jasmine.SpyObj<NotificationService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let domSanitizer: jasmine.SpyObj<DomSanitizer>;
    let mockAppInsightsService: jasmine.SpyObj<AppInsightsService>;

    beforeEach(async () => {
        spyOn(microsoftTeams.app, 'notifySuccess').and.callFake(() => {});
        spyOn(microsoftTeams.dialog.url, 'submit').and.callFake(() => {});

        const commentServiceSpy = jasmine.createSpyObj('IssueCommentService', ['addComment']);
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['getJiraUrlForPersonalScope', 'getIssueByIdOrKey']);
        const permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['getMyPermissions']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['getHttpErrorMessage']);
        const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['notifyError', 'notifySuccess']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent']);
        const domSanitizerSpy = jasmine.createSpyObj('DomSanitizer', ['bypassSecurityTrustUrl']);
        const snackBarRefSpy = jasmine.createSpyObj('MatSnackBarRef', ['afterDismissed']);
        const appInsightsServiceRefSpy = jasmine.createSpyObj('AppInsightsService', ['trackException']);
        snackBarRefSpy.afterDismissed.and.returnValue(of({}));

        notificationServiceSpy.notifySuccess.and.returnValue(snackBarRefSpy);

        await TestBed.configureTestingModule({
            declarations: [CommentIssueDialogComponent],
            imports: [ReactiveFormsModule, RouterTestingModule],
            providers: [
                { provide: IssueCommentService, useValue: commentServiceSpy },
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: PermissionService, useValue: permissionServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: DomSanitizer, useValue: domSanitizerSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceRefSpy },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: {
                            params:
                            { jiraUrl: 'testJiraUrl', issueId: 'testIssueId', issueKey: 'testIssueKey', application: 'testApp' } }
                    }
                }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(CommentIssueDialogComponent);
        component = fixture.componentInstance;
        commentService = TestBed.inject(IssueCommentService) as jasmine.SpyObj<IssueCommentService>;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        permissionService = TestBed.inject(PermissionService) as jasmine.SpyObj<PermissionService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        domSanitizer = TestBed.inject(DomSanitizer) as jasmine.SpyObj<DomSanitizer>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize component and load issue data', async () => {
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'testJiraUrl' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                ADD_COMMENTS: { havePermission: true },
                EDIT_ALL_COMMENTS: { havePermission: false },
                EDIT_OWN_COMMENTS: { havePermission: false },
                DELETE_ALL_COMMENTS: { havePermission: false },
                DELETE_OWN_COMMENTS: { havePermission: false },
            }
        }) as Promise<JiraPermissionsResponse>);
        apiService.getIssueByIdOrKey.and.returnValue(Promise.resolve({
            key: 'testKey',
            fields: {
                summary: 'testSummary',
                issuetype: { name: 'Bug' },
                project: { key: 'TEST' },
                workratio: 0,
                watches: { isWatching: false, watchCount: 0 },
            },
            expand: '',
            id: '',
            self: ''
        }) as Promise<Issue>);

        await component.ngOnInit();

        expect(component.jiraUrl).toBe('testJiraUrl');
        expect(component.issueId).toBe('testIssueId');
        expect(component.issueKey).toBe('testIssueKey');
        expect(component.issue?.key).toBe('testKey');
        expect(component.issue?.fields.summary).toBe('testSummary');
    });

    it('should handle permission error during initialization', async () => {
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                ADD_COMMENTS: { havePermission: false }
            }
        }) as Promise<JiraPermissionsResponse>);
        const navigateSpy = spyOn(component['router'], 'navigate');

        await component.ngOnInit();

        expect(navigateSpy).toHaveBeenCalledWith(['/error'], { queryParams: { message: 'You don\'t have permissions to add comments' } });
    });

    it('should submit comment successfully', async () => {
        component.commentForm = new UntypedFormGroup({
            comment: new UntypedFormControl('test comment', Validators.required)
        });
        commentService.addComment.and.returnValue(Promise.resolve({
            id: '1',
            author: { displayName: 'Test User', accountId: 'testAccountId' },
            body: 'test comment',
            created: new Date().toISOString(),
            updated: new Date().toISOString()
        }) as Promise<JiraComment>);

        await component.onSubmit();

        expect(component.formDisabled).toBeTrue();
        expect(notificationService.notifySuccess).toHaveBeenCalled();
    });

    it('should handle error during comment submission', async () => {
        component.commentForm = new UntypedFormGroup({
            comment: new UntypedFormControl('test comment', Validators.required)
        });
        commentService.addComment.and.returnValue(Promise.reject('error'));
        errorService.getHttpErrorMessage.and.returnValue('Error message');

        await component.onSubmit();

        expect(notificationService.notifyError).toHaveBeenCalledWith('Error message');
        expect(component.formDisabled).toBeFalse();
    });

    it('should return sanitized URL', () => {
        const url = 'http://example.com';
        domSanitizer.bypassSecurityTrustUrl.and.returnValue(url);
        const sanitizedUrl = component.sanitazeUrl(url);
        expect(sanitizedUrl).toBe(url);
    });
});
