import { Component, Input, OnInit } from '@angular/core';
import { FieldComponent } from './field.component';
import { FormGroup } from '@angular/forms';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';

@Component({
    styleUrls: ['./dynamic-fields-styles.scss'],
    template: `
    <div [formGroup]="formGroup">
        <div *ngIf="formGroup.get(data.formControlName)" class="field-group">
            <div class="field-group__header">
                <label [ngClass]="data.required ? 'field-group__label icon-required' : 'field-group__label'">{{data.name}}</label>

                <span *ngIf="labelsError" class="field-group__error">
                    Labels can't have spaces or be more than 255 characters.
                </span>
            </div>

            <div class="field-group__body">
            <ng-select  [items]="labelOptions"
                        bindLabel="name"
                        bindValue="id"
                        [addTag]="addCustomLabel"
                        (search)="onSearch($event)"
                        (open)="onOpen()"
                        (clear)="onClear()"
                        (blur)="onBlur($event)"
                        addTagText="{{data.addTagText}}"
                        [multiple]="true"
                        [hideSelected]="true"
                        [loading]="loading"
                        placeholder="{{data.placeholder}}"
                        formControlName="{{data.formControlName}}"
                        [(ngModel)]="selectedOptionIds"
                        [attr.disabled]="data.disabled">  
            </ng-select>
          </div>
        </div>
    </div>
    `
})

export class LabelsFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: FormGroup;

    private readonly LABEL_MAX_LENGTH: number = 255;

    public loading: boolean;
    public labelsError: boolean;
    public jiraUrl: string;
    public labelOptions: any;
    public selectedOptionIds: any;
    public dataInitialized: boolean;

    constructor(
        private apiService: ApiService,
        private dropdownUtilService: DropdownUtilService,
    ){}

    public async ngOnInit(): Promise<void> {
        this.loadingOn();

        this.jiraUrl = this.data.jiraUrl;

        this.loadingOff();
    }

    public addCustomLabel(term: string): any {
        if (/\s/.test(term) || term.length > this.LABEL_MAX_LENGTH) {
            return null;
        }
        return ({id: term, name: term});
    }

    public onSearch($event) {
        if (/\s/.test($event.term) || $event.term.length > this.LABEL_MAX_LENGTH) {
            this.labelsError = true;
        } else {
            this.labelsError = false;
        }
    }

    public async onOpen() {
        if (!this.dataInitialized)  {
            this.loadingOn();

            const labelsAutocompleteData = await this.apiService.getAutocompleteData(this.jiraUrl, 'labels');
            this.labelOptions = labelsAutocompleteData.map(this.dropdownUtilService.mapAutocompleteDataToSelectOption);
            this.dataInitialized = true;

            this.loadingOff();
        }
    }

    public onClear() {
        this.labelsError = false;
    }

    public onBlur() {
        this.labelsError = false;
    }

    private loadingOn(): void {
        this.loading = true;
    }

    private loadingOff(): void {
        this.loading = false;
    }
}