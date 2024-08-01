import { Component, OnInit, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { Issue } from '@core/models';
import { ApiService } from '@core/services/api.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { HttpErrorResponse } from '@angular/common/http';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { AppInsightsService } from '@core/services';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { SearchAssignableOptions } from '@core/models/Jira/search-assignable-options';

@Component({
    selector: 'app-assignee-dropdown',
    templateUrl: './assignee-dropdown.component.html',
    styleUrls: ['./assignee-dropdown.component.scss']
})
export class AssigneeDropdownComponent implements OnInit {

    public error: HttpErrorResponse | Error | undefined;
    public errorMessage: string | undefined;

    public loading = false;

    public options: DropDownOption<string>[] = [];
    public selectedOption: DropDownOption<string> | undefined;

    public currentUserAccountId: string | undefined;

    @Input() public issue: Issue | any;
    @Input() public projectKey: string| any;
    @Input() public issueKey: string| any;
    @Input() public assignee: JiraUser | null | any;
    @Input() public jiraUrl: string| any;

    @Output() public assigneeChange = new EventEmitter<JiraUser | null>();

    @ViewChild(DropDownComponent, {static: false}) public dropdown: DropDownComponent<string> | any;

    private assignees: JiraUser[] = [];

    constructor(
        private apiService: ApiService,
        private assigneeService: AssigneeService,
        private appInsightsService: AppInsightsService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        await this.setOptions();

        const { accountId, name } = await this.apiService.getCurrentUserData(this.jiraUrl);
        this.currentUserAccountId = accountId || name;

        this.loadingOff();
    }

    public async onOptionSelected(option: DropDownOption<string>): Promise<void> {
        await this.setAssignee(option.value);
    }

    public async assignToMe(): Promise<void> {
        await this.setAssignee(this.currentUserAccountId as string);
    }

    /**
   *
   * @param userAccountIdOrName userAccountId as string or name with values null or '-1' to set to the Unassigned and Automatic assignee
   * @returns {boolean} Shows whether the request was successful or not
   */
    private async setAssignee(userAccountIdOrName: string | null | '-1'): Promise<void> {
        this.loadingOn();

        const response = await this.assigneeService.setAssignee(this.jiraUrl, this.issueKey, userAccountIdOrName);

        if (!response.isSuccess) {
            // show pop up or error
            this.dropdown.setPreviousValue();
            this.loadingOff();

            this.errorMessage = response.errorMessage || 'Error on user assign. Please check your permissions and reload the page.';
            this.appInsightsService.trackException(
                new Error(response.errorMessage),
                'AssigneeDropdownComponent::setAssignee'
            );

            return;
        }

        // now returns name, instead of account id
        const newAccountId = response.content || null;

        this.selectedOption = this.options.find(option => option.value === newAccountId);
        this.assigneeChange.emit(this.assignees.find(user => user.accountId === newAccountId));

        this.loadingOff();
    }

    public async onSearchChanged(userDisplayNameOrEmail: string): Promise<void> {
        this.loadingOn();

        userDisplayNameOrEmail = userDisplayNameOrEmail.trim().toLowerCase();

        const assigneeOptions = await this.getAssigneeOptions(userDisplayNameOrEmail);

        this.dropdown.filteredOptions = assigneeOptions;

        this.loadingOff();
    }

    private async setOptions(): Promise<void> {
        this.options = await this.getAssigneeOptions();

        if (this.assignee === null) {
            this.selectedOption = this.assigneeService.unassignedOption;
        }

        if (this.assignee) {
            const value = this.assignee.accountId ? this.assignee.accountId : this.assignee.name;

            this.selectedOption = {
                id: value,
                value,
                icon: this.assignee.avatarUrls['24x24'],
                label: this.assignee.displayName
            };

            this.options.push(this.selectedOption);
        }
    }

    private async getAssigneeOptions(userDisplayNameOrEmail: string = ''): Promise<DropDownOption<string>[]> {
        // TODO: retrieve account id by display name if possible
        const options: SearchAssignableOptions = {
            jiraUrl: this.jiraUrl,
            issueKey: this.issueKey,
            projectKey: this.projectKey,
            query: userDisplayNameOrEmail || ''
        };
        this.assignees = await this.assigneeService.searchAssignable(options);

        return this.assigneeService.assigneesToDropdownOptions(this.assignees, userDisplayNameOrEmail);
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}
