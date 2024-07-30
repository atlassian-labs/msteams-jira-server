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
							[multiple]="false"
                            [hideSelected]="false"
                            [clearable]="true"
							placeholder="{{data.placeholder}}"
							formControlName="{{data.formControlName}}"
                            [attr.disabled]="data.disabled"
                            [(ngModel)]="selectedOptionId">
                    <ng-template ng-option-tmp let-item="item" let-index="index">
                        <mat-radio-button [checked]="item.id === selectedOptionId">{{item.name}}</mat-radio-button>
                    </ng-template>
				</ng-select>
			</div>
        </div>
    </div>
    `
})

export class RadioSelectFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup;

    public selectedOptionId: any;

    public ngOnInit() {

        if (this.data.defaultValue) {
            this.selectedOptionId = this.data.defaultValue;
        }
    }
}
