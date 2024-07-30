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
            <ng-select  [items]="epicOptions"
                        bindLabel="name"
                        bindValue="id"
                        (open)="onOpen()"
                        [multiple]="false"
                        [hideSelected]="true"
                        [loading]="loading"
                        placeholder="{{data.placeholder}}"
                        formControlName="{{data.formControlName}}"
                        [attr.disabled]="data.disabled">
                <ng-template ng-label-tmp let-item="item">
                    <label class="epic-color-label epic-{{item.color}}">{{item.name}}</label>
                </ng-template>
                <ng-template ng-option-tmp let-item="item" let-index="index">
                    <table>
                        <tr>
                            <td>
                                <label class="epic-color-preview epic-{{item.color}}"></label>
                            </td>
                            <td>
                                <tr>
                                    <td>
                                    {{item.name}}
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <div class="epic-key-text">
                                            {{item.key}}
                                        </div>
                                    </td>
                                </tr>
                            </td>
                        </tr>
                    </table>
                </ng-template>
            </ng-select>
          </div>
        </div>
    </div>
    `
})

export class EpicFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup;

    public loading: boolean;
    public jiraUrl: string;
    public projectKeyOrId: string;
    public dataInitialized: boolean;
    public epicOptions: any[] = [];

    constructor(
        private apiService: ApiService,
        private dropdownUtilService: DropdownUtilService,
    ){}

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        this.jiraUrl = this.data.jiraUrl;
        this.projectKeyOrId = this.data.projectKeyOrId;
        // add empty option to allow drop down to open and trigger onOpen event
        this.epicOptions = [{}];

        this.loadingOff();
    }

    public async onOpen() {
        if (!this.dataInitialized) {
            this.loadingOn();

            // remove empty field before getting real values
            this.epicOptions = [];
            // try to get all epics for selected project
            const epicsData = await this.apiService.getEpics(this.jiraUrl, this.projectKeyOrId);
            this.epicOptions = epicsData.map(this.dropdownUtilService.mapEpicDataToSelectOption);
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
