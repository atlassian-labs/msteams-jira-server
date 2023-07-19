import { Component, Input, OnInit } from '@angular/core';
import { FieldComponent } from './field.component';
import { FormGroup } from '@angular/forms';

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
							[multiple]="true"
                            [hideSelected]="false"
                            [closeOnSelect]="false"
							placeholder="{{data.placeholder}}"
							formControlName="{{data.formControlName}}"
                            [attr.disabled]="data.disabled"
                            [(ngModel)]="selectedOptionIds">
                    <ng-template ng-option-tmp let-item="item" let-index="index">
                        <mat-checkbox [checked]="selectedOptionIds.includes(item.id)">{{item.name}}</mat-checkbox>
                    </ng-template>
                </ng-select>
			</div>
        </div>
    </div>
    `
})

export class CheckboxSelectFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: FormGroup;

    public selectedOptionIds: any = [];

    public ngOnInit() {
        if (this.data.defaultValue) {
            this.selectedOptionIds = this.data.defaultValue;
        }
    }
}