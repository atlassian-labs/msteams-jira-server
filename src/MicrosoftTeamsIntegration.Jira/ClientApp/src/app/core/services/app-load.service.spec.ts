import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AppLoadService } from './app-load.service';

describe('AppLoadService', () => {
    let service: AppLoadService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [AppLoadService]
        });

        service = TestBed.inject(AppLoadService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get settings and store them in localStorage', async () => {
        const mockSettings = {
            clientId: 'test-client-id',
            instrumentationKey: 'test-instrumentation-key',
            baseUrl: 'https://test-base-url.com',
            analyticsEnvironment: 'test-environment',
            version: '1.0.0',
            microsoftLoginBaseUrl: 'https://login.microsoftonline.com'
        };

        service.getSettings().then((settings) => {
            expect(settings).toEqual(mockSettings);
            expect(localStorage.getItem('userClientId')).toBe(mockSettings.clientId);
            expect(localStorage.getItem('instrumentationKey')).toBe(mockSettings.instrumentationKey);
            expect(localStorage.getItem('baseUrl')).toBe(mockSettings.baseUrl);
            expect(localStorage.getItem('analyticsEnvironment')).toBe(mockSettings.analyticsEnvironment);
            expect(localStorage.getItem('version')).toBe(mockSettings.version);
            expect(localStorage.getItem('microsoftLoginBaseUrl')).toBe(mockSettings.microsoftLoginBaseUrl);
        });

        const req = httpMock.expectOne('/api/app-settings');
        expect(req.request.method).toBe('GET');
        req.flush(mockSettings);
    });

    it('should handle error when getting settings', async () => {
        const errorMessage = 'Failed to load settings';

        service.getSettings().catch((error) => {
            expect(error).toBe(errorMessage);
        });

        const req = httpMock.expectOne('/api/app-settings');
        expect(req.request.method).toBe('GET');
        req.flush({ message: errorMessage }, { status: 500, statusText: 'Server Error' });
    });
});
