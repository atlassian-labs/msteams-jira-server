import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { ErrorResponseInterceptor } from './error-response.interceptor';
import { ErrorService, AppInsightsService } from '@core/services';
import { MatDialog } from '@angular/material/dialog';
import { StatusCode } from '@core/enums';

describe('ErrorResponseInterceptor', () => {
    let httpMock: HttpTestingController;
    let httpClient: HttpClient;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let matDialog: jasmine.SpyObj<MatDialog>;

    beforeEach(() => {
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', [
            'showAddonNotInstalledWindow',
            'goToLoginWithStatusCode',
            'showJiraUnavailableWindow',
            'showErrorModal'
        ]);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['trackEvent', 'trackException']);
        const matDialogSpy = jasmine.createSpyObj('MatDialog', ['closeAll']);

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                { provide: HTTP_INTERCEPTORS, useClass: ErrorResponseInterceptor, multi: true },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: MatDialog, useValue: matDialogSpy }
            ]
        });

        httpMock = TestBed.inject(HttpTestingController);
        httpClient = TestBed.inject(HttpClient);
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        matDialog = TestBed.inject(MatDialog) as jasmine.SpyObj<MatDialog>;
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should handle Unauthorized error by redirecting to login', fakeAsync(() => {
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(errorService.goToLoginWithStatusCode).toHaveBeenCalledWith(StatusCode.Unauthorized);
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: StatusCode.Unauthorized, statusText: 'Unauthorized' });
    }));

    it('should handle Forbidden error with addon not installed message', fakeAsync(() => {
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(errorService.showAddonNotInstalledWindow).toHaveBeenCalled();
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({ error: 'Addon is not installed' }, { status: StatusCode.Forbidden, statusText: 'Forbidden' });
    }));

    it('should handle Forbidden error with user not authorized message', fakeAsync(() => {
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(errorService.goToLoginWithStatusCode).toHaveBeenCalledWith(StatusCode.Forbidden);
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({ error: 'User not authorized' }, { status: StatusCode.Forbidden, statusText: 'Forbidden' });
    }));

    it('should handle ServiceUnavailable error by showing Jira unavailable window', fakeAsync(() => {
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(errorService.showJiraUnavailableWindow).toHaveBeenCalledWith(StatusCode.ServiceUnavailable);
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: StatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    }));

    it('should handle other errors by showing error modal', fakeAsync(() => {
        const badRequestStatus = 505;

        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(matDialog.closeAll).toHaveBeenCalled();
                expect(errorService.showErrorModal).toHaveBeenCalled();
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: badRequestStatus, statusText: 'Bad Request' });
    }));

    it('should log error response for Unauthorized and Forbidden errors', fakeAsync(() => {
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(appInsightsService.trackEvent).toHaveBeenCalledWith('Authentication failed.');
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: StatusCode.Unauthorized, statusText: 'Unauthorized' });
    }));

    it('should log error response for other errors', fakeAsync(() => {
        const badRequestStatus = 505;
        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(appInsightsService.trackException).toHaveBeenCalled();
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: badRequestStatus, statusText: 'Bad Request' });
    }));
});
