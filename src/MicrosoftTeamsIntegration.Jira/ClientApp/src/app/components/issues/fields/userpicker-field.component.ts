import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { FieldComponent } from './field.component';
import { UntypedFormGroup } from '@angular/forms';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { JiraUser } from '@core/models/Jira/jira-user.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';

@Component({
    styleUrls: ['./dynamic-fields-styles.scss'],
    template: `
    <div [formGroup]="formGroup">
	  <div *ngIf="formGroup.get(data.formControlName)" class="field-group">
		<div class="field-group__header">
		  <label [ngClass]="data.required ? 'field-group__label icon-required' : 'field-group__label'">
			{{data.name}}
		  </label>
		  <span class="field-group__error">
            <!-- Error here-->
		  </span>
		</div>

        <div class="field-group__body">
            <app-dropdown
                [searchable]="true"
                [loading]="loading"
                [options]="options"
                [filteredOptions]="filteredOptions"
                (searchChange)="onSearchChanged($event)"
                formControlName="{{data.formControlName}}"
                [disabled]="data.disabled">
            </app-dropdown>
		</div>
	  </div>
    </div>
    `
})

export class UserPickerFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup | any;

    @ViewChild(DropDownComponent, {static: false}) public dropdown: DropDownComponent<string> | any;

    public readonly noneOption: DropDownOption<string> = {
        id: -1,
        value: null,
        label: 'None',
        icon: '/assets/useravatar24x24.png'
    };

    public loading = false;
    private users: JiraUser[] = [];
    public options: DropDownOption<string>[] = [];
    public filteredOptions: DropDownOption<string>[] = [];
    public jiraUrl: string | undefined;

    constructor(
        private apiService: ApiService,
        private dropdownUtilService: DropdownUtilService,
    ){}

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        this.jiraUrl = this.data.jiraUrl;

        if (this.data.defaultValue) {
            this.options.unshift(this.dropdownUtilService.mapUserToDropdownOption(this.data.defaultValue));
        } else {
            this.options.unshift(this.noneOption);
        }

        this.loadingOff();
    }

    private async getUserOptions(username: string = ''): Promise<DropDownOption<string>[]> {

        // try to get users on search
        this.users = await this.apiService.searchUsers(this.jiraUrl as string, username);

        return this.users.map(this.dropdownUtilService.mapUserToDropdownOption);
    }

    public async onSearchChanged(username: string): Promise<void> {
        this.loadingOn();

        username = username.trim().toLowerCase();

        const userOptions = await this.getUserOptions(username);

        this.dropdown.filteredOptions = userOptions;

        this.loadingOff();
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}
