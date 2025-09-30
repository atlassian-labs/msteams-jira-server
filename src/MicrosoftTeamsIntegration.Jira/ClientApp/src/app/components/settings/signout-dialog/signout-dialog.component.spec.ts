import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SignoutDialogComponent } from './signout-dialog.component';
import { ApiService, AuthService, UtilService, AppInsightsService } from '@core/services';
import { ActivatedRoute, Router } from '@angular/router';
import { TeamsService } from '@core/services/teams.service';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('SignoutDialogComponent', () => {

    let teamsService: jasmine.SpyObj<TeamsService>;
    let component: SignoutDialogComponent;
    let fixture: ComponentFixture<SignoutDialogComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let authService: jasmine.SpyObj<AuthService>;
    let utilService: jasmine.SpyObj<UtilService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let router: jasmine.SpyObj<Router>;
    let route: ActivatedRoute;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['logOut']);
        const authServiceSpy = jasmine.createSpyObj('AuthService', ['']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

        await TestBed.configureTestingModule({
            declarations: [SignoutDialogComponent],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: AuthService, useValue: authServiceSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: Router, useValue: routerSpy },
                { provide: ActivatedRoute, useValue: { snapshot: { params: { jiraUrl: 'test-jira-url' } } } },
                {
                    provide: TeamsService,
                    useValue: {
                        initialize: jasmine.createSpy('initialize').and.returnValue(Promise.resolve()),
                        notifySuccess: jasmine.createSpy('notifySuccess')
                    }
                }],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(SignoutDialogComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
        route = TestBed.inject(ActivatedRoute);
        teamsService = TestBed.inject(TeamsService) as jasmine.SpyObj<TeamsService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize and log navigation on init', async () => {
        spyOn(component, 'ngOnInit').and.callThrough();
        await component.ngOnInit();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(component.ngOnInit).toHaveBeenCalled();
        expect(teamsService.notifySuccess).toHaveBeenCalled();
    });

    it('should parse params correctly', () => {
        component.ngOnInit();
        expect(component['jiraUrl']).toBe('test-jira-url');
    });

    it('should handle sign out', async () => {
        apiService.logOut.and.returnValue(Promise.resolve() as any);
        component.ngOnInit();
        await component.onSignOut();
        expect(component.isSigningOut).toBe(false);
        expect(apiService.logOut).toHaveBeenCalledWith('test-jira-url');
        expect(router.navigate).toHaveBeenCalledWith(['/config', { jiraUrl: 'test-jira-url' }]);
    });

    it('should handle sign out error', async () => {
        const error = new Error('error');
        apiService.logOut.and.throwError(error);
        component.ngOnInit();
        await component.onSignOut();
        expect(component.isSigningOut).toBe(false);
        expect(appInsightsService.trackException).toHaveBeenCalledWith(
            new Error('Error while signout from Jira'),
            'SignoutComponent::signOut',
            { originalErrorMessage: error.message }
        );
    });

    it('should navigate back', async () => {
        component.ngOnInit();
        await component.navigateBack();
        expect(router.navigate).toHaveBeenCalledWith(['/settings', { jiraUrl: 'test-jira-url' }]);
    });
});
