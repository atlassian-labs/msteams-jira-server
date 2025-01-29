import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { ConnectJiraComponent } from './connect-jira.component';
import { ApiService, AuthService, ErrorService, AppInsightsService, JiraAddonStatus } from '@core/services';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';
import * as microsoftTeams from '@microsoft/teams-js';
import { AddonStatus, ApplicationType } from '@core/enums';

describe('ConnectJiraComponent', () => {
    let component: ConnectJiraComponent;
    let fixture: ComponentFixture<ConnectJiraComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let authService: jasmine.SpyObj<AuthService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let loadingIndicatorService: jasmine.SpyObj<LoadingIndicatorService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let router: jasmine.SpyObj<Router>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService',
            ['getAddonStatus', 'saveJiraServerId', 'submitLoginInfo', 'getMyselfData']);
        const authServiceSpy = jasmine.createSpyObj('AuthService', ['authenticateMicrosoftAccount']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['showDefaultError']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation']);
        const loadingIndicatorServiceSpy = jasmine.createSpyObj('LoadingIndicatorService', ['show', 'hide']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent', 'sendUiEvent', 'sendTrackEvent']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

        await TestBed.configureTestingModule({
            declarations: [ConnectJiraComponent],
            imports: [RouterTestingModule, ReactiveFormsModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: AuthService, useValue: authServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: LoadingIndicatorService, useValue: loadingIndicatorServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: ActivatedRoute, useValue:
                        { snapshot: { params: { endpoint: 'testEndpoint', application: 'testApplication' } } } },
                { provide: Router, useValue: routerSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ConnectJiraComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        loadingIndicatorService = TestBed.inject(LoadingIndicatorService) as jasmine.SpyObj<LoadingIndicatorService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

        spyOn(microsoftTeams.app, 'initialize').and.returnValue(Promise.resolve());
        spyOn(microsoftTeams.app, 'notifySuccess').and.returnValue(await Promise.resolve());
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        spyOn(component, 'ngOnInit').and.callThrough();

        await component.ngOnInit();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(analyticsService.sendScreenEvent).toHaveBeenCalled();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should handle onSubmitConnectForm correctly', async () => {
        const jiraServerId = 'testJiraId';
        component.connectForm = { get: () => ({ value: jiraServerId }) } as any;
        apiService.getAddonStatus.and
            .returnValue(Promise.resolve({ addonStatus: AddonStatus.Connected, addonVersion: '1.0' } as JiraAddonStatus));
        apiService.saveJiraServerId.and.returnValue(Promise.resolve({ isSuccess: true } as any));

        await component.onSubmitConnectForm();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(apiService.getAddonStatus).toHaveBeenCalledWith(jiraServerId);
        expect(apiService.saveJiraServerId).toHaveBeenCalledWith(jiraServerId);
        expect(component.showLoginForm).toBeFalse();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should handle onSubmitConnectForm when getAddonStatus returns AddonStatus.NotInstalled', async () => {
        const jiraServerId = 'testJiraId';
        component.connectForm = { get: () => ({ value: jiraServerId }) } as any;
        apiService.getAddonStatus.and
            .returnValue(Promise.resolve({ addonStatus: AddonStatus.NotInstalled, addonVersion: '1.0' } as JiraAddonStatus));

        await component.onSubmitConnectForm();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(apiService.getAddonStatus).toHaveBeenCalledWith(jiraServerId);
        expect(component.showAddonStatusError).toBeTrue();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should handle onSubmitConnectForm when getAddonStatus returns AddonStatus.Installed', async () => {
        const jiraServerId = 'testJiraId';
        component.connectForm = { get: () => ({ value: jiraServerId }) } as any;
        apiService.getAddonStatus.and
            .returnValue(Promise.resolve({ addonStatus: AddonStatus.Installed, addonVersion: '1.0' } as JiraAddonStatus));
        apiService.submitLoginInfo.and.returnValue(Promise.resolve({ isSuccess: true, message: 'success' }));

        await component.onSubmitConnectForm();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(apiService.getAddonStatus).toHaveBeenCalledWith(jiraServerId);
        expect(component.showLoginForm).toBeTrue();
        expect(component.showAddonStatusError).toBeFalse();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should call authService.authenticateMicrosoftAccount when add-on is connected and user was saved correctly', async () => {
        const jiraServerId = 'testJiraId';
        component.connectForm = { get: () => ({ value: jiraServerId }) } as any;
        apiService.getAddonStatus.and
            .returnValue(Promise.resolve({ addonStatus: AddonStatus.Connected, addonVersion: '1.0' } as JiraAddonStatus));
        apiService.saveJiraServerId.and.returnValue(Promise.resolve({ isSuccess: true } as any));
        authService.authenticateMicrosoftAccount.and.returnValue(Promise.resolve());

        await component.onSubmitConnectForm();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(apiService.getAddonStatus).toHaveBeenCalledWith(jiraServerId);
        expect(apiService.saveJiraServerId).toHaveBeenCalledWith(jiraServerId);
        expect(authService.authenticateMicrosoftAccount).toHaveBeenCalled();
        expect(component.showLoginForm).toBeFalse();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should call handleTab correctly for ApplicationType.JiraServerTab', async () => {
        const jiraServerId = 'testJiraId';
        component.connectForm = { get: () => ({ value: jiraServerId }) } as any;
        apiService.getAddonStatus.and
            .returnValue(Promise.resolve({ addonStatus: AddonStatus.Connected, addonVersion: '1.0' } as JiraAddonStatus));
        apiService.saveJiraServerId.and.returnValue(Promise.resolve({ isSuccess: true } as any));
        authService.authenticateMicrosoftAccount.and.returnValue(Promise.resolve());
        apiService.getMyselfData.and.returnValue(Promise.resolve({ id: 'testUserId' } as any));
        router.navigate.and.returnValue(Promise.resolve(true));
        component['application'] = ApplicationType.JiraServerTab;

        await component.onSubmitConnectForm();

        expect(authService.authenticateMicrosoftAccount).toHaveBeenCalledTimes(0);
        expect(apiService.getMyselfData).toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledWith([
            '/settings',
            { endpoint: undefined, application: 'testApplication', jiraUrl: 'testJiraId', displayName: undefined } as any]);
    });

    it('should handle onSubmitLoginForm correctly', async () => {
        component.jiraAuthUrl = 'https://example.com?oauth_token=testToken';
        component.loginForm = { get: () => ({ value: 'testCode' }) } as any;
        apiService.submitLoginInfo.and.returnValue(Promise.resolve({ isSuccess: true, message: 'success' }));
        apiService.saveJiraServerId.and.returnValue(Promise.resolve({ isSuccess: true, message: 'success' }));

        await component.onSubmitLoginForm();

        expect(loadingIndicatorService.show).toHaveBeenCalled();
        expect(apiService.submitLoginInfo).toHaveBeenCalledWith(component.jiraServerId as string, 'testToken', 'testCode');
        expect(apiService.saveJiraServerId).toHaveBeenCalledWith(component.jiraServerId as string);
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should handle errors in onSubmitConnectForm', async () => {
        component.connectForm = { get: () => ({ value: 'testJiraId' }) } as any;
        apiService.getAddonStatus.and.returnValue(Promise.reject('error'));

        await component.onSubmitConnectForm();

        expect(component.showAddonStatusError).toBeTrue();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should handle errors in onSubmitLoginForm', async () => {
        component.jiraAuthUrl = 'https://example.com?oauth_token=testToken';
        component.loginForm = { get: () => ({ value: 'testCode' }) } as any;
        apiService.submitLoginInfo.and.returnValue(Promise.reject('error'));

        await component.onSubmitLoginForm();

        expect(component.showAddonStatusError).toBeTrue();
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });
});
