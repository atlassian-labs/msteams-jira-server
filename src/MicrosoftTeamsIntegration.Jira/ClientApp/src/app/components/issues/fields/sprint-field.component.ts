import { Component, Input, OnInit } from '@angular/core';
import { FieldComponent } from './field.component';
import { UntypedFormGroup } from '@angular/forms';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';

@Component({
    styleUrls: ['./dynamic-fields-styles.scss'],
    template: `
    <div [formGroup]="formGroup">
        <div *ngIf="formGroup.get(data.formControlName)" class="field-group">
            <div class="field-group__header">
                <label [ngClass]="data.required ? 'field-group__label icon-required' : 'field-group__label'">{{data.name}}</label>

                <span class="field-group__error">
                    <!-- Error here-->
                </span>
            </div>

            <div class="field-group__body">
            <ng-select  [items]="sprintOptions"
                        bindLabel="name"
                        bindValue="id"
                        groupBy="state"
                        (open)="onOpen()"
                        [multiple]="false"
                        [hideSelected]="true"
                        [loading]="loading"
                        placeholder="{{data.placeholder}}"
                        formControlName="{{data.formControlName}}"
                        [attr.disabled]="data.disabled"
                        [(ngModel)]="selectedSprintId">
            </ng-select>
          </div>
        </div>
    </div>
    `,
    standalone: false
})

export class SprintFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup | any;

    public loading: boolean | any;
    public jiraUrl: string | undefined;
    public projectKeyOrId: string | undefined;
    public dataInitialized: boolean | undefined;
    public sprintOptions: any[] = [];
    public selectedSprintId: any;

    constructor(
        private apiService: ApiService,
        private dropdownUtilService: DropdownUtilService,
    ){}

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        this.jiraUrl = this.data.jiraUrl;
        this.projectKeyOrId = this.data.projectKeyOrId;
        // add empty option to allow drop down to open and trigger onOpen event
        this.sprintOptions = [{}];

        if (this.data.defaultValue) {
            for (const sprintString of this.data.defaultValue) {
                const match = sprintString.match(/name=([^,]+)/);
                if (!match || !match[1]) {
                    continue;
                }

                const sprintName = match[1].trim();

                await this.onOpen();

                const foundSprint = this.sprintOptions.find(
                    s => s.name === sprintName
                );

                if (foundSprint) {
                    this.selectedSprintId = foundSprint.id;
                    break;
                }
            }
        }

        this.loadingOff();
    }

    public async onOpen() {
        if (!this.dataInitialized) {
            this.loadingOn();

            // remove empty field before getting real values
            this.sprintOptions = [];
            // try to get all sprints for selected project
            let sprintsData = await this.apiService.getSprints(this.jiraUrl as string, this.projectKeyOrId as string);
            const groupOrder = ['active', 'future', 'closed'];
            sprintsData = sprintsData.sort((a, b) => groupOrder.indexOf(a.state) - groupOrder.indexOf(b.state));
            this.sprintOptions = sprintsData.map(this.dropdownUtilService.mapSprintDataToSelectOption);
            this.dataInitialized = true;

            this.loadingOff();
        }
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}
