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
    styleUrls: ['./status-dropdown.component.scss']
})
export class StatusDropdownComponent implements OnInit {

    public loading = false;
    public statusOptions: DropDownOption<JiraTransition>[];
    public selectedOption: DropDownOption<JiraTransition>;
    public errorMessage: string;

    @Input() public jiraUrl: string;
    @Input() public projectKey: string;
    @Input() public issueKey: string;
    @Input() public initialStatus: IssueStatus;

    @Output() statusChange = new EventEmitter<string>();

    @ViewChild(DropDownComponent, {static: false}) dropdown: DropDownComponent<string>;

    constructor(
        private transitionService: IssueTransitionService,
        private dropdownUtilService: DropdownUtilService,
        private appInsightsService: AppInsightsService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl, this.issueKey);

        const initOption = {
            id: this.initialStatus.id,
            value: {
                id: this.initialStatus.id
            },
            label: this.initialStatus.name
        } as DropDownOption<JiraTransition>;

        this.statusOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdonwOption);

        const initOptionInTransitions = this.statusOptions.find(option => option.value.to.id === this.initialStatus.id);
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
            const response = await this.transitionService.doTransition(this.jiraUrl, this.issueKey, option.value.id);

            if (response.isSuccess) {
                this.selectedOption = option;
                this.statusChange.emit(option.value.to.id);

                await this.loadTransitions();
            } else {
                this.dropdown.setPreviousValue();

                this.errorMessage = response.errorMessage || 'Error occured.';
            }
        } catch (error) {
            this.appInsightsService.trackException(
                new Error(error),
                'StatusDropdwonComponent::onStatusOptionSelected',
                option
            );

            this.dropdown.setPreviousValue();
            this.errorMessage = error.errorMessage || error.message || 'Error occured.';
        } finally {
            this.loadingOff();
        }
    }

    private async loadTransitions(): Promise<void> {
        this.loadingOn();

        const jiraTransitionsResponse = await this.transitionService.getTransitions(this.jiraUrl, this.issueKey);
        this.statusOptions = jiraTransitionsResponse.transitions.map(this.dropdownUtilService.mapTransitionToDropdonwOption);
        this.statusOptions.unshift(this.selectedOption);

        this.loadingOff();
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}
