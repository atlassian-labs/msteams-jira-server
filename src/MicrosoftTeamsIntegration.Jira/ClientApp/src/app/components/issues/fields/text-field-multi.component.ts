import { Component, Input } from '@angular/core';
import { FieldComponent } from './field.component';
import { UntypedFormGroup } from '@angular/forms';

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
          <textarea class="field-group__textarea-single" placeholder="{{data.placeholder}}" cols="20" rows="3"
          maxlength="1024" formControlName="{{data.formControlName}}" [attr.disabled]="data.disabled"
                    value="{{data.defaultValue}}"></textarea>
		</div>
	  </div>
    </div>
    `,
    standalone: false
})

export class TextFieldMultiComponent implements FieldComponent {
    @Input() data: any;
    @Input() formGroup: UntypedFormGroup | any;
}
