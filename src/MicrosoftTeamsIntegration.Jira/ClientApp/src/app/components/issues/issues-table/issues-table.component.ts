// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { HttpErrorResponse } from '@angular/common/http';
import {
    Component,
    ElementRef,
    OnInit,
    ViewChild
} from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { SignoutMaterialDialogComponent } from '@app/components/issues/signout-material-dialog/signout-material-dialog.component';
import { AddonStatus, ApplicationType, PageName, StatusCode } from '@core/enums';
import { JiraSortDirection } from '@core/enums/sort-direction.enum';
import { NormalizedIssue } from '@core/models';
import { JiraPermissions } from '@core/models/Jira/jira-permission.model';
import { JqlOptions } from '@core/models/Jira/jql-settings.model';
import {
    ApiService,
    AppInsightsService,
    ErrorService,
    UtilService
} from '@core/services';
import { IssuesService } from '@core/services/entities/issues.service';
import { PermissionService } from '@core/services/entities/permission.service';
import { logger } from '@core/services/logger.service';
import * as microsoftTeams from '@microsoft/teams-js';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { SelectOption } from '@shared/models/select-option.model';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';

@Component({
    selector: 'app-issues',
    styleUrls: [
        './issues-table.component.scss',
        './issues-table.component.themes.scss'
    ],
    templateUrl: './issues-table.component.html',
    providers: [IssuesService]
})
export class IssuesComponent implements OnInit {
    readonly defaultActiveColumn = 'updated';
    readonly defaultSortDirection = JiraSortDirection.Desc;

    public loading: boolean;
    public isMobile = false;

    public loadingTableData: boolean;

    public expandedElementIndex: any;
    public displayedColumns: string[] = [];

    public dataSource: MatTableDataSource<NormalizedIssue>;

    public jiraUrl: string;
    public issues: NormalizedIssue[] = [];

    public displayName: string;
    public accountId: string;
    public projectName: string;
    public showStaticTabElements: boolean;

    public sortDirection: JiraSortDirection = this.defaultSortDirection;
    public activeColumn = this.defaultActiveColumn;
    public defaultPriorityValue = 9999999;

    public selectedJiraFilter: SelectOption;
    public jiraFilters: SelectOption[];

    public label: string;
    public error: string;

    public transformedJql: string;
    public decodedTransformedJqlQuery: string;
    public filters: string;
    public currentFilter: string;

    public jiraLink: string;
    public jiraLinkWithJql: string;

    public permissions: JiraPermissions;

    private page: string;
    private projectKey: string;
    private application: string;
    private jqlQuery: string;
    // a value to be added to the end of jql query
    private initialJqlOrderBySuffix = ` order by ${this.defaultActiveColumn} ${this.defaultSortDirection}`;
    private jqlOrderBySuffix = ` order by ${this.activeColumn} ${this.sortDirection}`;
    private sortedColumnsState = new Map<string, JiraSortDirection>();
    private filtersLoading = false;
    private searchableFilters = false;

    @ViewChild('filtersDropdown', { static: false }) filtersDropdown: DropDownComponent<string>;

    public get isJiraServerApplication(): boolean {
        return this.application === ApplicationType.JiraServerStaticTab ||
            this.application === ApplicationType.JiraServerCompose ||
            this.application === ApplicationType.JiraServerTab;
    }

    private readonly avaialableToDisplayColumns = [
        'timeToResolution',
        'timeToFirstResponse',
        'timeToApproveNormalChange',
        'timeToCloseAfterResolution',
        'issuetype',
        'issuekey',
        'summary',
        'assignee',
        'reporter',
        'priority',
        'status',
        'created',
        'updated',
        'components',
        'duedate',
        'impact',
        'requestType',
        'satisfaction',
        'labels'
    ];

    /* paginator */
    // set default value for totat page count equals to 200
    public pageCount = 200;
    public pageSize = 50;
    public pageIndex = 0;
    public get showPaginator() {
        return this.pageCount > this.pageSize;
    }
    private get startAt() {
        // Jira has zero-based startAt parameter
        return this.pageIndex === 0 ? 0 : this.pageIndex * this.pageSize;
    }

