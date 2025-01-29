import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { IssuesComponent } from './issues-table.component';
import { ApiService, AppInsightsService, ErrorService, UtilService } from '@core/services';
import { IssuesService } from '@core/services/entities/issues.service';
import { PermissionService } from '@core/services/entities/permission.service';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { DomSanitizer } from '@angular/platform-browser';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { HttpErrorResponse } from '@angular/common/http';
import { JiraIssuesSearch, NormalizedIssue } from '@core/models';
import { ApplicationType, StatusCode } from '@core/enums';
import { JiraPermissions } from '@core/models/Jira/jira-permission.model';
import * as microsoftTeams from '@microsoft/teams-js';

describe('IssuesComponent', () => {
    let component: IssuesComponent;
    let fixture: ComponentFixture<IssuesComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let issuesService: jasmine.SpyObj<IssuesService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let permissionService: jasmine.SpyObj<PermissionService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let utilService: jasmine.SpyObj<UtilService>;
    let loadingIndicatorService: jasmine.SpyObj<LoadingIndicatorService>;
    let route: ActivatedRoute;
    let router: Router;
    let dialog: MatDialog;
    const jiraIssueSearch: JiraIssuesSearch = {
        issues: [], prioritiesIdsInOrder: [], errorMessages: [], total: 0, pageSize: 50, expand: '', startAt: 0, maxResults: 50 };

    beforeEach(async () => {
        spyOn(microsoftTeams.app, 'notifySuccess').and.callFake(() => {});
        spyOn(microsoftTeams.app, 'getContext').and.returnValue(Promise.resolve([] as any));
        spyOn(microsoftTeams.dialog.url, 'submit').and.callFake(() => {});

        const apiServiceSpy = jasmine.createSpyObj('ApiService',
            ['getJiraUrlForPersonalScope',
                'getMyselfData',
                'validateConnection',
                'getIssues',
                'getFavouriteFilters',
                'getStatuses',
                'getFilter',
                'getAddonStatus'
            ]);
        const issuesServiceSpy = jasmine.createSpyObj('IssuesService',
            ['createJqlQuery', 'normalizeIssues', 'getFiltersFromQuery', 'adjustOrderByQueryStringWithIssueProperty']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService',
            ['showDefaultError', 'showAddonNotInstalledWindow', 'showMyFiltersEmptyError']);
        const permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['getMyPermissions']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent', 'sendUiEvent']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['isMobile', 'convertStringToNull']);
        const loadingIndicatorServiceSpy = jasmine.createSpyObj('LoadingIndicatorService', ['show', 'hide']);

        await TestBed.configureTestingModule({
            declarations: [IssuesComponent],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: IssuesService, useValue: issuesServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: PermissionService, useValue: permissionServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: LoadingIndicatorService, useValue: loadingIndicatorServiceSpy },
                { provide: ActivatedRoute, useValue: { snapshot: { params: {} } } },
                { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
                { provide: MatDialog, useValue: {} },
                { provide: DomSanitizer, useValue: { bypassSecurityTrustUrl: (url: string) => url } }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(IssuesComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        issuesService = TestBed.inject(IssuesService) as jasmine.SpyObj<IssuesService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        permissionService = TestBed.inject(PermissionService) as jasmine.SpyObj<PermissionService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        loadingIndicatorService = TestBed.inject(LoadingIndicatorService) as jasmine.SpyObj<LoadingIndicatorService>;
        route = TestBed.inject(ActivatedRoute);
        router = TestBed.inject(Router);
        dialog = TestBed.inject(MatDialog);

        issuesService.adjustOrderByQueryStringWithIssueProperty.and.returnValue('');
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        route.snapshot.params = { application: ApplicationType.JiraServerStaticTab };
        utilService.isMobile.and.returnValue(Promise.resolve(false));
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'http://example.com' }));
        apiService.getMyselfData.and.returnValue(Promise.resolve({ displayName: 'Test User', accountId: 'testUser' }));
        apiService.validateConnection.and.returnValue(Promise.resolve({ isSuccess: true }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: { CREATE_ISSUES: { havePermission: true } } as JiraPermissions }));

        await component.ngOnInit();

        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(apiService.getJiraUrlForPersonalScope).toHaveBeenCalled();
        expect(apiService.getMyselfData).toHaveBeenCalled();
        expect(apiService.validateConnection).toHaveBeenCalled();
        expect(permissionService.getMyPermissions).toHaveBeenCalled();
    });

    it('should handle form submission successfully', async () => {
        component.jiraUrl = 'http://example.com';
        component['jqlQuery'] = 'test jql query';
        issuesService.createJqlQuery.and.returnValue('transformed jql query');
        apiService.getIssues.and.returnValue(Promise.resolve(jiraIssueSearch));

        await component['loadData']();

        expect(issuesService.createJqlQuery).toHaveBeenCalled();
        expect(apiService.getIssues).toHaveBeenCalled();
    });

    it('should handle form submission failure', async () => {
        component.jiraUrl = 'http://example.com';
        route.snapshot.params = { application: ApplicationType.JiraServerStaticTab, jiraUrl: 'http://example.com' };
        component['jqlQuery'] = 'test jql query';
        issuesService.createJqlQuery.and.returnValue('transformed jql query');
        apiService.getIssues.and.returnValue(Promise.reject(new HttpErrorResponse({ status: StatusCode.NotFound })));

        await component.ngOnInit();
        await component['loadData']();

        expect(issuesService.createJqlQuery).toHaveBeenCalled();
        expect(apiService.getIssues).toHaveBeenCalled();
        expect(errorService.showDefaultError).toHaveBeenCalled();
    });

    it('should handle user not authenticated', async () => {
        component.jiraUrl = 'http://example.com';
        apiService.getAddonStatus.and.returnValue(Promise.resolve({ addonStatus: 1, addonVersion: '1.0.0' }));

        await component['onUserNotAuthenticated'](StatusCode.Unauthorized);

        expect(router.navigate).toHaveBeenCalledWith(['/login',
            { ...route.snapshot.params, status: StatusCode.Unauthorized, jiraUrl: 'http://example.com' }]);
    });

    it('should handle user authenticated', async () => {
        component.jiraUrl = 'http://example.com';
        component['application'] = ApplicationType.JiraServerStaticTab;
        issuesService.createJqlQuery.and.returnValue('transformed jql query');
        apiService.getIssues.and.returnValue(Promise.resolve(jiraIssueSearch));

        await component['loadData']();

        expect(issuesService.createJqlQuery).toHaveBeenCalled();
        expect(apiService.getIssues).toHaveBeenCalled();
    });

    it('should handle user not authenticated with addon not installed', async () => {
        component.jiraUrl = 'http://example.com';
        apiService.getAddonStatus.and.returnValue(Promise.resolve({ addonStatus: 0, addonVersion: '1.0.0' }));

        await component['onUserNotAuthenticated'](StatusCode.Unauthorized);

        expect(errorService.showAddonNotInstalledWindow).toHaveBeenCalled();
    });

    it('should handle user authenticated with static tab elements', async () => {
        component.jiraUrl = 'http://example.com';
        component['application'] = ApplicationType.JiraServerStaticTab;
        issuesService.createJqlQuery.and.returnValue('transformed jql query');
        apiService.getIssues.and.returnValue(Promise.resolve(jiraIssueSearch));

        await component['loadData']();

        expect(issuesService.createJqlQuery).toHaveBeenCalled();
        expect(apiService.getIssues).toHaveBeenCalled();
    });

    it('should handle user authenticated without static tab elements', async () => {
        component.jiraUrl = 'http://example.com';
        component['application'] = ApplicationType.JiraServerTab;
        issuesService.createJqlQuery.and.returnValue('transformed jql query');
        apiService.getIssues.and.returnValue(Promise.resolve(jiraIssueSearch));

        issuesService.normalizeIssues.and.returnValue([]);

        await component['loadData']();

        expect(issuesService.createJqlQuery).toHaveBeenCalled();
        expect(apiService.getIssues).toHaveBeenCalled();
    });

    it ('isJiraServerApplication should return true when application is JiraServerStaticTab', () => {
        component['application'] = ApplicationType.JiraServerStaticTab;

        expect(component.isJiraServerApplication).toBeTrue();
    });
});
