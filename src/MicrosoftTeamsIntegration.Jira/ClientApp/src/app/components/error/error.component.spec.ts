import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ErrorComponent } from './error.component';
import { ErrorService, AppInsightsService } from '@core/services';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';

describe('ErrorComponent', () => {
    let component: ErrorComponent;
    let fixture: ComponentFixture<ErrorComponent>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let router: jasmine.SpyObj<Router>;

    beforeEach(async () => {
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['showRetryButton', 'redirectOnRetryRoute']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

        await TestBed.configureTestingModule({
            declarations: [ErrorComponent],
            imports: [RouterTestingModule],
            providers: [
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: ActivatedRoute, useValue:
                        { snapshot: { queryParams: { message: 'Test error message', status: 404, iconType: 'connect' } } } },
                { provide: Router, useValue: routerSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ErrorComponent);
        component = fixture.componentInstance;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    });

    it('should create component', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', () => {
        spyOn(component, 'ngOnInit').and.callThrough();

        component.ngOnInit();

        expect(appInsightsService.logNavigation).toHaveBeenCalledWith('ErrorComponent', jasmine.any(Object));
        expect(analyticsService.sendScreenEvent).toHaveBeenCalledWith(
            'errorScreen',
            'viewed',
            'uiView',
            'errorScreen'
        );
        expect(component.showRetryButton).toBe(errorService.showRetryButton);
        expect(component.message).toBe('Test error message');
        expect(component.status).toBe(404);
        expect(component.iconType).toBe('connect');
        expect(component.iconUrl).toContain('jira-integration-2.png');
    });

    it('should navigate on retry', () => {
        component.retry();

        expect(router.navigate).toHaveBeenCalledWith([errorService.redirectOnRetryRoute, jasmine.any(Object)]);
    });
});
