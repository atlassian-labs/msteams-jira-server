import {Component, Input, ViewChild, ComponentFactoryResolver, OnChanges, Type} from '@angular/core';

import { DynamicFieldsDirective } from './dynamic-fields.directive';
import { FieldItem } from './field-item';
import { FieldComponent } from './field.component';
import { UntypedFormGroup } from '@angular/forms';

@Component({
    selector: 'app-dynamic-fields',
    template: `
        <div [formGroup]="formGroup">
            <ng-template dynamic-fields-host></ng-template>
        </div>
    `
})

export class DynamicFieldsComponent implements OnChanges{
    @Input() dynamicFields: FieldItem[] | any;
    @Input() formGroup: UntypedFormGroup | any;
    @ViewChild(DynamicFieldsDirective, {static: true}) dynamicFieldsHost: DynamicFieldsDirective | any;

    constructor(private componentFactoryResolver: ComponentFactoryResolver) { }

    ngOnChanges() {
        this.loadDynamicFields();
    }

    private loadDynamicFields() {
        const dynamicFieldTemplates = this.dynamicFields;

        const viewContainerRef = this.dynamicFieldsHost.viewContainerRef;
        viewContainerRef.clear();

        dynamicFieldTemplates.forEach((field: { component: Type<unknown>; data: any }) => {
            const componentFactory = this.componentFactoryResolver.resolveComponentFactory(field.component);

            const componentRef = viewContainerRef.createComponent(componentFactory);
            (<FieldComponent>componentRef.instance).data = field.data;
            (<FieldComponent>componentRef.instance).formGroup = this.formGroup;
        });
    }
}
