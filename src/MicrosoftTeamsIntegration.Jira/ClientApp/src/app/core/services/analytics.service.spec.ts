import { TestBed } from '@angular/core/testing';
import { AnalyticsService, EventType, UiEventSubject, EventAction } from './analytics.service';
import { UtilService, AppInsightsService } from '@core/services';

describe('AnalyticsService', () => {
    let service: AnalyticsService;
    let utilService: jasmine.SpyObj<UtilService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;

    beforeEach(() => {
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['getMsTeamsContext', 'capitalizeFirstLetterAndJoin']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['trackPageView']);

        TestBed.configureTestingModule({
            providers: [
                AnalyticsService,
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy }
            ]
        });

        service = TestBed.inject(AnalyticsService);
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should send UI event', () => {
        utilService.capitalizeFirstLetterAndJoin.and.returnValue('ButtonClicked');
        service.sendUiEvent('loginScreen', EventAction.clicked, UiEventSubject.button, 'authorizeJira', { source: 'loginScreen' });

        expect(utilService.capitalizeFirstLetterAndJoin).toHaveBeenCalledWith('authorizeJira', 'button', 'clicked');
        expect(appInsightsService.trackPageView).toHaveBeenCalledWith('ButtonClicked', jasmine.any(Object));
    });

    it('should send track event', () => {
        utilService.capitalizeFirstLetterAndJoin.and.returnValue('SigninSuccessful');
        service.sendTrackEvent('connectToJira', 'successful', 'signin', '', { source: 'connectToJira' });

        expect(utilService.capitalizeFirstLetterAndJoin).toHaveBeenCalledWith('', 'signin', 'successful');
        expect(appInsightsService.trackPageView).toHaveBeenCalledWith('SigninSuccessful', jasmine.any(Object));
    });

    it('should send screen event', () => {
        utilService.capitalizeFirstLetterAndJoin.and.returnValue('ConfigureTabViewed');
        service.sendScreenEvent('configureTab',
            EventAction.viewed,
            UiEventSubject.taskModule,
            'configureTab',
            { application: 'testApplication' });

        expect(utilService.capitalizeFirstLetterAndJoin).toHaveBeenCalledWith('configureTab', 'taskModule', 'viewed');
        expect(appInsightsService.trackPageView).toHaveBeenCalledWith('ConfigureTabViewed', jasmine.any(Object));
    });

    it('should get analytics environment from localStorage', () => {
        spyOn(localStorage, 'getItem').and.returnValue('staging');
        const analyticsEnvironment = service['getAnalyticsEnvironment']();
        expect(analyticsEnvironment).toBe('staging');
    });

    it('should default to dev if analytics environment is not set or invalid', () => {
        spyOn(localStorage, 'getItem').and.returnValue('invalid');
        const analyticsEnvironment = service['getAnalyticsEnvironment']();
        expect(analyticsEnvironment).toBe('dev');
    });
});
