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
		  <span *ngIf="validationError" class="field-group__error">
            Please enter a valid URL
		  </span>
		</div>

        <div class="field-group__body">
          <input matInput type="url" placeholder="{{data.placeholder}}" maxlength="254" (keyup)="onChange($event)"
            formControlName="{{data.formControlName}}" [attr.disabled]="data.disabled" value="{{data.defaultValue}}">
		</div>
	  </div>
    </div>
    `
})

export class UrlFieldComponent implements FieldComponent {
    @Input() data: any;
    @Input() formGroup: FormGroup;

    public validationError: boolean;

    public onChange($event: any) {
        const value = $event.target ? $event.target.value : null;

        // check if entered value is valid URL string
        if (!/^(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$/.test(value) && value !== "") {
            this.validationError = true;
        } else {
            this.validationError = false;
        }
    }
}