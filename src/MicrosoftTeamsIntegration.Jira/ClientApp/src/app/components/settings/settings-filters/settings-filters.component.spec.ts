import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FilterSetting, FilterType, SettingsFiltersComponent } from './settings-filters.component';
import { ApiService, ErrorService, AppInsightsService, UtilService } from '@core/services';
import { IssuesService } from '@core/services/entities/issues.service';
import { SettingsService } from '@core/services/entities/settings.service';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from '@shared/services/notificationService';
import { ActivatedRoute } from '@angular/router';
import * as microsoftTeams from '@microsoft/teams-js';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SelectOption } from '@shared/models/select-option.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { SelectChange } from '@shared/models/select-change.model';

describe('SettingsFiltersComponent', () => {
    let component: SettingsFiltersComponent;
    let fixture: ComponentFixture<SettingsFiltersComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let utilService: jasmine.SpyObj<UtilService>;
    let issuesService: jasmine.SpyObj<IssuesService>;
    let settingsService: jasmine.SpyObj<SettingsService>;
    let loadingIndicatorService: jasmine.SpyObj<LoadingIndicatorService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    let snackBar: jasmine.SpyObj<MatSnackBar>;
    let notificationService: jasmine.SpyObj<NotificationService>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService',
            ['getStatusesByProject',
                'getProject',
                'getPriorities',
                'getProjects',
                'getFavouriteFilters',
                'getAddonStatus',
                'findProjects']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['showDefaultError']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['encode', 'isAddonUpdated', 'getUpgradeAddonMessage']);
        const issuesServiceSpy = jasmine.createSpyObj('IssuesService', ['createJqlQuery']);
        const settingsServiceSpy = jasmine.createSpyObj('SettingsService', ['buildOptionsFor']);
        const loadingIndicatorServiceSpy = jasmine.createSpyObj('LoadingIndicatorService', ['show', 'hide']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapProjectToDropdownOption']);
        const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);
        const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['notifyError']);

        await TestBed.configureTestingModule({
            declarations: [SettingsFiltersComponent, DropDownComponent],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: IssuesService, useValue: issuesServiceSpy },
                { provide: SettingsService, useValue: settingsServiceSpy },
                { provide: LoadingIndicatorService, useValue: loadingIndicatorServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy },
                { provide: MatSnackBar, useValue: snackBarSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: ActivatedRoute, useValue: { snapshot: { params: { jiraUrl: 'test-jira-url' } } } }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(SettingsFiltersComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        issuesService = TestBed.inject(IssuesService) as jasmine.SpyObj<IssuesService>;
        settingsService = TestBed.inject(SettingsService) as jasmine.SpyObj<SettingsService>;
        loadingIndicatorService = TestBed.inject(LoadingIndicatorService) as jasmine.SpyObj<LoadingIndicatorService>;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
        snackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
        notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;

        // Mock the projectsDropdown ViewChild
        component.projectsDropdown = {
            filteredOptions: []
        } as unknown as DropDownComponent<string>;

        if (!jasmine.isSpy(microsoftTeams.pages.config.setValidityState)) {
            spyOn(microsoftTeams.pages.config, 'setValidityState').and.callFake(() => {});
        }
        if (!jasmine.isSpy(microsoftTeams.pages.config.registerOnSaveHandler)) {
            spyOn(microsoftTeams.pages.config, 'registerOnSaveHandler').and.callFake(() => {});
        }
        if (!jasmine.isSpy(microsoftTeams.pages.config.setConfig)) {
            spyOn(microsoftTeams.pages.config, 'setConfig').and
                .callFake((instanceConfig: microsoftTeams.pages.InstanceConfig) => Promise.resolve([] as any));
        }
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize and load data on init', async () => {
        spyOn(component as any, 'loadData').and.callThrough();
        await component.ngOnInit();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect((component as any).loadData).toHaveBeenCalled();
    });

    it('should initialize and load data when filters are available', async () => {
        const favouriteFilters = [{ id: '1', name: 'My Filter' }];
        apiService.getFavouriteFilters.and.returnValue(Promise.resolve(favouriteFilters as any));
        apiService.getAddonStatus.and.returnValue({ addonVersion: '2025.01.01'} as any);
        await component.ngOnInit();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(apiService.getFavouriteFilters).toHaveBeenCalled();
        expect(apiService.getProjects).toHaveBeenCalledTimes(0);
    });

    it('should initialize and load data when filters are not available', async () => {
        const project = { id: '1', key: 'TEST', name: 'Test Project',
            issueTypes: [{ id: '1', name: 'Bug' }, { id: '2', name: 'Task' }] } as any;
        apiService.getFavouriteFilters.and.returnValue(Promise.resolve([] as any));
        apiService.getProjects.and.returnValue(Promise.resolve([project]));
        apiService.getAddonStatus.and.returnValue({ addonVersion: '2025.01.01'} as any);
        dropdownUtilService.mapProjectToDropdownOption.and.returnValue({ id: '1', label: 'Test Project', value: '1' });
        await component.ngOnInit();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(apiService.getFavouriteFilters).toHaveBeenCalled();
        expect(apiService.getProjects).toHaveBeenCalled();
        expect(utilService.isAddonUpdated).toHaveBeenCalled();
    });

    it('should handle project selection', async () => {
        const project = { id: '1', key: 'TEST', name: 'Test Project',
            issueTypes: [{ id: '1', name: 'Bug' }, { id: '2', name: 'Task' }] } as any;
        const statuses = [{ id: '1', name: 'Open' }, { id: '2', name: 'In Progress' }];
        component.projects = [project];
        apiService.getStatusesByProject.and.returnValue(Promise.resolve(statuses as any));
        apiService.getProjects.and.returnValue(Promise.resolve([project]));
        settingsService.buildOptionsFor.and
            .callFake((response: any[]) => response.map(item => ({ id: item.id, label: item.name, value: item.id })));
        await component.onProjectSelected(project.id);
        expect(component.selectedProject).toEqual(project);
        expect(component.statusesOptions).toBeDefined();
        expect(component.issueTypesOptions).toBeDefined();
    });

    it('should handle project selection when selected project do not have issue types', async () => {
        const project = { id: '1', key: 'TEST', name: 'Test Project' } as any;
        const projectWithIssueTypes = { id: '1', key: 'TEST', name: 'Test Project',
            issueTypes: [{ id: '1', name: 'Bug' }, { id: '2', name: 'Task' }] } as any;
        const statuses = [{ id: '1', name: 'Open' }, { id: '2', name: 'In Progress' }];
        component.projects = [project];
        apiService.getStatusesByProject.and.returnValue(Promise.resolve(statuses as any));
        apiService.getProjects.and.returnValue(Promise.resolve([project]));
        apiService.getProject.and.returnValue(Promise.resolve(projectWithIssueTypes));
        settingsService.buildOptionsFor.and
            .callFake((response: any[]) => response.map(item => ({ id: item.id, label: item.name, value: item.id })));
        await component.onProjectSelected(project.id);
        expect(apiService.getProject).toHaveBeenCalled();
        expect(component.selectedProject).toEqual(projectWithIssueTypes);
        expect(component.statusesOptions).toBeDefined();
        expect(component.issueTypesOptions).toBeDefined();
    });

    it('should handle filter option selection', () => {
        const event = { options: [{ value: '1' }], isAll: false } as any;
        component.onFilterOptionSelected(event, 'priority');
        expect((component as any).filters.get('priority')).toEqual(['1']);
    });

    it('should handle radio button click', async () => {
        spyOn(component as any, 'loadProjectsData').and.callThrough();
        await component.onRadioButtonClicked(FilterType.Custom);
        expect((component as any).loadProjectsData).toHaveBeenCalled();
        expect(component.filter).toBe(FilterType.Custom);
    });

    it('should handle project click and show snackbar if addon is not updated', () => {
        component.isAddonUpdated = false;
        spyOn(component as any, 'openSnackBar').and.callThrough();
        component.handleProjectClick();
        expect((component as any).openSnackBar).toHaveBeenCalled();
    });

    it('should open filters page', () => {
        spyOn(window, 'open');
        component.openFiltersPage();
        expect(window.open)
            .toHaveBeenCalledWith('https://confluence.atlassian.com/jiracorecloud/saving-your-search-as-a-filter-765593721.html');
    });

    it('should search projects', async () => {
        const projects = [{ id: '1', key: 'TEST', name: 'Test Project' }] as any;
        apiService.findProjects.and.returnValue(Promise.resolve(projects));
        dropdownUtilService.mapProjectToDropdownOption.and.returnValue({ id: '1', label: 'Test Project', value: '1' });
        await component.onSearchChanged('test');
        expect(component.projects).toEqual(projects);
        expect(component.projectsDropdown.filteredOptions).toEqual([{ id: '1', label: 'Test Project', value: '1' }]);
    });

    it('should handle error on search projects', async () => {
        const projects = [{ id: '1', key: 'TEST', name: 'Test Project' }] as any;
        apiService.findProjects.and.throwError(new Error('error'));
        await component.onSearchChanged('test');
        expect(appInsightsService.trackException).toHaveBeenCalled();
    });

    it('should handle filter selection', () => {
        const option = { id: '1', label: 'Priority 1', value: '1' } as DropDownOption<string>;
        const field: FilterSetting = 'priority';
        const selectChange = {
            isAll: false,
            options: [
                {
                    id: option.id,
                    value: option.id,
                    label: option.label
                } as SelectOption
            ]
        } as SelectChange;

        spyOn(component, 'onFilterOptionSelected').and.callThrough();
        component.onFilterSelected(option, field);
        expect(component.onFilterOptionSelected).toHaveBeenCalledWith(selectChange, field);
    });

    it('should handle filter selection with empty options', () => {
        const option = { id: '', label: '', value: '' } as DropDownOption<string>;
        const field: FilterSetting = 'priority';
        const selectChange = {
            isAll: false,
            options: [
                {
                    id: option.id,
                    value: option.id,
                    label: option.label
                } as SelectOption
            ]
        } as SelectChange;

        spyOn(component, 'onFilterOptionSelected').and.callThrough();
        component.onFilterSelected(option, field);
        expect(component.onFilterOptionSelected).toHaveBeenCalledWith(selectChange, field);
    });
});
