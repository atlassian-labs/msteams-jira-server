import { Component, Input } from '@angular/core';
import { FieldComponent } from './field.component';
import { FormGroup } from '@angular/forms';

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
          <input matInput type="text" placeholder="{{data.placeholder}}" maxlength="254" 
            formControlName="{{data.formControlName}}" [attr.disabled]="data.disabled" value="{{data.defaultValue}}">
		</div>
	  </div>
    </div>
    `
})

export class TextFieldSingleComponent implements FieldComponent {
    @Input() data: any;
    @Input() formGroup: FormGroup;
}