    constructor(
        public dialog: MatDialog,
        public domSanitizer: DomSanitizer,
        private router: Router,
        private route: ActivatedRoute,
        private apiService: ApiService,
        private utilService: UtilService,
        private errorService: ErrorService,
        private issuesService: IssuesService,
        private appInsightService: AppInsightsService,
        private loadingIndicatorService: LoadingIndicatorService,
        private permissionService: PermissionService,
    ) { }

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        this.isMobile = await this.utilService.isMobile();

        this.appInsightService.logNavigation('IssuesComponent', this.route);

        this.parseParams();

        this.label = this.page === 'MyFilters' ? 'Saved filter' : '';

        this.filters = await this.getConfigurationFiltersForHeader(decodeURIComponent(this.jqlQuery), this.jiraUrl);

        this.showStaticTabElements = this.application === ApplicationType.JiraServerStaticTab;

        await this.startAuthFlow();

        if (this.jiraUrl) {
            const { permissions } = await this.permissionService.getMyPermissions(this.jiraUrl, 'CREATE_ISSUES', null, this.projectKey);
            this.permissions = permissions;
            this.jiraLink = this.createJiraLink();
        }
        this.loadingOff();

        const issueTableComponent = this;

        // open edit issue dialog if context contains sub entity ID and user is authorized
        microsoftTeams.getContext(function (context) {
            if (context.subEntityId && issueTableComponent.jiraUrl) {
                issueTableComponent.openEditDialog(context.subEntityId);
            }
        });
    }

    public async setJiraFilter(filter: any) {
        // reset paginator when filter was selected
        this.pageIndex = 0;
        this.jqlOrderBySuffix = this.initialJqlOrderBySuffix;
        this.selectedJiraFilter = filter;
        await this.startAuthFlow();
    }

    public openEditDialog(issueId: string): void {
        const application = this.application || ApplicationType.JiraServerStaticTab;
        const url = `${localStorage.getItem('baseUrl')}/#/issues/edit;jiraUrl=${encodeURIComponent(this.jiraUrl)};` +
            `application=${application};issueId=${issueId};source=issuesTab`;

        const taskInfo = {
            title: 'Edit the issue',
            url,
            fallbackUrl: url,
            width: 710,
            height: 522
        };

        microsoftTeams.tasks.startTask(taskInfo, async (err, result: any) => {
            if (err) {
                if (err !== 'User cancelled/closed the task module.') {
                    this.appInsightService.trackException(new Error(err), 'Issue-table.component::openEditDialog');
                }
            } else {
                if (result instanceof Error) {
                    this.appInsightService.trackException(result, 'Issue-table.component::openEditDialog');
                } else {
                    await this.loadData();
                }
            }
        });
    }

    public openIssueCreateDialog(): void {
        const application = this.application || ApplicationType.JiraServerStaticTab;
        const url = `${localStorage.getItem('baseUrl')}/#/issues/create;jiraUrl=${encodeURIComponent(this.jiraUrl)};` +
                        `application=${application};source=issuesTab`;

        const taskInfo = {
            title: 'Create an issue',
            url,
            fallbackUrl: url,
            width: 600,
            height: 522
        };

        microsoftTeams.tasks.startTask(taskInfo, async (err, result: any) => {
            if (err) {
                if (err !== 'User cancelled/closed the task module.') {
                    this.appInsightService.trackException(new Error(err), 'Issue-table.component::openIssueCreateDialog');
                }
            } else {
                if (result instanceof Error) {
                    this.appInsightService.trackException(result, 'Issue-table.component::openIssueCreateDialog');
                } else {
                    await this.loadData();
                }
            }
        });
    }

    public getChevronClass(columnName: string): string {
        if (this.activeColumn !== columnName || !this.sortDirection) {
            return '';
        }

        return this.sortDirection === JiraSortDirection.Asc ? 'chevron-up' : 'chevron-down';
    }

    public makeArray(length: number | null): Array<any> {
        if (!length) {
            return [];
        }

        return Array(length).fill(1);
    }

    public getScrollWidth(elRef: ElementRef): number {
        if (!elRef) {
            throw new Error('Element is not defined');
        }

        const elem = (elRef.nativeElement || elRef['_elementRef'].nativeElement) as HTMLElement;
        return elem.offsetWidth - elem.clientWidth;
    }

    public async changePage(event: any) {
        this.pageIndex = event.pageIndex;
        await this.loadIssuesWithSpinner();
    }

    private parseParams(): void {
        const {
            jiraUrl,
            jqlQuery = '',
            projectKey,
            projectName = '',
            application
        } = this.route.snapshot.params;

        this.jiraUrl = this.utilService.convertStringToNull(jiraUrl);
        this.jqlQuery = jqlQuery;
        this.projectKey = projectKey;
        this.projectName = this.utilService.convertStringToNull(decodeURIComponent(projectName));
        this.page = this.route.snapshot.params.page;
        this.application = application;
    }

    private async startAuthFlow(): Promise<void> {
        try {
            if (this.jiraUrl) {
                const { displayName, accountId } = await this.apiService.getMyselfData(this.jiraUrl);
                this.displayName = displayName;
                this.accountId = accountId;
            }

            if (this.application === ApplicationType.JiraServerStaticTab ||
                this.application === ApplicationType.JiraServerTab) {

                if (this.application === ApplicationType.JiraServerStaticTab) {
                    const { jiraUrl } = await this.apiService.getJiraUrlForPersonalScope();
                    this.jiraUrl = jiraUrl;
                }

                if (this.jiraUrl) {
                    const { displayName, accountId } = await this.apiService.getMyselfData(this.jiraUrl);
                    this.displayName = displayName;
                    this.accountId = accountId;
                    const r = await this.apiService.validateConnection(this.jiraUrl);
                    if (r.isSuccess) {
                        await this.onUserAuthenticated();
                        return;
                    } else {
                        await this.onUserNotAuthenticated(StatusCode.Unauthorized);
                        return;
                    }
                } else {
                    return await this.onUserNotAuthenticated(StatusCode.Unauthorized);
                }
            }

            await this.onUserAuthenticated();

        } catch (error) {
            if (error instanceof HttpErrorResponse) {
                if (error.status === StatusCode.Forbidden) {
                    await this.onUserNotAuthenticated(error.status);
                }
                return;
            }

            this.errorService.showDefaultError(error);
        }
    }

    private createJiraLink(): string {
        this.jiraUrl = this.utilService.convertStringToNull(decodeURIComponent(this.jiraUrl));

        if (!this.jiraUrl) {
            return '';
        }

        const currentJiraProjectUrl = this.projectKey && `${this.jiraUrl}/browse/${this.projectKey}` || this.jiraUrl;
        return this.showStaticTabElements
            ? this.jiraUrl
            : currentJiraProjectUrl;
    }

    private onCheckAddonInstalledFailed(): void {
        this.errorService.showAddonNotInstalledWindow();
    }

    private async loadData(): Promise<void> {
        this.error = undefined;
        this.loadingOn();
        await this.loadIssues();
        this.loadingOff();
    }

    private async loadIssues(): Promise<void> {
        try {
            const transformedJqlQuery = this.getTransformedJqlQuery();

            this.decodedTransformedJqlQuery = decodeURIComponent(transformedJqlQuery);

            this.setColumnsSorting(this.decodedTransformedJqlQuery);

            this.jiraLinkWithJql = `${this.jiraUrl}/issues/?jql=${transformedJqlQuery}`;

            this.issues = await this.getIssues(transformedJqlQuery);
            this.setDisplayedColumns();
        } catch (error) {
            this.appInsightService.trackException(error, 'Issue-table.component::loadData');

            if (error instanceof HttpErrorResponse) {
                if (error.status === StatusCode.NotFound || error.status === StatusCode.Forbidden) {
                    this.error = this.errorService.PROJECT_PERMISSIONS_ERROR;
                }
            } else {
                this.error = this.errorService.DEFAULT_ERROR_MESSAGE;
            }
        }

        this.dataSource = new MatTableDataSource(this.issues || []);
    }

    private async loadIssuesWithSpinner(): Promise<void> {
        this.loadingTableData = true;
        await this.loadIssues();
        this.loadingTableData = false;
    }

    public async sortData(columnName: string): Promise<void> {
        if (this.isMobile) {
            return;
        }

        const columnDirection = this.sortedColumnsState.get(columnName);

        this.sortDirection = !columnDirection || columnDirection === JiraSortDirection.Desc ?
            JiraSortDirection.Asc :
            JiraSortDirection.Desc;

        this.activeColumn = columnName;

        this.sortedColumnsState.clear();
        this.sortedColumnsState.set(this.activeColumn, this.sortDirection);

        this.jqlOrderBySuffix = this.sortDirection ? ` order by ${this.activeColumn} ${this.sortDirection}` : '';
        await this.loadIssuesWithSpinner();
    }

    private setColumnsSorting(jql: string): void {
        if (!jql) {
            return;
        }

        const arr = jql.toLowerCase().split('order by ');
        let orderByQuery = '';
        if (arr.length > 1) {
            orderByQuery = arr[1];
            // if there is sorting by several fields (in jql it's separated by comma) - do not set data sorting in our table
            if (orderByQuery.split(',').length > 1) {
                this.activeColumn = '';
                this.sortDirection = JiraSortDirection.None;
                return;
            }
        }

        orderByQuery = this.issuesService.adjustOrderByQueryStringWithIssueProperty(orderByQuery, 'key', 'issuekey');
        orderByQuery = this.issuesService.adjustOrderByQueryStringWithIssueProperty(orderByQuery, 'type', 'issuetype');

        for (let i = 0; i < this.avaialableToDisplayColumns.length; i++) {
            const column = this.avaialableToDisplayColumns[i].toLowerCase();
            if (orderByQuery.indexOf(column) !== -1) {
                this.activeColumn = column;
                const jqlArray = jql.toLowerCase().split(' ');
                const isAsc = jqlArray.indexOf('asc') !== -1;

                /* set sortDirection just in case we have an appropriate active column in jql
                   in another case remain default activeColumn and sortDirection */
                this.sortDirection = isAsc ? JiraSortDirection.Asc : JiraSortDirection.Desc;
                return;
            } else {
                // reset active column value
                this.activeColumn = '';
                continue;
            }
        }
    }

    private setDisplayedColumns(): void {
        if (!this.issues.length || this.displayedColumns.length) {
            return;
        }

        const issue = this.issues[0];

        this.displayedColumns = this.avaialableToDisplayColumns
            .filter(fieldColumnName => issue[fieldColumnName] !== undefined);
    }

    private async onUserAuthenticated(): Promise<void> {
        if (this.showStaticTabElements) {
            await this.setStaticTabFilters();
        }

        await this.loadData();
    }

    private getTransformedJqlQuery(): string {
        let options = {} as JqlOptions;
        if (this.page) {
            const selectedJiraFilter = this.selectedJiraFilter && this.selectedJiraFilter.value;
            options = { jql: selectedJiraFilter, jqlSuffix: this.jqlOrderBySuffix, accountId: this.accountId, page: this.page };
        } else {
            options = { jql: this.jqlQuery, jqlSuffix: this.jqlOrderBySuffix, projectKey: this.projectKey };
        }

        return this.issuesService.createJqlQuery(options);
    }

    private async getIssues(jqlQuery: string): Promise<NormalizedIssue[]> {
        const { issues, prioritiesIdsInOrder, errorMessages, total, pageSize } =
                await this.apiService.getIssues(this.jiraUrl, jqlQuery, this.startAt);
        this.pageCount = total;
        this.pageSize = pageSize;

        this.error = '';

        if (errorMessages && errorMessages.length) {
            this.error = `${errorMessages[0]} ${this.errorService.PROJECT_PERMISSIONS_ERROR}`;
            return [];
        }

        return this.issuesService.normalizeIssues(issues || [], prioritiesIdsInOrder);
    }

    private async onUserNotAuthenticated(status: number): Promise<void> {
        logger('IssuesTableComponent::onUserNotAuthenticated', status);

        if (!this.jiraUrl) {
            await this.router.navigate(['/login', { ...this.route.snapshot.params, status }]);
            return;
        }

        const { addonStatus } = await this.apiService.getAddonStatus(this.jiraUrl);
        const addonIsInstalled = addonStatus === AddonStatus.Installed || addonStatus === AddonStatus.Connected;

        if (addonIsInstalled) {
            await this.router.navigate(['/login', { ...this.route.snapshot.params, status, jiraUrl: this.jiraUrl }]);
            return;
        }

        this.onCheckAddonInstalledFailed();
    }

    private async setStaticTabFilters(): Promise<void> {
        if (this.jiraFilters) {
            return;
        }

        const page = this.route.snapshot.params.page;
        if (page === PageName.IssuesAssigned || page === PageName.IssuesReported || page === PageName.IssuesWatched) {
            this.initialJqlOrderBySuffix = ` order by ${this.defaultActiveColumn} ${this.defaultSortDirection}`;
            this.jiraFilters = await this.getJiraStatusFilters();
            this.searchableFilters = false;
        } else {
            // for saved filters we keep sorting from Jira and we don't add our 'order by' sequence
            this.initialJqlOrderBySuffix = '';
            this.jiraFilters = await this.getJiraFilters();
            this.searchableFilters = true;
        }
        this.jqlOrderBySuffix = this.initialJqlOrderBySuffix;
        this.selectedJiraFilter = this.jiraFilters && this.jiraFilters.length ? this.jiraFilters[0] : undefined;
    }

    private async getJiraFilters(skipEmptyResult: boolean = false): Promise<SelectOption[] | never> {
        this.filtersLoading = true;

        const filters = await this.apiService.getFavouriteFilters(this.jiraUrl);

        this.filtersLoading = false;
        if ((!filters || !filters.length) && !skipEmptyResult) {
            await this.errorService.showMyFiltersEmptyError();
            return;
        }

        return filters.map(
            (filter) => ({
                id: filter.id,
                label: filter.name,
                value: filter.jql
            })
        );
    }

    private async getJiraStatusFilters(): Promise<SelectOption[]> {
        const statuses = await this.apiService.getStatuses(this.jiraUrl);

        const defaultFilter = { id: 0, label: 'All issues', value: '' };
        const jiraFilters = statuses.map((status, index) => ({
            id: ++index,
            label: status.name,
            value: ` status = "${status.name}" `
        }));
        jiraFilters.unshift(defaultFilter);

        return jiraFilters;
    }

    private async getConfigurationFiltersForHeader(jqlQuery: string, jiraUrl: string): Promise<string> {
        const filters = this.issuesService.getFiltersFromQuery(jqlQuery);

        if (filters === '0') {
            const id = jqlQuery.split('=')[1];
            let savedFilter = await this.apiService.getFilter(jiraUrl, id);

            // try to find saved filter in list of all filters. Used for backward compatibility with older addons
            if (!savedFilter) {
                const savedFilters = await this.apiService.getFavouriteFilters(jiraUrl);
                savedFilter = savedFilters.find((x) => x.id === id);
            }

            if (savedFilter && savedFilter.jql) {
                // for saved filters we keep sorting from Jira and we don't add our 'order by' sequence
                this.jqlOrderBySuffix = '';
                this.jqlQuery = this.issuesService.createFullFilterJql(this.jqlQuery, savedFilter.jql);
            }
            return savedFilter ? savedFilter.name : '';
        }

        return filters;
    }

    private loadingOn(): void {
        this.loadingIndicatorService.show();
        this.loading = true;
    }

    private loadingOff(): void {
        this.loadingIndicatorService.hide();
        this.loading = false;
    }

    public async openSignOutDialog(): Promise<void> {
        const dialogConfig = {
            ...{
                width: '350px',
                height: '200px',
                minWidth: '200px',
                minHeight: '150px',
                ariaLabel: 'Confirmation dialog',
                closeOnNavigation: true,
                autoFocus: false,
                role: 'dialog'
            } as MatDialogConfig,
            ...{
                data: {
                    jiraUrl: this.jiraUrl
                }
            }
        };

        this.dialog.open(SignoutMaterialDialogComponent, dialogConfig).afterClosed()
            .subscribe(async (isConfirmed: boolean) => {
                if (isConfirmed) {
                    await this.router.navigate(['/login', { ...this.route.snapshot.params, status: StatusCode.Unauthorized }]);
                }
            });
    }
}
