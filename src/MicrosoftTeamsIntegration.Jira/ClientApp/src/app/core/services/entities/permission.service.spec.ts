import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PermissionService } from './permission.service';
import { UtilService } from '@core/services/util.service';
import { JiraPermissionsResponse, JiraPermissionName, JiraPermission, JiraPermissions } from '@core/models/Jira/jira-permission.model';

describe('PermissionService', () => {
    let service: PermissionService;
    let httpMock: HttpTestingController;
    let utilService: UtilService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [PermissionService, UtilService]
        });

        service = TestBed.inject(PermissionService);
        httpMock = TestBed.inject(HttpTestingController);
        utilService = TestBed.inject(UtilService);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should call getMyPermissions with correct URL and parameters (single permission)', async () => {
        const jiraUrl = 'http://example.com';
        const permissions: JiraPermissionName = 'ADD_COMMENTS';
        const mockResponse: JiraPermissionsResponse = {
            permissions: {
                ADD_COMMENTS: {
                    id: '1',
                    key: 'ADD_COMMENTS',
                    name: 'Add Comments',
                    type: 'JiraPermission',
                    description: 'Add comments to issues',
                    havePermission: true
                } as unknown as JiraPermission
            } as JiraPermissions
        };

        spyOn(utilService, 'appendParamsToLink').and.callThrough();

        service.getMyPermissions(jiraUrl, permissions).then(response => {
            expect(response).toEqual(mockResponse);
        });

        const req = httpMock.expectOne((request) => request.url.includes('/api/mypermissions'));
        expect(req.request.method).toBe('GET');
        expect(utilService.appendParamsToLink).toHaveBeenCalledWith(
            '/api/mypermissions',
            { jiraUrl, permissions: 'ADD_COMMENTS', issueId: undefined, projectKey: undefined }
        );

        req.flush(mockResponse);
    });

    it('should call getMyPermissions with correct URL and parameters (multiple permissions)', async () => {
        const jiraUrl = 'http://example.com';
        const permissions: JiraPermissionName[] = ['ADD_COMMENTS', 'DELETE_ALL_COMMENTS'];
        const mockResponse: JiraPermissionsResponse = {
            permissions: {
                ADD_COMMENTS: {
                    id: '1',
                    key: 'ADD_COMMENTS',
                    name: 'Add Comments',
                    type: 'JiraPermission',
                    description: 'Add comments to issues',
                    havePermission: true
                } as unknown as JiraPermission,
                DELETE_ALL_COMMENTS: {
                    id: '2',
                    key: 'DELETE_ALL_COMMENTS',
                    name: 'Delete All Comments',
                    type: 'JiraPermission',
                    description: 'Delete all comments',
                    havePermission: false
                } as unknown as JiraPermission
            } as JiraPermissions,
        };

        spyOn(utilService, 'appendParamsToLink').and.callThrough();

        service.getMyPermissions(jiraUrl, permissions).then(response => {
            expect(response).toEqual(mockResponse);
        });

        const req = httpMock.expectOne((request) => request.url.includes('/api/mypermissions'));
        expect(req.request.method).toBe('GET');
        expect(utilService.appendParamsToLink).toHaveBeenCalledWith(
            '/api/mypermissions',
            { jiraUrl, permissions: 'ADD_COMMENTS,DELETE_ALL_COMMENTS', issueId: undefined, projectKey: undefined }
        );

        req.flush(mockResponse);
    });

    it('should call getMyPermissions with issueId and projectKey', async () => {
        const jiraUrl = 'http://example.com';
        const permissions: JiraPermissionName = 'ADD_COMMENTS';
        const issueId = '10001';
        const projectKey = 'PROJ';
        const mockResponse: JiraPermissionsResponse = {
            permissions: {
                ADD_COMMENTS: {
                    id: '1',
                    key: 'ADD_COMMENTS',
                    name: 'Add Comments',
                    type: 'JiraPermission',
                    description: 'Add comments to issues',
                    havePermission: true
                } as unknown as JiraPermission
            } as JiraPermissions
        };

        spyOn(utilService, 'appendParamsToLink').and.callThrough();

        service.getMyPermissions(jiraUrl, permissions, issueId, projectKey).then(response => {
            expect(response).toEqual(mockResponse);
        });

        const req = httpMock.expectOne((request) => request.url.includes('/api/mypermissions'));
        expect(req.request.method).toBe('GET');
        expect(utilService.appendParamsToLink).toHaveBeenCalledWith(
            '/api/mypermissions',
            { jiraUrl, permissions: 'ADD_COMMENTS', issueId, projectKey }
        );

        req.flush(mockResponse);
    });
});
