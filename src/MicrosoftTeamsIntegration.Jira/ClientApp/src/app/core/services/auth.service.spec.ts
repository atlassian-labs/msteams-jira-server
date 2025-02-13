import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { ErrorService, AppLoadService } from '@core/services';
import { DOCUMENT } from '@angular/common';
import * as microsoftTeams from '@microsoft/teams-js';
import { StatusCode } from '@core/enums';

describe('AuthService', () => {
    let service: AuthService;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appLoadService: jasmine.SpyObj<AppLoadService>;
    let document: Document;
    let getAuthTokenSpy: jasmine.Spy;

    beforeEach(() => {
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['goToLoginWithStatusCode']);
        const appLoadServiceSpy = jasmine.createSpyObj('AppLoadService', ['getSettings']);
        const mockDocument = {
            ...document,
            location: {
                ...document?.location,
                href: ''
            }
        };

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                AuthService,
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppLoadService, useValue: appLoadServiceSpy },
                { provide: DOCUMENT, useValue: mockDocument }
            ]
        });

        service = TestBed.inject(AuthService);
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appLoadService = TestBed.inject(AppLoadService) as jasmine.SpyObj<AppLoadService>;
        document = TestBed.inject(DOCUMENT);
        getAuthTokenSpy = spyOn(microsoftTeams.authentication, 'getAuthToken');
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should return true if user is authenticated', async () => {
        spyOn(service, 'getToken').and.callFake(() => Promise.resolve('test-token'));

        const isAuthenticated = await service.isAuthenticated();

        expect(isAuthenticated).toBeTrue();
    });

    it('should return false if user is not authenticated', async () => {
        spyOn(service, 'getToken').and.callFake(() => Promise.resolve(undefined));

        const isAuthenticated = await service.isAuthenticated();

        expect(isAuthenticated).toBeFalse();
    });

    it('should authenticate Microsoft account', async () => {
        const settings = {
            clientId: 'test-client-id',
            microsoftLoginBaseUrl: 'https://login.microsoftonline.com'
        };
        appLoadService.getSettings.and.returnValue(Promise.resolve(settings));
        spyOn(microsoftTeams.authentication, 'authenticate').and.callFake((params: any) => params.successCallback());

        await service.authenticateMicrosoftAccount();

        expect(microsoftTeams.authentication.authenticate).toHaveBeenCalled();
    });

    it('should handle error when authenticating Microsoft account', async () => {
        const settings = {
            clientId: 'test-client-id',
            microsoftLoginBaseUrl: 'https://login.microsoftonline.com'
        };
        appLoadService.getSettings.and.returnValue(Promise.resolve(settings));
        spyOn(microsoftTeams.authentication, 'authenticate').and.callFake((params: any) => params.failureCallback('error'));

        try {
            await service.authenticateMicrosoftAccount();
        } catch (error) {
            expect(error).toBe('error');
        }
    });

    it('should handle error when authenticating Jira account', async () => {
        spyOn(microsoftTeams.authentication, 'authenticate').and.callFake((params: any) => params.failureCallback('error'));

        try {
            await service.authenticateJiraAccount('https://jira.example.com');
        } catch (error) {
            expect(error).toBe('error');
        }
    });

    it('should authenticate Jira account', async () => {
        spyOn(microsoftTeams.authentication, 'authenticate').and.callFake((params: any) => params.successCallback());

        await service.authenticateJiraAccount('https://jira.example.com');

        expect(microsoftTeams.authentication.authenticate).toHaveBeenCalled();
    });

    it('should handle error when authenticating Jira account', async () => {
        spyOn(microsoftTeams.authentication, 'authenticate').and.callFake((params: any) => params.failureCallback('error'));

        try {
            await service.authenticateJiraAccount('https://jira.example.com');
        } catch (error) {
            expect(error).toBe('error');
        }
    });

    it('should get token', async () => {
        getAuthTokenSpy.and.callFake(() => Promise.resolve('test-token'));

        const token = await service.getToken();

        expect(token).toBe('test-token');
    });

    it('should handle other error when getting token', async () => {
        getAuthTokenSpy.and.callFake(() => Promise.reject(new Error('error')));

        const token = await service.getToken();

        expect(token).toBeUndefined();
        expect(errorService.goToLoginWithStatusCode).toHaveBeenCalledTimes(0);
    });

    it('should handle consent error when getting token', async () => {
        [
            { error: 'resourceRequiresConsent', statusCode: StatusCode.Unauthorized },
            { error: 'resourceRequiresMfa', statusCode: StatusCode.Unauthorized },
            { error: 'tokenRevoked', statusCode: StatusCode.Unauthorized },
        ].forEach(async ({ error, statusCode }) => {
            getAuthTokenSpy.and.callFake(() => Promise.reject(new Error(error)));

            await service.getToken();

            expect(errorService.goToLoginWithStatusCode).toHaveBeenCalledWith(statusCode);
        });
    });
});
