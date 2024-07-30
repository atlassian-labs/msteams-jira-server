import { Component, Input, OnInit } from '@angular/core';
import { FieldComponent } from './field.component';
import { UntypedFormGroup } from '@angular/forms';

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
				<ng-select  [items]="data.allowedValues"
							bindLabel="name"
							bindValue="id"
							[multiple]="data.multiple"
							[hideSelected]="true"
							placeholder="{{data.placeholder}}"
							formControlName="{{data.formControlName}}"
                            [attr.disabled]="data.disabled"
                            [(ngModel)]="selectedOptionIds">
				</ng-select>
			</div>
        </div>
    </div>
    `
})

export class SelectFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup;

    public selectedOptionIds: any;

    public ngOnInit() {
        if (this.data.defaultValue) {
            this.selectedOptionIds = this.data.defaultValue;
        }
    }
}
