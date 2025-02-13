import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, HttpClientModule, HttpClient } from '@angular/common/http';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AuthGuard } from '@core/guards/auth.guard';
import { AuthInterceptor } from '@core/interceptors/auth.interceptor';
import { ErrorResponseInterceptor } from '@core/interceptors/error-response.interceptor';
import { AppLoadService } from '@core/services/app-load.service';
import { CoreModule, getSettingsFactory } from './core.module';

describe('CoreModule', () => {
    let appLoadService: jasmine.SpyObj<AppLoadService>;

    beforeEach(() => {
        const appLoadServiceSpy = jasmine.createSpyObj('AppLoadService', ['getSettings']);

        TestBed.configureTestingModule({
            imports: [HttpClientModule],
            providers: [
                CoreModule,
                AuthGuard,
                { provide: AppLoadService, useValue: appLoadServiceSpy },
                {
                    provide: HTTP_INTERCEPTORS,
                    useClass: ErrorResponseInterceptor,
                    multi: true
                },
                {
                    provide: HTTP_INTERCEPTORS,
                    useClass: AuthInterceptor,
                    multi: true
                },
                provideHttpClient(withInterceptorsFromDi())
            ]
        });

        appLoadService = TestBed.inject(AppLoadService) as jasmine.SpyObj<AppLoadService>;
    });

    it('should create the module', () => {
        const module = TestBed.inject(CoreModule);
        expect(module).toBeTruthy();
    });

    it('should initialize settings on module load', async () => {
        appLoadService.getSettings.and.returnValue(Promise.resolve());

        const initializerFn = getSettingsFactory(appLoadService);
        await initializerFn();

        expect(appLoadService.getSettings).toHaveBeenCalled();
    });

    it('should handle error when initializing settings', async () => {
        appLoadService.getSettings.and.returnValue(Promise.reject('error'));

        try {
            const initializerFn = getSettingsFactory(appLoadService);
            await initializerFn();
        } catch (error) {
            expect(error).toBe('error');
        }
    });
});
