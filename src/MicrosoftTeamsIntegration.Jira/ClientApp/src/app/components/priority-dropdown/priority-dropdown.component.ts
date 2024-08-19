import { Component, OnInit, Input, ViewChild, Output, EventEmitter } from '@angular/core';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { ApiService } from '@core/services';

import { Priority } from '@core/models';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { DropDownOption } from '@shared/models/dropdown-option.model';

@Component({
    selector: 'app-priority-dropdown',
    templateUrl: './priority-dropdown.component.html',
    styleUrls: ['./priority-dropdown.component.scss']
})
export class PriorityDropdownComponent implements OnInit {

    public priorityOptions: DropDownOption<string>[] = [];
    public selectedOption: DropDownOption<string> | any;
    public loading = false;
    public errorMessage: string | undefined;

    private priorities: Priority[] | undefined;

    @ViewChild(DropDownComponent, {static: false}) dropdown: DropDownComponent<string> | any;

    @Input() jiraUrl: string | any;
    @Input() issuePriorityId: string | any;
    @Input() issueId: string | any;

    @Output() priorityChange: EventEmitter<string> = new EventEmitter<string>();

    constructor(
        private apiService: ApiService,
        private dropdownUtilService: DropdownUtilService
    ) { }

    public async ngOnInit(): Promise<void> {
        this.loading = true;

        this.priorities = await this.apiService.getPriorities(this.jiraUrl);

        this.priorityOptions = this.priorities.map(this.dropdownUtilService.mapPriorityToDropdownOption);

        this.selectedOption = this.priorityOptions.find(prt => prt.id === this.issuePriorityId);

        this.loading = false;
    }

    public async onPriorityOptionSelected(option: DropDownOption<string>): Promise<void> {
        this.loading = true;

        try {
            const priority = this.priorities?.find(prt => prt.id === option.id) as any;
            const response = await this.apiService.updatePriority(this.jiraUrl, this.issueId, priority);

            if (response.isSuccess) {
                this.priorityChange.emit(priority.id);
            } else {
                this.dropdown.setPreviousValue();
                this.errorMessage = response.errorMessage || 'Error occured while updating.';
            }
        } catch (error) {
            this.dropdown.setPreviousValue();
            this.errorMessage = (error as any).errorMessage || (error as any).message || 'Error occured.';
        }

        this.loading = false;
    }
}
