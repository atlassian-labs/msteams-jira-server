import { Component, Input, OnInit } from '@angular/core';
import { FieldComponent } from './field.component';
import { FormGroup, FormControl, Validators } from '@angular/forms';
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

            <div class="field-group__body select-cascading-options">
                <!-- Hidden input to set result of selection -->
                <input formControlName="{{data.formControlName}}"  [(ngModel)]="selectedCascadingOptions" style="display: none"/>
                <!-- Parent select options -->
				<ng-select  [items]="allowedParentOptions"
							bindLabel="name"
                            bindValue="id"
                            class="select-cascading-parent"
                            (change)="onParentChange($event)"
							[multiple]="false"
							[hideSelected]="true"
                            placeholder="{{data.placeholder}}"
                            formControlName="{{data.formControlName}}_parent"
                            [attr.disabled]="data.disabled"
                            [(ngModel)]="selectedParentId">
                </ng-select>
                <!-- Children select options -->
                <ng-select  [items]="allowedChildrenOptions"
							bindLabel="name"
                            bindValue="id"
                            class="select-cascading-children"
                            (change)="onChildrenChange($event)"
							[multiple]="false"
							[hideSelected]="true"
                            placeholder="{{data.placeholder}}"
                            formControlName="{{data.formControlName}}_children"
                            [attr.disabled]="data.disabled"
                            [(ngModel)]="selectedChildId">
                </ng-select>
			</div>
        </div>
    </div>
    `
})

export class SelectCascadingFieldComponent implements FieldComponent, OnInit {
    @Input() data: any;
    @Input() formGroup: FormGroup;

    public children: FormControl;
    public parent: FormControl;

    public allowedParentOptions: any[];
    public allowedChildrenOptions: any[];

    public selectedParentId: any;
    public selectedChildId: any;
    public selectedCascadingOptions: any;

    constructor(
        private dropdownUtilService: DropdownUtilService
    ){}

    public ngOnInit() {

        this.formGroup.addControl(this.data.formControlName + '_parent', this.data.required ? new FormControl(null, [Validators.required]) : new FormControl());
        this.formGroup.addControl(this.data.formControlName + '_children', new FormControl());
        
        if (this.data.allowedValues) {
            // get allowed values for cascading including all child values
            this.allowedParentOptions = this.data.allowedValues.map(this.dropdownUtilService.mapAllowedValueWithChildrenToSelectOption);

            // select default values only if we have some values to show
            const defaultValue = this.data.defaultValue
            if (defaultValue) {
                this.selectedParentId = defaultValue.id || defaultValue;

                // get allowed children values for default parent
                const defaultParentOption = this.allowedParentOptions.find(x => x.id === this.selectedParentId);

                if (defaultParentOption) {
                    this.allowedChildrenOptions = defaultParentOption.children.map(this.dropdownUtilService.mapAllowedValueToSelectOption); 
                }                

                // get default children if it was set
                if (defaultValue.child && defaultValue.child.id) {
                    this.selectedChildId = defaultValue.child.id;
                }
            }
        }
    }

    public onParentChange($event) {
        var selectedValue = $event;
        this.selectedChildId = []; // clear child options

        if (selectedValue) {
            this.allowedChildrenOptions = selectedValue.children.map(this.dropdownUtilService.mapAllowedValueToSelectOption);

            // create an cascading object for selected parent value
            this.selectedCascadingOptions = ({id: selectedValue.id});
        } else {
            this.allowedChildrenOptions = [];
        }
    }

    public onChildrenChange($event) {
        var selectedValue = $event;

        if (selectedValue) {
            // create an cascading object for selected parent and child value
            this.selectedCascadingOptions = ({id: this.selectedParentId, child: {id: selectedValue.id}});
        }
    }
}