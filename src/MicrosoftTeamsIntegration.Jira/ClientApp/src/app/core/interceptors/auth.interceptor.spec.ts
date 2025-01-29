import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { AuthInterceptor, ALLOW_ANONYMOUS_ENDPOINTS } from './auth.interceptor';
import { AuthService, ErrorService } from '@core/services';
import { StatusCode } from '@core/enums';

describe('AuthInterceptor', () => {
    let httpMock: HttpTestingController;
    let httpClient: HttpClient;
    let authService: jasmine.SpyObj<AuthService>;
    let errorService: jasmine.SpyObj<ErrorService>;

    beforeEach(() => {
        const authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['goToLoginWithStatusCode']);

        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
                { provide: AuthService, useValue: authServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy }
            ]
        });

        httpMock = TestBed.inject(HttpTestingController);
        httpClient = TestBed.inject(HttpClient);
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should add Authorization header for protected endpoints', fakeAsync(() => {
        authService.getToken.and.returnValue(Promise.resolve('test-token'));

        httpClient.get('/api/protected-endpoint').subscribe();

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({});

        expect(req.request.headers.has('Authorization')).toBeTrue();
        expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    }));

    it('should not add Authorization header for anonymous endpoints', fakeAsync(() => {
        httpClient.get(ALLOW_ANONYMOUS_ENDPOINTS[0]).subscribe();

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne(ALLOW_ANONYMOUS_ENDPOINTS[0]);
        req.flush({});

        expect(req.request.headers.has('Authorization')).toBeFalse();
    }));

    it('should handle Unauthorized error by redirecting to login', fakeAsync(() => {
        authService.getToken.and.returnValue(Promise.resolve('test-token'));

        httpClient.get('/api/protected-endpoint').subscribe({
            error: () => {
                expect(errorService.goToLoginWithStatusCode).toHaveBeenCalledWith(StatusCode.Unauthorized);
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: StatusCode.Unauthorized, statusText: 'Unauthorized' });
    }));

    it('should pass through other errors', fakeAsync(() => {
        const badRequestStatus = 505;
        authService.getToken.and.returnValue(Promise.resolve('test-token'));

        httpClient.get('/api/protected-endpoint').subscribe({
            error: (error) => {
                expect(error.status).toBe(badRequestStatus);
            }
        });

        tick(); // Simulate the passage of time for async operations

        const req = httpMock.expectOne('/api/protected-endpoint');
        req.flush({}, { status: badRequestStatus, statusText: 'Bad Request' });
    }));
});
