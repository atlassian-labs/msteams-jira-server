import { ApplicationType } from './../../../core/enums/application-type.enum';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import {
    ApiService,
    ErrorService,
    AppInsightsService,
    UtilService
} from '@core/services';

import { IssuesService } from '@core/services/entities/issues.service';
import { SettingsService } from '@core/services/entities/settings.service';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { DropDownOption } from '@shared/models/dropdown-option.model';

import {
    Project,
    Filter,
    IssueType,
    Priority,
    IssueStatus
} from '@core/models';

import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';

import { SelectOption } from '@shared/models/select-option.model';
import { SelectChange } from '@shared/models/select-change.model';

import * as microsoftTeams from '@microsoft/teams-js';
import { StatusCode } from '@core/enums';
import { logger } from '@core/services/logger.service';
import { DropdownUtilService } from '../../../shared/services/dropdown.util.service';

enum FilterType {
    Saved = 'from-saved',
    Custom = 'create-new',
}

export type JiraFilterName = 'priority' | 'project' | 'type' | 'status';
export type FilterSetting = JiraFilterName | 'filter';

@Component({
    selector: 'app-settings-filters',
    templateUrl: './settings-filters.component.html',
    styleUrls: ['./settings-filters.component.scss']
})
export class SettingsFiltersComponent implements OnInit {
    public statusesOptions: SelectOption[] = [];
    public issueTypesOptions: SelectOption[] = [];
    public projectsOptions: SelectOption[] = [];
    public prioritiesOptions: SelectOption[] = [];
    public savedFiltersOptions: SelectOption[] = [];
    public savedFilteredFiltersOptions: SelectOption[] = [];

    public availableProjectsOptions: DropDownOption<string>[];
    public projectFilteredOptions: DropDownOption<string>[];
    
    public FilterType = FilterType;
    public filter: FilterType = FilterType.Saved;

    public selectedProject: Project = undefined;

    public savedFiltersIsDisabled = false;
    public projectsDataLoaded = false;
    public filtersLoading = false;
    public isFetchingProjects = false;

    private jiraUrl: string;
    public projects: Project[];

    private settings = new Map<string, string>();
    private filters = new Map<FilterSetting, string[]>();
    private cachedSettings = new Map<string, string>();
    
    private readonly SERVER_TAB_NAME = 'Jira Server';
    private readonly ISSUES_PAGE_URL = `https://${window.location.host}/#/issues`;
    private readonly FILTERS_PAGE = 'https://confluence.atlassian.com/jiracorecloud/saving-your-search-as-a-filter-765593721.html';
    private readonly CACHED_FILTER_KEY = 'cachedFilter';

    @ViewChild('filtersDropdown', { static: false }) filtersDropdown: DropDownComponent<string>;
    @ViewChild('projectsDropdown', { static: false }) projectsDropdown: DropDownComponent<string>;

    constructor(
        private route: ActivatedRoute,
        private apiService: ApiService,
        private errorService: ErrorService,
        private settingsService: SettingsService,
        private issuesService: IssuesService,
        private utilService: UtilService,
        private appInsightsService: AppInsightsService,
        private loadingIndicatorService: LoadingIndicatorService,
        private dropdownUtilService: DropdownUtilService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.appInsightsService.logNavigation('SettingsProjectComponent', this.route);
        this.jiraUrl = this.route.snapshot.params.jiraUrl;
        this.settings.set('jiraUrl', this.jiraUrl);

        await this.loadData();
    }

    public async onSearchChanged(filterName: string): Promise<void> {
        filterName = filterName.trim().toLowerCase();
        this.isFetchingProjects = true;
        try {
            this.projects = await this.findProjects(this.jiraUrl, filterName);
            const filteredProjects = this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
            this.projectsDropdown.filteredOptions = filteredProjects;
        }
        catch(error)
        {
            this.appInsightsService.trackException(
                new Error('Error while searching projects'),
                'Settings Filter Component',
                { originalErrorMessage: error.message }
            );
        }
        finally {
            this.isFetchingProjects = false;
        }
    }

