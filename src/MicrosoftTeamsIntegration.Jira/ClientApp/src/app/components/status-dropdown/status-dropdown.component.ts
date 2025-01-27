import { Component, OnInit, Input, ViewChild, Output, EventEmitter } from '@angular/core';

import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { AppInsightsService } from '@core/services';

import { DropDownOption } from '@shared/models/dropdown-option.model';
import { JiraTransition } from '@core/models/Jira/jira-transition.model';
import { IssueStatus } from '@core/models';

@Component({
    selector: 'app-status-dropdown',
    templateUrl: './status-dropdown.component.html',
    styleUrls: ['./status-dropdown.component.scss'],
    standalone: false
})
export class StatusDropdownComponent implements OnInit {

    public loading = false;
    public statusOptions: DropDownOption<JiraTransition>[] | any;
    public selectedOption: DropDownOption<JiraTransition> | any;
    public errorMessage: string | any;

    @Input() public jiraUrl: string | any;
    @Input() public projectKey: string | any;
    @Input() public issueKey: string | any;
    @Input() public initialStatus: IssueStatus | any;

    @Output() statusChange = new EventEmitter<string>();

    @ViewChild(DropDownComponent, { static: false }) dropdown: DropDownComponent<string> | any;

    constructor(
        private transitionService: IssueTransitionService,
        private dropdownUtilService: DropdownUtilService,
        private appInsightsService: AppInsightsService
    ) {
    }

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl as string, this.issueKey as string);

        const initOption = {
            id: this.initialStatus?.id,
            value: {
                id: this.initialStatus?.id
            },
            label: this.initialStatus?.name
        } as DropDownOption<JiraTransition>;

        this.statusOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdownOption);

        const initOptionInTransitions = this.statusOptions.find((option: { value: { to: { id: any } } }) =>
            option?.value?.to.id === this.initialStatus?.id);
        if (initOptionInTransitions) {
            this.selectedOption = initOptionInTransitions;
        } else {
            this.statusOptions.unshift(initOption);
            this.selectedOption = initOption;
        }

        this.loadingOff();
    }

    public async onStatusOptionSelected(option: DropDownOption<JiraTransition>): Promise<void> {
        this.loadingOn();

        try {
            const response =
                await this.transitionService.doTransition(this.jiraUrl as string, this.issueKey as string, option?.value?.id as string);

            if (response.isSuccess) {
                this.selectedOption = option;
                this.statusChange.emit(option?.value?.to.id);

                await this.loadTransitions();
            } else {
                this.dropdown?.setPreviousValue();

                this.errorMessage = response.errorMessage || 'Error occured.';
            }
        } catch (error: any) {
            this.appInsightsService.trackException(
                new Error(error),
                'StatusDropdwonComponent::onStatusOptionSelected',
                option
            );

            this.dropdown?.setPreviousValue();
            this.errorMessage = error.errorMessage || error.message || 'Error occured.';
        } finally {
            this.loadingOff();
        }
    }

    private async loadTransitions(): Promise<void> {
        this.loadingOn();

        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl as string, this.issueKey as string);
        this.statusOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdownOption);
        if (this.selectedOption) {
            this.statusOptions.unshift(this.selectedOption);
        }

        this.loadingOff();
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}
