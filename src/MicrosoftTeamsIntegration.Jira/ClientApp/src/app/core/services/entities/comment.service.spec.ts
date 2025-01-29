import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { IssueCommentService } from './comment.service';
import { JiraComment } from '@core/models';
import { IssueUpdateCommentOptions, IssueAddCommentOptions } from '@core/models/Jira/issue-comment-options.model';

describe('IssueCommentService', () => {
    let service: IssueCommentService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [IssueCommentService]
        });

        service = TestBed.inject(IssueCommentService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should add a comment', () => {
        const dummyComment: JiraComment = {
            id: '1',
            body: 'This is a test comment',
            author: {
                accountId: '12345',
                displayName: 'John Doe',
                emailAddress: 'john.doe@example.com',
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                self: '',
                hashedEmailAddress: '',
                active: false,
                timeZone: '',
                name: ''
            },
            created: '',
            updated: '',
            self: ''
        };

        const options: IssueAddCommentOptions = {
            jiraUrl: 'http://example.com',
            issueIdOrKey: 'TEST-1',
            comment: 'This is a test comment',
            metadataRef: ''
        };

        service.addComment(options).then(data => {
            expect(data).toEqual(dummyComment);
        });

        const req = httpMock.expectOne('/api/addCommentAndGetItBack');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(options);
        req.flush(dummyComment);
    });

    it('should update a comment', () => {
        const dummyComment: JiraComment = {
            id: '1',
            body: 'This is an updated comment',
            author: {
                accountId: '12345',
                displayName: 'John Doe',
                emailAddress: 'john.doe@example.com',
                avatarUrls: {
                    '16x16': '',
                    '24x24': '',
                    '32x32': '',
                    '48x48': ''
                },
                self: '',
                hashedEmailAddress: '',
                active: false,
                timeZone: '',
                name: ''
            },
            created: '',
            updated: '',
            self: ''
        };

        const options: IssueUpdateCommentOptions = {
            jiraUrl: 'http://example.com',
            issueIdOrKey: 'TEST-1',
            commentId: '1',
            comment: 'This is an updated comment'
        };

        service.updateComment(options).then(data => {
            expect(data).toEqual(dummyComment);
        });

        const req = httpMock.expectOne('/api/updateComment');
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(options);
        req.flush(dummyComment);
    });
});