    public openFiltersPage(): void {
        window.open(this.FILTERS_PAGE);
    }

    public async onProjectSelected(optionOrValue: DropDownOption<string> | string): Promise<void> {
        const projectId = typeof optionOrValue === 'string' ? optionOrValue : optionOrValue.value;
        this.loadingIndicatorService.show();

        const project = this.projects.find(proj => proj.id === projectId);
        this.selectedProject = project;

        this.filters.clear();
        this.settings.delete('jqlQuery');

        this.settings.set('projectKey', this.utilService.encode(encodeURIComponent(project.key)));
        this.settings.set('projectName', this.utilService.encode(encodeURIComponent(project.name)));

        const statuses = await this.apiService.getStatusesByProject(this.jiraUrl, project.key);
        this.statusesOptions = this.settingsService.buildOptionsFor<IssueStatus>(statuses);

        if (!this.selectedProject.issueTypes) {
            this.selectedProject = await this.apiService.getProject(this.jiraUrl, project.key);
        }

        this.issueTypesOptions = this.settingsService.buildOptionsFor<IssueType>(this.selectedProject.issueTypes);

        this.loadingIndicatorService.hide();
        this.getQueryAndRegisterTeamsHandler();
    }


    public onFilterOptionSelected(event: SelectChange, field: FilterSetting): void {
        const { options = [], isAll } = event;

        const values = options.map((x: SelectOption) => x.value);

        if (isAll) {
            this.filters.delete(field);
            this.settings.delete('jqlQuery');
        } else {
            this.filters.set(field, values);
        }

        this.getQueryAndRegisterTeamsHandler();
    }

    public onFilterSelected(option: DropDownOption<string>, field: FilterSetting): void {

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

        this.onFilterOptionSelected(selectChange, field);
    }

    public async onRadioButtonClicked(filter: FilterType): Promise<void> {
        if (this.filter === filter) {
            return;
        }

        if (filter === FilterType.Custom && !this.projectsDataLoaded) {
            await this.loadProjectsData();
        }

        microsoftTeams.settings.setValidityState(false);

        this.selectedProject = undefined;
        this.filter = filter;
    }

    private async getFilterOptions(): Promise<DropDownOption<string>[]> {
        this.filtersLoading = true;

        const filters = await this.apiService.getFavouriteFilters(this.jiraUrl);

        this.filtersLoading = false;

        return filters.map(
            (filter) => ({
                id: filter.id,
                label: filter.name,
                value: filter.id
            })
        );
    }

    private async loadData(): Promise<void> {
        try {
            this.loadingIndicatorService.show();

            this.savedFiltersOptions = await this.getFilterOptions();
            this.savedFilteredFiltersOptions = this.savedFiltersOptions;

            this.savedFiltersIsDisabled = !Boolean(this.savedFiltersOptions.length);
            this.filter = this.savedFiltersIsDisabled ? FilterType.Custom : FilterType.Saved;

            if (this.savedFiltersIsDisabled) {
                await this.loadProjectsData();
            }
        } catch (error) {
            this.errorService.showDefaultError(error);
        } finally {
            this.loadingIndicatorService.hide();
        }
    }

    private async loadProjectsData(): Promise<void> {
        try {
            this.loadingIndicatorService.show();

            const [priorities] = await Promise.all([
                this.apiService.getPriorities(this.jiraUrl)
            ]);

            this.projects = await this.apiService.getProjects(this.jiraUrl, true);
            this.availableProjectsOptions = this.projects.map(this.dropdownUtilService.mapProjectToDropdownOption);
            this.projectFilteredOptions = this.availableProjectsOptions;
            
            this.projectsOptions = this.settingsService.buildOptionsFor<Project>(this.projects);

            this.prioritiesOptions = this.settingsService.buildOptionsFor<Priority>(priorities);

        } catch (error) {
            this.errorService.showDefaultError(error);
        } finally {
            this.loadingIndicatorService.hide();
            this.projectsDataLoaded = true;
        }
    }

