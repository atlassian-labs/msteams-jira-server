import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LoginComponent } from './login.component';
import { AuthService, UtilService, ErrorService, AppInsightsService, RoutingState } from '@core/services';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusCode } from '@core/enums';

describe('LoginComponent', () => {
    let component: LoginComponent;
    let fixture: ComponentFixture<LoginComponent>;
    let authService: jasmine.SpyObj<AuthService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let router: jasmine.SpyObj<Router>;
    let routingState: jasmine.SpyObj<RoutingState>;
    let route: jasmine.SpyObj<ActivatedRoute>;

    beforeEach(async () => {
        const authServiceSpy = jasmine.createSpyObj('AuthService',
            ['isAuthenticated', 'authenticateJiraAccount', 'authenticateMicrosoftAccount']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['showDefaultError']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackWarning', 'trackException']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent', 'sendUiEvent']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
        const routingStateSpy = jasmine.createSpyObj('RoutingState', ['previousUrl']);
        const routeSpy = jasmine.createSpyObj('ActivatedRoute',
            { snapshot: { params: { status: 200, application: 'testApplication', jiraUrl: 'testJiraUrl' } } });

        await TestBed.configureTestingModule({
            declarations: [LoginComponent],
            imports: [RouterTestingModule],
            providers: [
                { provide: AuthService, useValue: authServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: ActivatedRoute, useValue: routeSpy },
                { provide: Router, useValue: routerSpy },
                { provide: RoutingState, useValue: routingStateSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(LoginComponent);
        component = fixture.componentInstance;
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
        routingState = TestBed.inject(RoutingState) as jasmine.SpyObj<RoutingState>;
        route = TestBed.inject(ActivatedRoute) as jasmine.SpyObj<ActivatedRoute>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        route.snapshot.params = { status: '200', application: 'testApplication', jiraUrl: 'testJiraUrl' };
        spyOn(component, 'ngOnInit').and.callThrough();

        await component.ngOnInit();

        expect(appInsightsService.logNavigation).toHaveBeenCalledWith('LoginComponent', jasmine.any(Object));
        expect(analyticsService.sendScreenEvent).toHaveBeenCalledWith(
            'loginScreen',
            'viewed',
            'uiView',
            'loginScreen',
            { application: 'testApplication' }
        );
        expect(component.buttonTitle).toBe('Sign in with Microsoft account');
        expect(component.isLoginButtonVisible).toBeTrue();
    });

    it('should call authenticateJiraAccount when Jira user is not authorized', async () => {
        route.snapshot.params = { status: StatusCode.Forbidden, application: 'testApplication', jiraUrl: 'testJiraUrl' };
        authService.isAuthenticated.and.returnValue(Promise.resolve(false));
        authService.authenticateJiraAccount.and.returnValue(Promise.resolve());

        component.ngOnInit();
        await component.getAuthentication();

        expect(authService.authenticateJiraAccount).toHaveBeenCalledWith('/#/config;application=testApplication');
    });

    it('should call authenticateJiraAccount for ApplicationType.JiraServerTab application when Jira user is not authorized', async () => {
        route.snapshot.params = { status: StatusCode.Forbidden, application: 'jiraServerTab', jiraUrl: 'testJiraUrl' };
        authService.isAuthenticated.and.returnValue(Promise.resolve(false));
        authService.authenticateJiraAccount.and.returnValue(Promise.resolve());

        component.ngOnInit();
        await component.getAuthentication();

        expect(authService.authenticateJiraAccount)
            .toHaveBeenCalledWith('/#/config;application=jiraServerTab;endpoint=%2FloginResult.html;jiraUrl=testJiraUrl');
    });

    it('should call authenticateMicrosoftAccount when Jira user is authorized', async () => {
        authService.isAuthenticated.and.returnValue(Promise.resolve(true));
        authService.authenticateMicrosoftAccount.and.returnValue(Promise.resolve());

        await component.getAuthentication();

        expect(authService.authenticateMicrosoftAccount).toHaveBeenCalled();
    });

    it('should handle authentication failure when authenticateJiraAccount failed', async () => {
        route.snapshot.params = { status: StatusCode.Forbidden, application: 'testApplication', jiraUrl: 'testJiraUrl' };
        authService.isAuthenticated.and.returnValue(Promise.resolve(false));
        authService.authenticateJiraAccount.and.throwError('error');

        component.ngOnInit();
        await component.getAuthentication();

        expect(component.isLoginButtonVisible).toBeTrue();
    });

    it('should handle authentication failure when authenticateMicrosoftAccount failed', async () => {
        authService.isAuthenticated.and.returnValue(Promise.resolve(true));
        authService.authenticateMicrosoftAccount.and.throwError('error');

        await component.getAuthentication();

        expect(component.isLoginButtonVisible).toBeTrue();
    });

    it('should navigate to the correct route on authentication success', async () => {
        route.snapshot.params = { status: '200', application: 'testApplication', jiraUrl: 'testJiraUrl', endpoint: '/issues' };;
        authService.isAuthenticated.and.returnValue(Promise.resolve(true));

        component.ngOnInit();
        await component.getAuthentication();

        expect(router.navigate).toHaveBeenCalledWith(['/issues', { jiraUrl: 'testJiraUrl' }]);
    });

    it('should show login button on authentication on authentication success, but navigation failed', async () => {
        route.snapshot.params = { status: '200', application: 'testApplication', jiraUrl: 'testJiraUrl', endpoint: '/issues' };;
        authService.isAuthenticated.and.returnValue(Promise.resolve(true));
        router.navigate.and.throwError('error');

        component.ngOnInit();
        await component.getAuthentication();

        expect(component.isLoginButtonVisible).toBeTrue();
    });

    it('should show login button on authentication failure', async () => {
        route.snapshot.params = { status: StatusCode.Forbidden, application: 'testApplication', jiraUrl: 'testJiraUrl' };
        authService.isAuthenticated.and.returnValue(Promise.resolve(true));
        authService.authenticateJiraAccount.and.returnValue(Promise.reject('error'));

        component.ngOnInit();
        await component.getAuthentication();

        expect(component.isLoginButtonVisible).toBeTrue();
    });
});
