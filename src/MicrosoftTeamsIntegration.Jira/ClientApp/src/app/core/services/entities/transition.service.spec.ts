import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { IssueTransitionService } from './transition.service';
import { JiraTransition, JiraTransitionsResponse } from '@core/models/Jira/jira-transition.model';
import { JiraApiActionCallResponse } from '@core/models/Jira/jira-api-action-call-response.model';

describe('IssueTransitionService', () => {
    let service: IssueTransitionService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [IssueTransitionService]
        });

        service = TestBed.inject(IssueTransitionService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should call getTransitions with correct URL and parameters', async () => {
        const jiraUrl = 'http://example.com';
        const issueIdOrKey = '10001';
        const mockResponse: JiraTransitionsResponse = { expand: '', transitions: [{} as JiraTransition] };

        service.getTransitions(jiraUrl, issueIdOrKey).then(response => {
            expect(response).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`);
        expect(req.request.method).toBe('GET');

        req.flush(mockResponse);
    });

    it('should call doTransition with correct URL and parameters', async () => {
        const jiraUrl = 'http://example.com';
        const issueIdOrKey = '10001';
        const transitionId = '5';
        const mockResponse: JiraApiActionCallResponse = { isSuccess: true };

        service.doTransition(jiraUrl, issueIdOrKey, transitionId).then(response => {
            expect(response).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ transition: { id: transitionId } });

        req.flush(mockResponse);
    });

    it('should handle error response for getTransitions', async () => {
        const jiraUrl = 'http://example.com';
        const issueIdOrKey = '10001';

        service.getTransitions(jiraUrl, issueIdOrKey).catch(error => {
            expect(error.status).toBe(500);
        });

        const req = httpMock.expectOne(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`);
        req.flush('Error', { status: 500, statusText: 'Server Error' });
    });

    it('should handle error response for doTransition', async () => {
        const jiraUrl = 'http://example.com';
        const issueIdOrKey = '10001';
        const transitionId = '5';

        service.doTransition(jiraUrl, issueIdOrKey, transitionId).catch(error => {
            expect(error.status).toBe(500);
        });

        const req = httpMock.expectOne(`/api/issue/transitions?jiraUrl=${jiraUrl}&issueIdOrKey=${issueIdOrKey}`);
        req.flush('Error', { status: 500, statusText: 'Server Error' });
    });
});