    private getQueryAndRegisterTeamsHandler(): void {
        let jql = '';
        
        const encodedFilterJql = this.utilService.encode(encodeURIComponent(this.getCachedOrBuildFiltersJql()));
        this.settings.set('jqlQuery', encodedFilterJql || '');

        jql = this.issuesService.createJqlQuery({ jql: encodedFilterJql, projectKey: this.settings.get('projectKey') });     

        this.registerHandler(this.createContentUrl(), jql);
    }

    private getCachedOrBuildFiltersJql(): string {
        const cachedFilter = this.filters.get('filter');
        if (cachedFilter) {
            const filter = this.savedFiltersOptions.find((x: SelectOption) => x.value === cachedFilter[0]) ||
                this.savedFilteredFiltersOptions.find((x: SelectOption) => x.value === cachedFilter[0]);
            if (filter) {
                return `filter=${filter.id}`;
            }
        }

        return this.buildFiltersJql();
    }

    private buildFiltersJql(): string {
        const ordering = {
            type: 0,
            priorioty: 1,
            status: 2
        };

        const jqlQuery = Array
            .from(this.filters)
            .sort((a: [FilterSetting, string[]], b: [FilterSetting, string[]]) => {
                const [keyA] = a;
                const [keyB] = b;
                return (ordering[keyA] - ordering[keyB]);
            })
            .filter((row: [FilterSetting, string[]]) => {
                const [key, value] = row;

                return key !== 'filter' && typeof value !== 'undefined' && value != null && value.length > 0;
            })
            .map((row: [FilterSetting, string[]]) => {
                const [key, value] = row;

                if (value.length) {
                    return `${key} in ("${value.join('","')}")`;
                }

                return '';
            })
            .join(' AND ');

        return jqlQuery;
    }

    private createContentUrl(): string {
        let contentUrl = this.ISSUES_PAGE_URL;

        this.settings.forEach((value: string, key: string) => {
            contentUrl += `;${key}=${value}`;
        });

        const application = ApplicationType.JiraServerTab;
        // set application in order to detect is this app Jira Server
        contentUrl += `;application=${application}`;

        return contentUrl;
    }

    private getProjectSettingsFromCache(): void {
        this.cachedSettings.forEach((value: string, key: string) => this.settings.set(key, value));
    }

    private getSavedFilterFromCache(): void {
        if (localStorage.getItem(this.CACHED_FILTER_KEY)) {
            this.filters.set('filter', [localStorage.getItem(this.CACHED_FILTER_KEY)]);
            localStorage.removeItem(this.CACHED_FILTER_KEY);
        }
    }

    private cacheProjectSettings(): void {
        const projectSettingsKeys = ['projectKey', 'projectName'];
        this.settings.forEach((value: string, key: string) => {
            if (projectSettingsKeys.includes(key)) {
                this.cachedSettings.set(key, value);
                this.settings.delete(key);
            }
        });
    }

    private cacheFilterSettings(): void {
        if (this.filters.has('filter')) {
            localStorage.setItem(this.CACHED_FILTER_KEY, this.filters.get('filter')[0]);
            this.filters.delete('filter');
        }
    }

    private async findProjects(jiraUrl: string, filterName?: string): Promise<Project[]> {
        const result = await this.apiService.findProjects(jiraUrl, filterName, true);
        return result;
    }

    private registerHandler(contentUrl: string, jql: string): void {
        // base user's repository url e.g. `userrepository.atlassian.net`
        let websiteUrl = `${decodeURIComponent(this.jiraUrl)}`;

        websiteUrl += `/issues/?jql=${decodeURIComponent(jql)}`;

        const settings = {
            entityId: contentUrl,
            contentUrl,
            suggestedDisplayName: this.SERVER_TAB_NAME,
            websiteUrl: null
        };

        microsoftTeams.settings.registerOnSaveHandler((saveEvent: microsoftTeams.settings.SaveEvent) => {
            microsoftTeams.settings.setSettings(settings);
            saveEvent.notifySuccess();
        });

        microsoftTeams.settings.setValidityState(true);
    }
}
