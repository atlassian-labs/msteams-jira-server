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

			<div class="field-group__body date-picker-container">
			  <input [matDatepicker]="datePicker"
                     placeholder="{{data.placeholder}}"
                     formControlName="{{data.formControlName}}"
                     class="input-date-picker"
                     [disabled]="false"
                     [(ngModel)]="selectedDate">
			  <mat-datepicker-toggle [for]="datePicker"></mat-datepicker-toggle>
			  <mat-datepicker #datePicker disabled="false"></mat-datepicker>
			</div>
		</div>
	</div>
    `,
    standalone: false
})

export class DatePickerFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup | any;
    public selectedDate: any;

    ngOnInit(): void {
        if (this.data.defaultValues) {
            this.selectedDate = this.data.defaultValues;
        }
    }
}
