import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { SettingsComponent } from './settings.component';
import { ApiService, ErrorService, AppInsightsService, UtilService } from '@core/services';
import { AnalyticsService, EventAction, UiEventSubject } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';

describe('SettingsComponent', () => {
    let component: SettingsComponent;
    let fixture: ComponentFixture<SettingsComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let router: jasmine.SpyObj<Router>;
    let route: jasmine.SpyObj<ActivatedRoute>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['getMyselfData']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['showDefaultError']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
        const routeSpy = jasmine.createSpyObj('ActivatedRoute',
            { snapshot: { params: { jiraUrl: 'testJiraUrl', displayName: 'testUser' } } });

        await TestBed.configureTestingModule({
            declarations: [SettingsComponent],
            imports: [RouterTestingModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: ActivatedRoute, useValue: routeSpy },
                { provide: Router, useValue: routerSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(SettingsComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
        route = TestBed.inject(ActivatedRoute) as jasmine.SpyObj<ActivatedRoute>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        route.snapshot.params = { jiraUrl: 'testJiraUrl', displayName: 'testUser' };
        spyOn(component, 'ngOnInit').and.callThrough();

        await component.ngOnInit();

        expect(appInsightsService.logNavigation).toHaveBeenCalledWith('SettingsComponent', jasmine.any(Object));
        expect(analyticsService.sendScreenEvent).toHaveBeenCalledWith(
            'configureTab',
            EventAction.viewed,
            UiEventSubject.taskModule,
            'configureTab'
        );
        expect(component.username).toBe('testUser');
    });

    it('should set user settings from api call', async () => {
        route.snapshot.params = { jiraUrl: 'testJiraUrl' };
        apiService.getMyselfData.and.returnValue(Promise.resolve({ displayName: 'testUser', accountId: 'testAccountId' }));
        component.username = undefined;

        await component.ngOnInit();

        expect(apiService.getMyselfData).toHaveBeenCalledWith('testJiraUrl');
        expect(component.username).toBe('testUser');
    });

    it('should handle error when setting user settings', async () => {
        route.snapshot.params = { jiraUrl: 'testJiraUrl' };
        apiService.getMyselfData.and.throwError('error');

        await component.ngOnInit();

        expect(errorService.showDefaultError).toHaveBeenCalledWith(jasmine.any(Error));
    });

    it('should navigate to signout dialog on signOut', async () => {
        route.snapshot.params = { jiraUrl: 'testJiraUrl', displayName: 'testUser' };

        await component.ngOnInit();
        await component.signOut();

        expect(router.navigate).toHaveBeenCalledWith(['/settings/signout-dialog', { jiraUrl: 'testJiraUrl', displayName: 'testUser' }]);
    });
});
